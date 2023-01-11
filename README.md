# PhoenixAdult

This metadata provider helps fill Jellyfin/Emby with information for your adult videos by pulling from the original site

[![GPL 2.0 License](https://img.shields.io/github/license/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult)](./LICENSE)
[![Current Release](https://img.shields.io/github/release/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult)](https://github.com/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult/releases/latest)
[![Build status](https://img.shields.io/github/actions/workflow/status/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult/release.yml)](https://github.com/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult/releases/tag/nightly)

------------

## Install
- Repository (Jellyfin only):
  - Add to the list this URL `https://raw.githubusercontent.com/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult/master/manifest.json`
- Manual:
  - Download Archive from [Latest Release](https://github.com/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult/releases/latest)
  - Follow the [Instruction](https://jellyfin.org/docs/general/server/plugins/index.html)

## Features
- Scrapes any available Metadata, including:
  - Scene Title
  - Scene Summary
  - Studio
  - Release Date
  - Genres / Categories / Tags
  - PornStars
  - Movie Poster(s) / Background Art

## File Naming

### Here are some naming structures we recommend:
- `SiteName` - `YYYY-MM-DD` - `Scene Name` `.[ext]`
- `SiteName` - `Scene Name` `.[ext]`
- `SiteName` - `YYYY-MM-DD` - `Actor(s)` `.[ext]`
- `SiteName` - `Actor(s)` `.[ext]`

Real world examples:
- `Blacked - 2018-12-11 - The Real Thing.mp4`
- `Blacked - Hot Vacation Adventures.mp4`
- `Blacked - 2018-09-07 - Alecia Fox.mp4`
- `Blacked - Alecia Fox Joss Lescaf.mp4`

Some sites do not have a search function available. This is where SceneID and Direct URL come in to play.
See the [manual searching document](./docs/manualsearch.md) for more information.

#### If you would prefer to integrate SceneID's into your filenames, instead of manually matching in Jellyfin, here are some naming structures we recommend:
- `SiteName` - `YYYY-MM-DD` - `SceneID` `.[ext]`
- `SiteName` - `SceneID` `.[ext]`
- `SiteName` - `SceneID` - `Scene Name` `.[ext]`

Real world examples:
- `EvilAngel - 2016-10-02 - 119883` (taken from the URL [https://www.evilangel.com/en/video/Allie--Lilys-Slobbery-Anal-Threesome/**119883**](https://www.evilangel.com/en/video/Allie--Lilys-Slobbery-Anal-Threesome/119883))
- `MomsTeachSex - 314082` (taken from the URL [https://momsteachsex.com/tube/watch/**314082**](https://momsteachsex.com/tube/watch/314082))
- `Babes - 3075191 - Give In to Desire` (taken from the URL [https://www.babes.com/scene/**3075191**/1](https://www.babes.com/scene/3075191/1))

## Supported Networks
To view the full list of supported sites, [check out the sitelist doc](./docs/sitelist.md).
