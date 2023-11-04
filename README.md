# Jellyfin.UWP
----------------------------

[![Build and Tests](https://github.com/senbeiwabaka/Jellyfin.UWP/actions/workflows/main.yml/badge.svg)](https://github.com/senbeiwabaka/Jellyfin.UWP/actions/workflows/main.yml)
[![Quality Gate Status](https://sonarqube.mjy-home.duckdns.org/api/project_badges/measure?project=senbeiwabaka_Jellyfin.UWP_AYuW2VkcOTfeuyv7RdaP&metric=alert_status&token=sqb_6c07531063c194729707722741753a732644e274)](https://sonarqube.mjy-home.duckdns.org/dashboard?id=senbeiwabaka_Jellyfin.UWP_AYuW2VkcOTfeuyv7RdaP)

This is a UWP application for Windows store and Xbox to use Jellyfin.
It should operate very similiar to the website.
It is NOT hosting a browser version of the website.
This is a native UWP application.
This application will only be to watch media and not to administrate Jellyfin.

To note this will not have Live TV or Pictures.
If someone would like to start adding that to this, that would be cool.
I have neither so I can not build or test.

As an additional note, this only works on Windows 11 and above as certain elements were only added in 11.
This should not be a problem as 10 is losing support soon enough.

## Features
* Load your libraries
* Basic search/filtering
* Library page with basic filtering and paging
* Specific media item page (mostly there, see `Not finished` below)
* Media playback (see `Not finished` below)
* Can choose audio stream
* Can transcode (from jellyfin itself, does not transcode itself)

[Supported Codecs](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/supported-codecs)

### Not finished

* TV show episode page (needs work)
* Subtitle options
* Bitrate selection on playing video (may not be possible)
* Some weird issues with video in full-screen on some videos
* No trailers, if present

Needs general UI cleanup

## Notes
Some forms of DTS just don't work but none of them play in UWP media player as the supported codecs says.
Those will automatically generate a transcode URL.

## Local Setup
Local/side loading is not yet working.

Jellyfin cert password `JellyfinPassword`

