PhoenixAdult
===========================
This metadata provider helps fill Jellyfin with information for your adult videos by pulling from the original site.

Features
--------
Currently the features of this metadata agent are:
- Scrapes any available Metadata, including:
  - Scene Title
  - Scene Summary
  - Studio
  - Originating Site / Subsite / Site Collection
  - Release Date
  - Genres / Categories / Tags
  - PornStars
  - Scene Director(s)
  - Movie Poster(s) / Background Art

File Naming
-----------
The agent will try to match your file automatically, usually based on the filename. You can assist it by renaming your video appropriately.
If the video is not successfully matched, you can try to manually match it using the [Identify] function in Jellyfin. See the [manual searching document](./docs/manualsearch.md) for more information.
Best practice for each site is listed in the [sitelist document](./docs/sitelist.md).

#### Here are some naming structures we recommend:
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
These usually don't make the most intuitive filenames, so it is often better to use the [Identify] function in Jellyfin. See the [manual searching document](./docs/manualsearch.md) for more information.

#### If you would prefer to integrate SceneIDs into your filenames, instead of manually matching in Jellyfin, here are some naming structures we recommend:
- `SiteName` - `YYYY-MM-DD` - `SceneID` `.[ext]`
- `SiteName` - `SceneID` `.[ext]`
- `SiteName` - `SceneID` - `Scene Name` `.[ext]`

Real world examples:
- `EvilAngel - 2016-10-02 - 119883` (taken from the URL [https://www.evilangel.com/en/video/Allie--Lilys-Slobbery-Anal-Threesome/**119883**](https://www.evilangel.com/en/video/Allie--Lilys-Slobbery-Anal-Threesome/119883))
- `MomsTeachSex - 314082` (taken from the URL [https://momsteachsex.com/tube/watch/**314082**](https://momsteachsex.com/tube/watch/314082))
- `Babes - 3075191 - Give In to Desire` (taken from the URL [https://www.babes.com/scene/**3075191**/1](https://www.babes.com/scene/3075191/1))

## Build Process
1. Clone or download this repository
2. Ensure you have .NET Core SDK setup and installed
3. Build plugin with following command.
```sh
dotnet publish --configuration Release --output bin
```
4. Place the resulting file in the `plugins` folder under the program data directory or inside the portable install directory

Supported Networks
------------------
To view the full list of supported sites, [check out the sitelist doc](./docs/sitelist.md).
