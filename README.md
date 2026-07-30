# MeGUI Linux and Wine Compatibility Fork

This is a customized fork of MeGUI focused on fixing UI bugs, scaling issues, and playback stability when running under Wine or Proton on Linux. For general information about MeGUI, visit the official project page at https://sourceforge.net/projects/megui/ or the upstream repository at https://github.com/Kurtnoise-zeus/megui.

## What Has Been Enhanced in This Version

- Fixed video preview window collapsing and freeze bugs under Wine and Proton
- Restored smooth asynchronous frame seeking in the video player without freezing the user interface
- Fixed aspect ratio calculation and dynamic window resizing when maximizing or zooming the preview window
- Updated real-time encoding progress calculations and log output tracking
- Integrated updated MediaInfo libraries for improved media indexing under Linux Wine prefixes
- Disabled auto-update checks by default to keep bundled tools stable

## Linux Wine Prefix Installation Guide

Follow these steps to set up a dedicated 64-bit Wine prefix for MeGUI on Linux.

### 1. Create a 64-bit Wine Prefix

Open a terminal and create a dedicated Wine prefix for MeGUI:

WINEPREFIX=~/.local/share/wineprefixes/megui WINEARCH=win64 winecfg

### 2. Install Required Dependencies with Winetricks

MeGUI requires Microsoft .NET Framework 4.8 and the Visual C++ redistributable runtimes. Install them into your prefix using winetricks:

winetricks -q --self-update
WINEPREFIX=~/.local/share/wineprefixes/megui winetricks -q dotnet48 vcrun2015 corefonts

### 3. Extract and Launch MeGUI

Download and extract the latest MeGUI release zip into your desired folder, then launch MeGUI using Wine:

WINEPREFIX=~/.local/share/wineprefixes/megui wine /path/to/extracted/MeGUI.exe

## Building

Open MeGUI.sln in Visual Studio or compile under Wine using MSBuild to build the project binary.

## License

This project is licensed under the GNU General Public License v3.0 (GPLv3). See the LICENSE file for full license terms.
