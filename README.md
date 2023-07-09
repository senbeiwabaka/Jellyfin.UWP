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
* Can choose audio stream now
* Can transcode

[Supported Codecs](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/supported-codecs)

### Not finished

* TV show episode page (needs work)
* Subtitle options
* Bitrate selection on playing video
* Some weird issues with video in full-screen on some videos
* No trailers, if present

Needs general UI cleanup

## Notes
Some forms of DTS just don't work but none of them play in UWP media player as the supported codecs says. Those will automatically be generate transcode URL.

## Local Setup
Jellyfin cert password `JellyfinPassword`

