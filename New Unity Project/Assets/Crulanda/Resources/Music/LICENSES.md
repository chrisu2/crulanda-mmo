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
