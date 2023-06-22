# Jellyfin.UWP
----------------------------

This is a UWP application for Windows store and Xbox to use Jellyfin. It should operate very similiar to the website. It is NOT hosting a browser version of the website. This is a native UWP application. This application will only be to watch media and not to administrate Jellyfin.

To note this will not have Live TV or Pictures. If someone would like to start adding that to this, that would be cool. I have neither so I can not build or test.

## Features
* Load your libraries
* Basic search/filtering
* Library page with basic filtering and paging
* Specific media item page (mostly there, see `Not finished` below)
* Media playback (see `Not finished` below)

[Supported Codecs](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/supported-codecs)

### Not finished

* TV show main play button
* TV show episode page (needs work)
* Audio options
	* DTS is not playable through UWP MediaPlayer (see transcode below) 
* Subtitle options
* Bitrate selection on playing video
* Getting more video options to correctly display controls
* Transcoding
* Some weird issues with video in full-screen on some videos
