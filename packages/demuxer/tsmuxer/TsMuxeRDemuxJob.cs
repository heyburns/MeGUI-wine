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
using System.Collections.Generic;

namespace MeGUI
{
    /// <summary>
    /// Container object for tsMuxeR demuxing
    /// </summary>
    public class TsMuxeRDemuxJob : Job
    {
        private List<TrackInfo> _tracks;
        private string _outputPath;

        public TsMuxeRDemuxJob()
        {
        }

        public TsMuxeRDemuxJob(string input, string outputPath, List<TrackInfo> tracks)
            : base(input, null)
        {
            this._tracks = tracks;
            this._outputPath = outputPath;
        }

        public override string CodecString
        {
            get { return "tsmuxer"; }
        }

        public override string EncodingMode
        {
            get { return "ext"; }
        }

        public List<TrackInfo> Tracks
        {
            get { return _tracks; }
            set { _tracks = value; }
        }

        public string OutputPath
        {
            get { return _outputPath; }
            set { _outputPath = value; }
        }
    }
}
