# Dawn

![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/insire/dawn) [![Build status](https://dev.azure.com/SoftThorn/Dawn/_apis/build/status/Dawn-CI)](https://dev.azure.com/SoftThorn/Dawn/_build/latest?definitionId=5) [![License](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/Insire/Dawn/blob/master/license.md)

A utility to quickly update a directories contents while automatically backing up all the new files and making them available as past updates/backups.

## Impressions

### First start

![Default screen](/screenshots/default.png)

### Main screen after some time

![Main screen](/screenshots/main_window.png)

### Staging some files for backup

![Main screen during staging](/screenshots/staging.png)

### Creating a new backup

![Backup creation in progress](/screenshots/actionshot.png)

### Backup restore in progress

![Backup restoration in progess](/screenshots/log.png)

## Features

- backup any file, folder or zip-archive (everything is just being copied to different directories)
- drag and drop anything on to the window
- stage files until you got everything that belongs into your backup
- auto filter files based on file extension
- configurable auto filter
- configurable backup directory and deployment directory
- go to folder
- live logging
- auto updates from github releases
- deleting, naming, renaming of backups
- comment support, if naming a backup is not enough
- injecting configuration from a web url, json file or cli args
- single file exe
- ready for production (i'm using this myself for my day to day work)
- 3rd party dependencies are attributed in the about section

## Development

You can compile this yourself, by running the ``build.ps1`` file on Windows. You need a recent version of Visual Studio 2019 or newer to be able to do that. You also need internet access and optionally git for cloning the respository.
