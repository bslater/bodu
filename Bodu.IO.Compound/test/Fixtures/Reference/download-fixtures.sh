#!/usr/bin/env bash
# ---------------------------------------------------------------------------------------------------------------
# Refreshes the third-party reference compound-file corpus and regenerates manifest.valid.json.
#
# The checked-in fixtures and manifest make the test suite fully offline; run this only to update the corpus.
# Requires: git, python3 with the `olefile` package (pip install olefile).
# ---------------------------------------------------------------------------------------------------------------
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

git clone --depth 1 https://github.com/waveform-computing/compoundfiles.git "$work/cf"
git clone --depth 1 https://github.com/decalage2/olefile.git "$work/olef"

mkdir -p "$here/valid" "$here/invalid"

# Valid (well-formed or recoverable) fixtures the strict reader accepts.
for f in clean.dat example.dat example2.dat sample1.doc sample1.xls sample2.doc strange_sector_size_v4.dat; do
    cp "$work/cf/tests/$f" "$here/valid/$f"
done
cp "$work/olef/tests/images/test-ole-file.doc" "$here/valid/test-ole-file.doc"

# Malformed / unsupported fixtures (the adversarial corpus).
cp "$work/cf"/tests/invalid_*.dat "$here/invalid/"
cp "$work/cf/tests/strange_sector_size_v3.dat" "$here/invalid/"
cp "$work/cf/tests/strange_mini_sector_size.dat" "$here/invalid/"

# Regenerate the valid-fixture manifest with the independent olefile parser.
python3 - "$here" <<'PY'
import olefile, json, hashlib, glob, os, datetime, sys
root = sys.argv[1]
def gen(p):
    ole = olefile.OleFileIO(p); entries = []
    for parts in ole.listdir(streams=True, storages=True):
        t = ole.get_type(parts); ep = "/".join(parts)
        if t == olefile.STGTY_STREAM:
            d = ole.openstream(parts).read()
            entries.append({"path": ep, "type": "stream", "size": len(d), "sha256": hashlib.sha256(d).hexdigest()})
        elif t == olefile.STGTY_STORAGE:
            entries.append({"path": ep, "type": "storage", "size": 0})
    fx = {"relativePath": "valid/" + os.path.basename(p), "expected": "valid",
          "sectorSize": ole.sector_size, "miniSectorSize": ole.mini_sector_size,
          "miniStreamCutoff": ole.minisectorcutoff, "rootClsid": (ole.root.clsid or "").lower(),
          "entries": sorted(entries, key=lambda e: e["path"])}
    ole.close(); return fx
fixtures = [gen(f) for f in sorted(glob.glob(os.path.join(root, "valid", "*")))]
manifest = {"source": "waveform-computing/compoundfiles (MIT) + decalage2/olefile (BSD)",
            "generatedBy": "olefile " + olefile.__version__,
            "generatedAtUtc": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
            "fixtures": fixtures}
open(os.path.join(root, "manifest.valid.json"), "w").write(json.dumps(manifest, indent=2, ensure_ascii=True))
print("wrote manifest with", len(fixtures), "valid fixtures")
PY
