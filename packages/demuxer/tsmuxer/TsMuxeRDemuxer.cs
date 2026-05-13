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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using MeGUI.core.util;

namespace MeGUI
{
    class TsMuxeRDemuxer : CommandlineJobProcessor<TsMuxeRDemuxJob>
    {
        public static readonly JobProcessorFactory Factory =
            new JobProcessorFactory(new ProcessorFactory(init), "TsMuxeRDemuxer");

        private static IJobProcessor init(MainForm mf, Job j)
        {
            if (j is TsMuxeRDemuxJob)
                return new TsMuxeRDemuxer(mf.Settings.TSMuxer.Path);
            return null;
        }

        private string metaFile = null;

        public TsMuxeRDemuxer(string executablePath)
        {
            UpdateCacher.CheckPackage("tsmuxer");
            this.Executable = executablePath;
        }

        protected override void checkJobIO()
        {
            Su.Status = "Demuxing...";
            generateMetaFile();
            Util.ensureExists(metaFile);
            base.checkJobIO();
        }

        public override void ProcessLine(string line, StreamType stream, ImageType oType)
        {
            if (Regex.IsMatch(line, @"^[0-9]{1,3}\.[0-9]{1}%", RegexOptions.Compiled))
            {
                Su.PercentageCurrent = getPercentage(line);
                return;
            }

            if (stream == StreamType.Stderr || line.ToLowerInvariant().Contains("error"))
                oType = ImageType.Error;
            else if (line.ToLowerInvariant().Contains("warning"))
                oType = ImageType.Warning;
            base.ProcessLine(line, stream, oType);
        }

        private decimal? getPercentage(string line)
        {
            try
            {
                string[] strPercentage = line.Split('%')[0].Split('.');
                return Convert.ToDecimal(strPercentage[0], new System.Globalization.CultureInfo("en-US"))
                     + Convert.ToDecimal(strPercentage[1], new System.Globalization.CultureInfo("en-US")) / 10;
            }
            catch (Exception e)
            {
                log.LogValue("Exception in getPercentage(" + line + ")", e, ImageType.Warning);
                return null;
            }
        }

        protected override string Commandline
        {
            get
            {
                return " \"" + metaFile + "\"" + " \"" + Job.OutputPath + "\"";
            }
        }

        private void generateMetaFile()
        {
            metaFile = Path.Combine(Job.OutputPath, Path.GetFileNameWithoutExtension(Job.Input) + ".meta");

            using (StreamWriter sw = new StreamWriter(metaFile, false, Encoding.Default))
            {
                sw.WriteLine("MUXOPT --demux");

                using (MediaInfoFile oInfo = new MediaInfoFile(Job.Input, ref log))
                {
                    if (oInfo.HasVideo && oInfo.VideoInfo.Track != null)
                    {
                        foreach (TrackInfo track in Job.Tracks)
                        {
                            if (track.TrackType == TrackType.Video)
                            {
                                string vcodecID = getVideoCodecId(oInfo);
                                if (!string.IsNullOrEmpty(vcodecID))
                                    sw.WriteLine(vcodecID + ", \"" + Job.Input + "\", track=" + track.TrackID);
                            }
                        }
                    }

                    foreach (TrackInfo track in Job.Tracks)
                    {
                        if (track.TrackType == TrackType.Audio)
                        {
                            string acodecID = getAudioCodecId(track);
                            if (!string.IsNullOrEmpty(acodecID))
                                sw.WriteLine(acodecID + ", \"" + Job.Input + "\", track=" + track.TrackID);
                        }
                    }

                    foreach (TrackInfo track in Job.Tracks)
                    {
                        if (track.TrackType == TrackType.Subtitle)
                        {
                            sw.WriteLine("S_HDMV/PGS, \"" + Job.Input + "\", track=" + track.TrackID);
                        }
                    }
                }
            }

            Job.FilesToDelete.Add(metaFile);
        }

        private string getVideoCodecId(MediaInfoFile oInfo)
        {
            if (oInfo.VideoInfo.Codec == VideoCodec.AVC)
                return "V_MPEG4/ISO/AVC";
            else if (oInfo.VideoInfo.Codec == VideoCodec.HEVC)
                return "V_MPEGH/ISO/HEVC";
            else if (oInfo.VideoInfo.Codec == VideoCodec.MPEG2)
                return "V_MPEG-2";
            else if (oInfo.VideoInfo.Codec == VideoCodec.VC1)
                return "V_MS/VFW/WVC1";
            return null;
        }

        private string getAudioCodecId(TrackInfo track)
        {
            string codec = track.Codec.ToUpperInvariant();
            if (codec.Contains("AC3") || codec.Contains("AC-3"))
                return "A_AC3";
            else if (codec.Contains("DTS"))
                return "A_DTS";
            else if (codec.Contains("AAC"))
                return "A_AAC";
            else if (codec.Contains("LPCM") || codec.Contains("PCM"))
                return "A_LPCM";
            else if (codec.Contains("TRUEHD"))
                return "A_TRUEHD";
            else if (codec.Contains("EAC3") || codec.Contains("E-AC-3"))
                return "A_EAC3";
            return "A_AC3";
        }
    }
}
