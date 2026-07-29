# MeGUI Linux and Wine Compatibility Fork

This is a customized fork of MeGUI focused on fixing UI bugs, scaling issues, and playback stability when running under Wine or Proton on Linux. For general information about MeGUI, visit the official project page at https://sourceforge.net/projects/megui/ or the upstream repository at https://github.com/Kurtnoise-zeus/megui.

## What Has Been Enhanced in This Version

- Fixed video preview window collapsing and freeze bugs under Wine and Proton
- Restored smooth asynchronous frame seeking in the video player without freezing the user interface
- Fixed aspect ratio calculation and dynamic window resizing when maximizing or zooming the preview window
- Updated real-time encoding progress calculations and log output tracking
- Integrated updated MediaInfo libraries for improved media indexing under Linux Wine prefixes

## Building

Open MeGUI.sln in Visual Studio or compile under Wine using MSBuild to build the project binary.
