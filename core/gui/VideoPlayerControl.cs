// ****************************************************************************
// 
// Copyright (C) 2005-2026 Doom9 & al
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// 
// ****************************************************************************

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace MeGUI.core.gui
{
    public partial class VideoPlayerControl : UserControl
    {
        private readonly ReaderWriterLock readerWriterLock = new ReaderWriterLock();
        private readonly object positionLock = new object();
        private readonly object frameLock = new object();
        private readonly Timer playTimer;

        private Bitmap currentFrame;
        private int position;
        private IVideoReader videoReader;
        private double framerate = 25;
        private Padding cropMargin;
        private bool displayActualFramerate;
        private bool ensureCorrectPlaybackSpeed;
        private double speedUp = 1d;
        private bool isPlaying;
        private double actualFramerate;
        private long fetchToken = 0;

        public event EventHandler PositionChanged;

        public VideoPlayerControl()
        {
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.Opaque |
                     ControlStyles.ResizeRedraw, true);
            UpdateStyles();

            playTimer = new Timer(PlayNextFrame);
        }

        #region Position Handling
        public void OnPositionChanged()
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool OffsetPosition(int offset, bool update)
        {
            bool success;
            lock (positionLock)
            {
                success = SetPositionInternal(position + offset);
            }

            InvokeOnPositionChanged();

            if (update)
                UpdateVideo();

            return success;
        }

        public bool OffsetPosition(int offset)
        {
            return OffsetPosition(offset, true);
        }

        private bool SetPositionInternal(int value)
        {
            int max = FrameCount - 1;
            if (value < 0) value = 0;
            else if (value > max) value = max;

            if (position == value) return false;

            position = value;
            return true;
        }

        public void InvokeOnPositionChanged()
        {
            if (IsHandleCreated && !IsDisposed && InvokeRequired)
                Invoke(new SimpleDelegate(OnPositionChanged));
            else
                OnPositionChanged();
        }
        #endregion

        #region Rendering & Painting
        public void UpdateVideo()
        {
            if (IsDisposed) return;

            int pos = position;
            long currentToken = Interlocked.Increment(ref fetchToken);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Bitmap newFrame = GetFrame(pos);
                if (newFrame != null)
                {
                    if (Interlocked.Read(ref fetchToken) == currentToken)
                    {
                        lock (frameLock)
                        {
                            currentFrame?.Dispose();
                            currentFrame = newFrame;
                        }

                        if (IsHandleCreated && !IsDisposed)
                        {
                            BeginInvoke((MethodInvoker)Invalidate);
                        }
                    }
                    else
                    {
                        newFrame.Dispose();
                    }
                }
            });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            lock (frameLock)
            {
                if (currentFrame != null && currentFrame.Width > 0 && currentFrame.Height > 0)
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                    float uncroppedW = Math.Max(1, currentFrame.Width - cropMargin.Horizontal);
                    float uncroppedH = Math.Max(1, currentFrame.Height - cropMargin.Vertical);

                    float scale = Math.Min(this.Width / uncroppedW, this.Height / uncroppedH);

                    float drawWidth = uncroppedW * scale;
                    float drawHeight = uncroppedH * scale;
                    float drawX = (this.Width - drawWidth) / 2f;
                    float drawY = (this.Height - drawHeight) / 2f;

                    RectangleF src = new RectangleF(
                        cropMargin.Left,
                        cropMargin.Top,
                        uncroppedW,
                        uncroppedH);

                    RectangleF dst = new RectangleF(drawX, drawY, drawWidth, drawHeight);

                    g.Clear(Color.Black);
                    g.DrawImage(currentFrame, dst, src, GraphicsUnit.Pixel);

                    if (displayActualFramerate)
                    {
                        g.DrawString(actualFramerate.ToString("0.00 fps"), Font, Brushes.Lime, 5, 5);
                    }
                }
                else
                {
                    g.Clear(Color.Black);
                }
            }
        }

        private Bitmap GetFrame(int pos)
        {
            readerWriterLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                IVideoReader reader = videoReader;
                if (reader == null) return null;
                return reader.ReadFrameBitmap(pos);
            }
            catch
            {
                return null;
            }
            finally
            {
                readerWriterLock.ReleaseReaderLock();
            }
        }
        #endregion

        #region Video Playback
        private void PlayNextFrame(object state)
        {
            try
            {
                if (!OffsetPosition(1, true))
                    Stop();
            }
            catch { }
        }

        public void Play()
        {
            if (videoReader == null)
                throw new InvalidOperationException("Video must be loaded before playback can be started");

            playTimer.Change(0, (int)(1000d / (Framerate * SpeedUp)));
            isPlaying = true;
            actualFramerate = Framerate * SpeedUp;
        }

        public void Stop()
        {
            playTimer.Change(Timeout.Infinite, Timeout.Infinite);
            isPlaying = false;
            actualFramerate = 0;
        }
        #endregion

        #region Load/Unload Video
        public void LoadVideo(IVideoReader reader) => LoadVideo(reader, 25, 0);
        public void LoadVideo(IVideoReader reader, double fps) => LoadVideo(reader, fps, 0);

        public void LoadVideo(IVideoReader reader, double fps, int startPosition)
        {
            UnloadVideo();

            readerWriterLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                videoReader = reader;
            }
            finally
            {
                readerWriterLock.ReleaseWriterLock();
            }

            framerate = fps;
            position = startPosition;
            UpdateVideo();
            InvokeOnPositionChanged();
        }

        public void UnloadVideo()
        {
            Stop();

            readerWriterLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                videoReader = null;
            }
            finally
            {
                readerWriterLock.ReleaseWriterLock();
            }

            lock (frameLock)
            {
                currentFrame?.Dispose();
                currentFrame = null;
            }

            position = 0;
            Invalidate();
        }
        #endregion

        #region Event Handlers
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateVideo();
        }

        private void VideoPlayerControl_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void VideoPlayerControl_Load(object sender, EventArgs e)
        {
            UpdateVideo();
        }
        #endregion

        #region Properties
        public int Position
        {
            get => position;
            set
            {
                if (SetPositionInternal(value))
                {
                    UpdateVideo();
                    InvokeOnPositionChanged();
                }
            }
        }

        public IVideoReader VideoReader => videoReader;

        public double Framerate
        {
            get => framerate;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "FPS cannot be zero or lower");
                framerate = value;
                if (isPlaying) Play();
            }
        }

        public int FrameCount
        {
            get
            {
                readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    IVideoReader reader = videoReader;
                    return reader?.FrameCount ?? 0;
                }
                finally
                {
                    readerWriterLock.ReleaseReaderLock();
                }
            }
        }

        public Padding CropMargin
        {
            get => cropMargin;
            set
            {
                cropMargin = value;
                Invalidate();
            }
        }

        public bool DisplayActualFramerate
        {
            get => displayActualFramerate;
            set
            {
                displayActualFramerate = value;
                Invalidate();
            }
        }

        public bool EnsureCorrectPlaybackSpeed
        {
            get => ensureCorrectPlaybackSpeed;
            set => ensureCorrectPlaybackSpeed = value;
        }

        public double SpeedUp
        {
            get => speedUp;
            set
            {
                speedUp = value;
                if (isPlaying) Play();
            }
        }

        public double ActualFramerate => actualFramerate;
        #endregion

        protected override void Dispose(bool disposing)
        {
            Stop();
            playTimer.Dispose();

            lock (frameLock)
            {
                currentFrame?.Dispose();
                currentFrame = null;
            }

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
