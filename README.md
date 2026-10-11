# Fit  Overlay

Windows application that reads a Garmin activity file (.fit) and creates a video file with map, altitude graph and other activity data over a transparent background.

[![Release](https://img.shields.io/github/v/release/Florin-Purice/FitFileOverlay)](https://github.com/Florin-Purice/FitFileOverlay/releases)
![GitHub Release Date](https://img.shields.io/github/release-date/Florin-Purice/FitFileOverlay)
[![License](https://img.shields.io/github/license/Florin-Purice/FitFileOverlay
)](https://github.com/Florin-Purice/FitFileOverlay?tab=GPL-3.0-1-ov-file)

## Getting started

### Install

* Option 1

Download and install the [latest version](https://github.com/Florin-Purice/FitFileOverlay/releases/latest/download/FitFileOverlay-win-x64-net10-Setup.exe). It will install the required .NET version if it's missing.

* Option 2

Install the [self-contained](https://github.com/Florin-Purice/FitFileOverlay/releases/latest/download/FitFileOverlay-win-x64-Setup.exe) version which doesn't require .NET Runtime to be installed (bigger file size).

> [!IMPORTANT]
> This app requires ffmpeg to run. At startup it will check for a valid ffmpeg install and if one is not found it will prompt the user if he or she wants the app to automatically setup ffmpeg binaries. 

### Update

The app will check for updates at startup and prompt the user for installing them.  
Install/update process is handled by [velopack](https://velopack.io/).

## Usage

### 1. Load FIT file

> [!NOTE]
> When downloading an activity file from [Garmin Connect](https://connect.garmin.com/app/home), it is usually archived into a zip file. You need to unzip it first since Fit Overlay expects a .fit file.

When a file is loaded a preview of the overlay will be displayed. Use the slider at the bottom of the preview to see how the overlay will look at different points in the activity.

![Home](https://github.com/user-attachments/assets/906656a8-52e2-4959-aec6-208d275c8a27)

### 2. Crop activity duration

Trim the start and end of the activity, if needed, using sliders or by specifying the exact record indexes for more control. Useful for when you forgot to stop the activity recording on time or when you want to isolate a specific workout interval.  
The preview will reflect the changes in real time.

![Crop](https://github.com/user-attachments/assets/4c14fa8c-04d1-45b2-bebf-9f92c1b6940a)

### 3. Change the look of the overlay

Play with the settings to make the overlay look exactly how you want it to.  
Things that you can customize:

* Framerate of the output video. Higher values than the number of records per second of the .fit file (usually 1) means that intermediary frames will use interpolated data
* Choose between using custom value for lactate threshold heart rate or use the value read from the file
* Primary and secondary colors
* Background color (recommended to leave it fully transparent)
* Include/exclude sections of the overlay: map, altitude graph, various data fields; and the size of these sections
* Specific settings to change the look of the map and individual data fields
* Change the order in which data fields are drawn

To help you see the changes in real time you can open a preview window from the settings page.  
When you are satisfied with how the overlay looks you may want to save the settings as a template so you can reuse them some other time.

![Edit](https://github.com/user-attachments/assets/bdd8b1ae-ce08-482c-ad05-0a45235b76e9)

### 4. Export

Finally it's time to create the overlay video. Use the `Export video` button from Home page, choose a save location and a name for the file and start exporting.  
During export a progress bar will be displayed along with the time elapsed. Export can be cancelled at any time using the `Cancel export` button.

![Export](https://github.com/user-attachments/assets/02ded927-ceb8-4921-ab9a-ca3c7c8d2350)

> [!NOTE]
> Exporting process uses `prores` codec to preserve background transparency. This way the exported video can be used directly in the video editor without needing to key out the background. The downside is slower export and increased video size compared to .mp4 variant.  
> Video playback tested with [PotPlayer](https://potplayer.tv/) and video editing with [DaVinci Resolve](https://www.blackmagicdesign.com/products/davinciresolve). Compatibility with other programs not guaranteed.

## Examples

 Here are a few examples of videos using the overlay created by Fit Overlay.

<a href="https://www.youtube.com/watch?v=hz7Gj9dUl5w">
  <img src="https://img.youtube.com/vi/hz7Gj9dUl5w/maxresdefault.jpg" width="300" alt="Watch Video">
</a><br/>
<a href="https://www.youtube.com/watch?v=JstSYJi-Fho">
  <img src="https://img.youtube.com/vi/JstSYJi-Fho/maxresdefault.jpg" width="300" alt="Watch Video">
</a><br/>
<a href="https://www.youtube.com/watch?v=2SCQUztco2o">
  <img src="https://img.youtube.com/vi/2SCQUztco2o/maxresdefault.jpg" width="300" alt="Watch Video">
</a><br/>
<a href="https://www.youtube.com/watch?v=eQ8ZyxgEZJ8">
  <img src="https://img.youtube.com/vi/eQ8ZyxgEZJ8/maxresdefault.jpg" width="300" alt="Watch Video">
</a><br/>

> [!NOTE]
> The exported video from Fit Overlay has been applied over a recording, cropped, zoomed and added drop shadow using DaVinci Resolve.  
> Fit Overlay does not draw directly over an existing video, it creates a video that displays FIT data that can later be applied over another video using a video editor.
