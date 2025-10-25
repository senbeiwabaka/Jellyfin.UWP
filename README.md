# Jellyfin.UWP
----------------------------

[![Build and Tests](https://github.com/senbeiwabaka/Jellyfin.UWP/actions/workflows/main.yml/badge.svg)](https://github.com/senbeiwabaka/Jellyfin.UWP/actions/workflows/main.yml)
[![Quality Gate Status](https://sonarqube.mjy-home.duckdns.org/api/project_badges/measure?project=senbeiwabaka_Jellyfin.UWP_7dc6acd8-bf51-4fb5-b404-27af13d1269f&metric=alert_status&token=sqb_e8533c3b8185b5fe47cd1c2efd6d48bf9457c7bd)](https://sonarqube.mjy-home.duckdns.org/dashboard?id=senbeiwabaka_Jellyfin.UWP_7dc6acd8-bf51-4fb5-b404-27af13d1269f)

This is a UWP application for Windows store and Xbox to use Jellyfin.
The UI design and playback is similiar to default Jellyfin website
It is NOT hosting a browser version of the website.
This is a native UWP application.
This application will only be to watch media and not to administrate Jellyfin.

To note this will not have Live TV or Pictures.
If someone would like to add that capability, that would be cool.
I have neither so I can not build or test.

The primary purpose/use of this app is for the Xbox. 
There is another Windows based client that seems far better than this one.
This is being built with the default Windows media items.
In fact, this will operate more or less exactly like the windows media player that ships with Windows but plays your Jellyfin content.
The reason that is is because Xbox only has so many functions available.
The reason to even build this is because no one is maintaining the current Jellyfin UWP.
I would pick up maintaining that but to me that would be more of a nightmare versus just rolling this native one.


## Features
* Load your libraries
* Basic search
* Library page with basic filtering, paging, and sorting
* Specific media item page
* Media playback
* Can choose audio stream
* Can choose video stream
* Can choose subtitle
* If media can not direct play (codec doesn't exist for audio, video, or both) then it will play transcoding happening on Jellyfin side
* Can play next episode (doesn't respect settings)

[Supported Codecs](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/supported-codecs)

### Not finished

* No trailers, if present
* User settings (this is for the defaults you have choosen. One of them is being used but not the rest)

### Not going to work

* Bitrate selection
* While external links could work, not going to implement that as this is a tide over until a new platform tech is specified for C# on xbox where a better use of resources will happen

## Notes
DTS audio codec is a hit or miss whether or not it will work

## Local Setup
Local/side loading should work as the key needed to install it is there.

### Local Install
1. 

