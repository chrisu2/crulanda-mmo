# Music: sources and terms

The tracks under `Resources/Music` come from three packs Chris bought and put in Downloads on 2026-10-15 01:20 ("I have some
music: dark realm, eastern life, last zone"): `dark-realm-music-pack.zip`, `eastern-life-music-pack.zip`,
`last-zone-music-pack.zip`, all by **ithappy studios** (ithappystudios.com, support@ithappystudios.com). Each pack carries the
same `Music_License.pdf` (dated 2026-04-01). Its terms, in full:

**Music License Certificate.** "This audio pack contains multiple music tracks provided under a Royalty-Free License."

You are allowed to:
- use the included audio tracks in personal and commercial projects;
- use in games, applications, videos, and other interactive media;
- modify, edit, and adapt the audio tracks as needed.

You are NOT required to:
- provide attribution or credit the author.

You are NOT allowed to:
- resell, sublicense, or redistribute any of the audio tracks as standalone files;
- distribute the audio tracks as part of another audio/music pack, library, or stock collection;
- claim authorship or ownership of the original audio tracks.

Important: the license applies to all audio files included in the pack; the audio may be used only as part of a larger project
(a game, application or video). Ownership: all rights to the original audio remain with ithappy studios.

What that means for this repo: the WAVs may ship inside the game's builds, and may be cut or re-encoded; they must not be
offered on their own (the GitHub repo is private; keep it so, and never publish the `Resources/Music` folder by itself).

## The tracks brought in (13 of 28; 48 kHz 16-bit stereo WAV as sold; Unity compresses them to Vorbis at import)

| Folder | File | Pack track | Length | Used for |
|---|---|---|---|---|
| Dark | Cemetery.wav | dark realm 1, cemetery | 4:08 | Khaven by day; Oakhaven by night |
| Dark | Crypt.wav | dark realm 4, crypt | 2:31 | the Sealed Adit and every cave |
| Dark | ScaryCrypt.wav | dark realm 7, scary crypt | 4:14 | the Sealed Adit and every cave |
| Eastern | Oasis.wav | eastern life 7, oasis | 4:49 | Oakhaven and the Verdant Shore by day |
| Eastern | DesertSunset.wav | eastern life 5, desert sunset | 4:06 | the Ashland Rim by day |
| Eastern | MysteriousCity.wav | eastern life 6, mysterious city | 5:20 | the Shattered Peaks by day |
| Eastern | RestlessNight.wav | eastern life 9, restless night | 4:04 | the Peaks by day and night; the Shore by night |
| Eastern | MeasuredFight.wav | eastern life 2, measured fight | 4:14 | the field's fight track |
| Eastern | AggressiveFight.wav | eastern life 1, aggressive fight | 3:52 | the dungeons' fight track |
| Last | LastHope.wav | last zone 6, last hope | 4:20 | Oakhaven and the Shore by day |
| Last | RustAndSilence.wav | last zone 7, rust and silence | 3:54 | Khaven |
| Last | EchoesAfterFire.wav | last zone 3, echoes after fire | 3:14 | the Rim by day; the grey's edge in Oakhaven and on the Rim |
| Last | ForgottenSouls.wav | last zone 4, forgotten souls | 5:14 | the Rim by day and night |

The sets by zone are in `Scripts/World/ZoneMusic.cs` (`ZoneMusic.Sets`); `Tests/EditMode/ZoneMusicTests.cs` checks every path is a
clip here. The other fifteen tracks stay in the zips in Downloads (vampire ball, abandoned mansion, classical horror, old cemetery,
old school horror, sense of fear, true fear; scorching sun, arabian bazaar, desert haze, oriental dancing; broken toy, desolate
pulse, frightening loneliness, technological ruins).

The Fantasy FREE props under `Resources/Props/Fantasy2` are ithappy's too, from the Unity Asset Store (`Fantasy_Free_fbx.zip`,
`Unity_6_Fantasy_Free_v2.1.unitypackage`), under the Asset Store EULA like the other kits.

## Fantasy3: alkakrab, Free 25 Fantasy RPG Tracks Vol. 3 (2026-10-19, play note 251)

Play note 251 (Chris, verbatim): "more music tracks for in game. myassets Free 25 Fantasy RPG Tracks Vol. 3". From the Unity Asset
Store (publisher **alkakrab**; `Asset Store-5.x\alkakrab\AudioMusic\Free 25 Fantasy RPG Tracks Vol 3.unitypackage`), a free
package under the **Unity Asset Store EULA** (Standard Unity Asset Store End User License): the audio may be used and edited inside
the game and ship in its builds; it may not be resold or redistributed as audio on its own. The package carries no licence or
readme file of its own. Same rule as above: the repo stays private, and `Resources/Music` is never published by itself.

25 of its 28 pieces are taken (`tools/import_music_vol3.py`, which extracts them from the package; the WAVs are not in git, the
`.meta` files are, with the package's own GUIDs). Left in the package: the one-shot copies of Ambient 1-10 and Action 1-5 (the
loops are the same music) and Fx 1-3 (stings; nothing needs one yet). **Edited:** the ambient, ambience and dark pieces are mixed
20-40 dB under everything else (RMS .001-.03 against ~.15 for the ithappy packs), so each was raised toward an RMS of .12 with
its peak held to .9, and written as 16-bit (the action loops and Ambient 3 and 4 were already at level and are unchanged). Import:
Vorbis, quality .5, streamed (`Editor/ThirdPartyImport.cs`, `OnPreprocessAudio`).

| File (Fantasy3/) | Pack piece | Length | Used for |
|---|---|---|---|
| Ambient1Loop, Ambient6Loop | Loops/Ambient 1, 6 | 2:24, 2:12 | the Shattered Peaks by day |
| Ambient2Loop, Ambient7Loop | Loops/Ambient 2, 7 | 1:54, 2:10 | Oakhaven by day |
| Ambient3Loop | Loops/Ambient 3 | 1:47 | Khaven |
| Ambient4Loop | Loops/Ambient 4 | 2:02 | the Ashland Rim by day |
| Ambient5Loop, Ambient8Loop | Loops/Ambient 5, 8 | 2:08, 2:06 | the Crown of Argentis by day |
| Ambient9Loop, Ambient10Loop | Loops/Ambient 9, 10 | 2:11, 2:09 | the Verdant Shore by day (the airiest two) |
| Ambience1 | Tracks/Ambience 1 | 1:06 | the Shore by night |
| Ambience2 | Tracks/Ambience 2 | 1:00 | Khaven |
| Ambience3 | Tracks/Ambience 3 | 0:54 | the Rim by night |
| Ambience4 | Tracks/Ambience 4 | 0:59 | Oakhaven by night |
| Ambience5 | Tracks/Ambience 5 | 0:54 | the Lowtowns by night |
| Dark1, Dark3 | Tracks/Dark 1, 3 | 0:52, 0:56 | the Sealed Adit |
| Dark2, Dark4 | Tracks/Dark 2, 4 | 0:40, 0:46 | Hallow's Creek's cellars (its streets stay silent) |
| Dark5 | Tracks/Dark 5 | 0:49 | the Lowtowns by day |
| Action1Loop, Action4Loop | Loops/Action 1, 4 | 1:26, 1:36 | fights in the field (with MeasuredFight) |
| Action3Loop, Action5Loop | Loops/Action 3, 5 | 1:36, 1:36 | fights under ground (with AggressiveFight) |
| Action2Loop | Loops/Action 2 | 1:51 | kept for the world boss raid (the Unraveller, `Docs/WORLD_BOSS_1.md`; not built yet) |