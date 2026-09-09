# Fit  Overlay

Windows application that reads a Garmin activity file (.fit) and creates a video file with GPS and other activity data.

[![Release](https://img.shields.io/github/v/release/Florin-Purice/FitFileOverlay)](https://github.com/Florin-Purice/FitFileOverlay/releases)
![GitHub Release Date](https://img.shields.io/github/release-date/Florin-Purice/FitFileOverlay)
[![License](https://img.shields.io/github/license/Florin-Purice/FitFileOverlay
)](https://github.com/Florin-Purice/FitFileOverlay?tab=GPL-3.0-1-ov-file)

## Getting started

### Option 1

Download the zip of the latest [version](https://github.com/Florin-Purice/FitFileOverlay/releases), extract it into its own folder and run.  
Requires [.NET Runtime](https://dotnet.microsoft.com/en-us/download/dotnet) to be installed (e.g. `FitOverlay_net10_win-x64.zip` needs .NET 10).

### Option 2

Download the `selfcontained` version which doesn't require .NET Runtime to be installed.

> [!IMPORTANT]
> This app makes use of [`FFMpegCore`](https://github.com/rosenbjerg/FFMpegCore) which requires either:
> * an [install of ffmpeg](https://www.gyan.dev/ffmpeg/builds/) - recommended, install with `winget install ffmpeg`
> * or a directory containing ffmpeg/ffprobe binaries and a config file that points at that directory ([release 1.0.0](https://github.com/Florin-Purice/FitFileOverlay/releases/tag/1.0.0) contains `ffmpeg_fix.zip`, extract it into the executing directory if you don't want to install ffmpeg).

## Usage

### 1. Load .fit file

![Load](https://github.com/user-attachments/assets/58c299ad-5c9b-495c-bc24-abaed500c088)

> [!NOTE]
> When downloading an activity file from [Garmin Connect](https://connect.garmin.com/app/home), it is usually archived into a zip file. You need to unzip it first since Fit Overlay expects a .fit file.

![Home](https://github.com/user-attachments/assets/15b016fd-9007-49f5-a335-b552908b85d5)

After the file is loaded a preview of the overlay will be displayed. Use the slider at the bottom of the preview to see how the overlay will look at different points in the activity.

### 2. Change the settings

![Settings button](https://github.com/user-attachments/assets/f2608436-d23b-4da6-935a-0b96bf504f03)

Play with the settings to make the overlay look exactly how you want it to.  
Things that you can customize:

* Framerate of the output video. Higher values than the number of records per second of the .fit file (usually 1) means that intermediary frames will use interpolated data
* Choose between using custom value for lactate threshold heart rate or use the value read from the file
* Primary and secondary colors
* Background color (recommended to leave it fully transparent)
* Include/exclude sections of the overlay: map, various data fields; and the size of these sections
* Specific settings to change the look of the map and individual data fields
* Change the order in which data fields are drawn

To help you see the changes in real time you can open a preview window from the settings page.  
When you are satisfied with how the overlay looks you may want to save the settings as a template.

![Save/Load Template, Preview Window](https://github.com/user-attachments/assets/7ae2c806-aa4f-4d80-99d4-55f313ed0307)

### 3. Export

![Export button](https://github.com/user-attachments/assets/5516bb07-8428-4e06-bcc4-a3b07021878d)

Finally it's time to create the overlay video. Use the `Export video` button from Home page, choose a save location and a name for the file and start exporting.  
During export a progress bar will be displayed along with the time elapsed. Export can be cancelled at any time using the relevant button.

![Export](https://github.com/user-attachments/assets/3568dac7-c541-471a-bc56-5c89dacb23da)

> [!NOTE]
> Exporting process uses prores codec to preserve background transparency. This way the exported video can be used directly in the video editor without needing to key out the background (tested in Davinci Resolve). The downside is slower export and increased video size compared to .mp4 variant.

```cs
await FFMpegArguments.FromPipeInput(framesSource)
    .OutputToFile(outputFilename, true, opt => opt
        .WithFramerate(Settings.FPS)
        .WithVideoCodec("prores_ks")
        .ForcePixelFormat("yuva444p10le")
        .WithCustomArgument("-profile:v 4444")
        .WithConstantRateFactor(17))
    .CancellableThrough(cancellationToken ?? new CancellationToken())
    .ProcessAsynchronously(throwOnError: true);
 ```

## Examples

 Here are a few examples of videos using the overlay created by Fit Overlay.

<a href="https://www.youtube.com/watch?v=JstSYJi-Fho">
  <img src="https://img.youtube.com/vi/JstSYJi-Fho/maxresdefault.jpg" width="300" alt="Watch Video">
</a>

<a href="https://www.youtube.com/watch?v=2SCQUztco2o">
  <img src="https://img.youtube.com/vi/2SCQUztco2o/maxresdefault.jpg" width="300" alt="Watch Video">
</a>

<a href="https://www.youtube.com/watch?v=eQ8ZyxgEZJ8">
  <img src="https://img.youtube.com/vi/eQ8ZyxgEZJ8/maxresdefault.jpg" width="300" alt="Watch Video">
</a>

> [!NOTE]
> The exported video from Fit Overlay has been applied over a recording, cropped, zoomed and added drop shadow using Davinci Resolve.  
> Fit Overlay does not draw directly over an existing video, it creates a video that displays FIT data that can later be applied over another video using a video editor.
