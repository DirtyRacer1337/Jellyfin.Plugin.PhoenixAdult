# Manual Search Usage
Each search query can be comprised of *up to* 5 parts, depending on the supported [*search type*](./manualsearch.md#search-types-and-their-capabilities):
- `Site` - Either the shorthand abbreviation, or full site name.
- `Date` - Follows immediately after site name, in the format of either `YYYY-MM-DD` ([more on how this can be used](./manualsearch.md#search-types-and-their-capabilities))
- `Actor(s)`
- `Title` - The title/name of the scene.
- `StudioID` - A numeric value found in the URL of a studio page.
- `ActressID` - An alphanumeric value found in the URL of an actress page. Typically similar to the name of the actress.
- `SceneID` - A numeric value found in the URL of a scene page.
- `Direct URL` - A string of characters at the end of a URL of a scene page. Typically includes some combination of a SceneID, Scene Title, or Actor.

# Search types and their capabilities
There are 3 available search/matching methods, as listed below:
+ **Enhanced Search:** `Title` `Actor` `Date` `SceneID`
+ **Limited Search:** `Title` `Actor`
+ **Exact Match:** `StudioID` `ActressID` `SceneID` `Direct URL`

## Enhanced Search
#### Multi-search available.
+ **Available search methods:**
  - **Title**
  - **Actor(s)**
+ **Available match methods:**
*These can be used in conjunction with available search methods and will increase the possibility of locating the correct scene. However, it is not recommended they be used as standalone search terms. Usually, at least one of these can be utilized (depending on the site)*
  - **Date**
  - **SceneID**

+ **Date Match:** Date can be entered directly after the site name, but before any other search terms. This will increase the possibility for a match.
+ **SceneID Match:** SceneID can be entered directly after the site name (and date), but before all other search terms. This will increase the possibility for a match.

## Limited Search
#### Limited-search available.
+ **Available search methods:**
  - **Title**
  - **Actor(s)**

## Exact Match
#### No search available.
*Locating the correct scene is entirely dependent on entering the correct StudioID, ActressID, SceneID, or Direct URL. However, entering additional search terms may help with matching.*
+ **StudioID:** Typically used when sites host many small, independent studios (ie. Clips4Sale).
  - Can add the Date (before the StudioID).
  - Can add a Title/Actor (after the StudioID). This acts as a search term.
+ **ActressID:** Typically used when actresses have their own page, but scenes do not. 
  - Can add the Date (before the ActressID).
  - Can add a Title/Actor (after the ActressID). This acts as a search term.
+ **SceneID**
  - Can add the Date (before the SceneID).
  - Can add a Title/Actor (after the SceneID).
+ **Direct URL**
  - Can add the Date (before the URL).
  - Adding any additional terms (ie. Title/Actors) will cause issues with matching.

## Notes
+ **Date Add** - Some sites don't make release dates available. The agent will scrape the date from your filename/search term, instead.

# Search Examples
Depending on the capability of any one network/site, you can try a few combination of the above.

Here are some examples for each type of search:
+ **Enhanced Search** examples:
  - A full search, with all available details:
    - `SiteName` - `YY-MM-DD` - `SceneID` - `Jane Doe` - `An Interesting Plot`
  - A minimal search, with fewer details, but includes SceneID:
    - `SiteName` - `SceneID` - `Jane Doe`
  - A basic search with the most common details:
    - `SiteName` - `YY-MM-DD` - `An Interesting Plot`
  - Another minimal search, using site shorthand:
    - `SN` - `An Interesting Plot`

+ **Limited Search** examples:
  - A search using both actor and scene title:
    - `SiteName` - `Jane Doe` - `An Interesting Plot`
  - A search using site name and an actor from the scene:
    - `SiteName` - `Jane Doe`
  - A search using site shorthand with the scene title:
    - `SN` - `An Interesting Plot`

+ **Exact Match** examples:
  - An exact search using site name and ID:
    - `SiteName` - `SceneID`
  - An exact search using site shorthand and ID:
    - `SN` - `SceneID`
  - A direct url match, using only a suffix:
    - `SiteName` - `Direct URL`
      - `PornPros` - `eager-hands` (taken from the URL [https://pornpros.com/video/**eager-hands**](https://dereferer.me/?https%3A//pornpros.com/video/eager-hands))
    - `SiteName` - `YY-MM-DD` - `Direct URL`
      - `Mylf` - `2019-01-01` - `1809 manicured-milf-masturbation` (taken from the URL [https://www.mylf.com/movies/**1809/manicured-milf-masturbation**](https://dereferer.me/?https%3A//www.mylf.com/movies/1809/manicured-milf-masturbation))
      - `Wicked` - `2019-10-10` - `Stranger-Than-Fiction 77675` (taken from the URL [https://www.wicked.com/en/movie/**Stranger-Than-Fiction/77675**](https://dereferer.me/?https%3A//www.wicked.com/en/movie/Stranger-Than-Fiction/77675))
      - `Wicked` - `2019-10-10` - `Stranger Than Fiction Scene 1 167063` (taken from the URL [https://www.wicked.com/en/video/**Stranger-Than-Fiction-Scene-1/167063**](https://dereferer.me/?https%3A//www.wicked.com/en/video/Stranger-Than-Fiction-Scene-1/167063))
