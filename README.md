# Jellyfin.UWP
----------------------------

[![Build and Tests](https://github.com/senbeiwabaka/Jellyfin.UWP/actions/workflows/main.yml/badge.svg)](https://github.com/senbeiwabaka/Jellyfin.UWP/actions/workflows/main.yml)
[![Quality Gate Status](https://sonarqube.mjy-home.duckdns.org/api/project_badges/measure?project=senbeiwabaka_Jellyfin.UWP_7dc6acd8-bf51-4fb5-b404-27af13d1269f&metric=alert_status&token=sqb_e8533c3b8185b5fe47cd1c2efd6d48bf9457c7bd)](https://sonarqube.mjy-home.duckdns.org/dashboard?id=senbeiwabaka_Jellyfin.UWP_7dc6acd8-bf51-4fb5-b404-27af13d1269f)

This is a UWP application for Windows store and Xbox to use Jellyfin.
It should operate very similiar to the website.
It is NOT hosting a browser version of the website.
This is a native UWP application.
This application will only be to watch media and not to administrate Jellyfin.

To note this will not have Live TV or Pictures.
If someone would like to start adding that to this, that would be cool.
I have neither so I can not build or test.

The primary purpose/use of this app is for the Xbox. 
There is another Windows based client that seems far better than this one.
This is being built with the default Windows media items.
In fact, this will operate more or less exactly like the windows media player that ships with new Windows.
The reason that is is because Xbox only has so many functions available.
The reason to even build this is because no one is maintaining the current Jellyfin UWP.
I would pick up maintaining that but to me that would be more of a nightmare versus just rolling this native one.


## Features
* Load your libraries
* Basic search/filtering
* Library page with basic filtering and paging
* Specific media item page
* Media playback
* Can choose audio stream
* Can choose video stream
* Can transcode (from jellyfin itself, does not transcode itself)

[Supported Codecs](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/supported-codecs)

### Not finished

* Some subtitle things to work out
* No trailers, if present
* User settings (this is for the defaults you have choosen. One of them is being used but not the rest)

### Not going to work

* Bitrate selection
* While external links could work, not going to implement that as this is a tide over until a new platform tech is specified for C# on xbox where a better use of resources will happen

## Notes
Some forms of DTS just don't work but none of them play in UWP media player as the supported codecs says.
Those will automatically generate a transcode URL.

## Local Setup
Local/side loading is not yet working.

