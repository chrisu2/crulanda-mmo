"""Bring alkakrab's "Free 25 Fantasy RPG Tracks Vol. 3" into Resources/Music/Fantasy3 (play note 251, 2026-10-19).

The WAVs are not in git (Resources/Music/.gitignore: *.wav; ~480 MB). Their .meta files are, with the package's own GUIDs and the
import settings below (Vorbis, quality .5, streaming), so a fresh checkout needs only this run to have the music back:

    python tools/import_music_vol3.py ["<path to the .unitypackage>"] ["<project root>"]   (from the repo root; needs numpy)

Only the 25 tracks the game uses are taken (the ten ambient loops, the five ambience and five dark tracks, the five action loops),
each re-levelled (relevel); the one-shot copies of the ambient and action tracks and the three Fx stings stay in the package.
"""
import os
import re
import sys
import tarfile
import wave

PACKAGE = r"C:\Users\ChrisWin11\AppData\Roaming\Unity\Asset Store-5.x\alkakrab\AudioMusic\Free 25 Fantasy RPG Tracks Vol 3.unitypackage"
HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEST = os.path.join("New Unity Project", "Assets", "Crulanda", "Resources", "Music", "Fantasy3")
ROOT = "Assets/Free 25 Fantasy RPG Tracks Vol. 3/"

# package path (under ROOT) -> file name under Resources/Music/Fantasy3
TAKE = {}
for i in range(1, 11):
    TAKE["Loops/Ambient %d Loop.wav" % i] = "Ambient%dLoop.wav" % i
for i in range(1, 6):
    TAKE["Tracks/Ambience %d.wav" % i] = "Ambience%d.wav" % i
    TAKE["Tracks/Dark %d.wav" % i] = "Dark%d.wav" % i
    TAKE["Loops/Action %d Loop.wav" % i] = "Action%dLoop.wav" % i

# The import rule (Editor/MusicImport.cs sets the same on every re-import).
TARGET_RMS, PEAK = .12, .9
SETTINGS = {"loadType": "2", "compressionFormat": "1", "quality": "0.5", "preloadAudioData": "0", "loadInBackground": "1"}


def fix_meta(text):
    for k, v in SETTINGS.items():
        text = re.sub(r"(?m)^(\s*%s:\s*).*$" % re.escape(k), lambda m: m.group(1) + v, text)
    return text


def relevel(src, out):
    """Write the track as 16-bit PCM, raised to the ithappy packs' level. The pack's ambient and dark pieces are mixed 20-40 dB
    under the action loops and the three older packs (RMS .001-.03 against ~.15), so they would be lost under the field's sounds at
    the same /music level: each is lifted toward an RMS of .12, never past a peak of .9 (the licence allows editing). Returns the gain."""
    import numpy as np   # in the user's site-packages: run this without -I
    w = wave.open(src)
    ch, sw, sr, nf = w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getnframes()
    raw = w.readframes(nf)
    if sw == 2:
        x = np.frombuffer(raw, "<i2").astype(np.float32) / 32768
    elif sw == 3:
        a = np.frombuffer(raw, np.uint8).reshape(-1, 3).astype(np.int32)
        x = (((a[:, 0] | (a[:, 1] << 8) | (a[:, 2] << 16)) << 8) >> 8).astype(np.float32) / 8388608
    else:
        raise ValueError("unexpected sample width %d in %s" % (sw, out))
    rms, peak = float(np.sqrt((x * x).mean())), float(np.abs(x).max())
    gain = max(1.0, min(TARGET_RMS / max(rms, 1e-6), PEAK / max(peak, 1e-6)))
    y = np.clip(np.round(x * gain * 32767), -32768, 32767).astype("<i2")
    o = wave.open(out, "wb")
    o.setnchannels(ch); o.setsampwidth(2); o.setframerate(sr); o.writeframes(y.tobytes()); o.close()
    return gain


def main():
    pkg = sys.argv[1] if len(sys.argv) > 1 else PACKAGE
    root = sys.argv[2] if len(sys.argv) > 2 else HERE
    dest = os.path.join(root, DEST)
    os.makedirs(dest, exist_ok=True)
    by_guid = {}
    with tarfile.open(pkg, "r:gz") as t:
        members = t.getmembers()
        for m in members:
            parts = m.name.strip("./").split("/")
            if len(parts) == 2 and parts[1] == "pathname":
                path = t.extractfile(m).read().decode("utf-8").splitlines()[0]
                if path.startswith(ROOT) and path[len(ROOT):] in TAKE:
                    by_guid[parts[0]] = TAKE[path[len(ROOT):]]
        done = 0
        for m in members:
            parts = m.name.strip("./").split("/")
            if len(parts) != 2 or parts[0] not in by_guid or not m.isfile():
                continue
            name = by_guid[parts[0]]
            if parts[1] == "asset":
                with t.extractfile(m) as src:
                    gain = relevel(src, os.path.join(dest, name))
                print("  %-18s x%.1f" % (name, gain))
                done += 1
            elif parts[1] == "asset.meta":
                out = os.path.join(dest, name + ".meta")
                if not os.path.exists(out):   # the committed meta wins; the package's GUID either way
                    text = fix_meta(t.extractfile(m).read().decode("utf-8"))
                    with open(out, "w", encoding="utf-8", newline="\n") as f:
                        f.write(text)
    missing = [n for n in TAKE.values() if not os.path.exists(os.path.join(dest, n))]
    print("Fantasy3: %d WAVs written, %d of %d in place%s" % (done, len(TAKE) - len(missing), len(TAKE),
                                                              (", missing " + ", ".join(missing)) if missing else ""))
    return 1 if missing else 0


if __name__ == "__main__":
    sys.exit(main())
