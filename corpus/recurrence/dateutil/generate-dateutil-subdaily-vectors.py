#!/usr/bin/env python3
"""Generate the sub-daily recurrence comparison vectors from python-dateutil.

Writes dateutil-subdaily-vectors.csv next to this script: seeded, randomly composed RRULEs at the
HOURLY, MINUTELY and SECONDLY frequencies, each with a DTSTART and the first occurrences dateutil
enumerates for it. The table is evidence from one implementation family (dateutil, and rrule.js,
which ports it), not an authority; see corpus/recurrence/README.md.

Usage:
    python3 corpus/recurrence/dateutil/generate-dateutil-subdaily-vectors.py

The output is deterministic for a given seed and dateutil version.
"""

import os
import random
import signal
import sys
from datetime import datetime, timedelta
from itertools import islice

import dateutil
from dateutil.rrule import rrulestr

SEED = 5545
ROWS = 200
RECORDED = 30
TIME_LIMIT_SECONDS = 2
WEEKDAYS = ["MO", "TU", "WE", "TH", "FR", "SA", "SU"]
INTERVALS = {
    "HOURLY": [1, 1, 1, 2, 3, 4, 5, 7, 8, 10, 12, 13, 25, 36, 49],
    "MINUTELY": [1, 1, 1, 2, 5, 7, 10, 15, 20, 25, 45, 90, 97, 360, 1441],
    "SECONDLY": [1, 1, 1, 2, 3, 7, 10, 15, 30, 59, 61, 90, 3600, 86401],
}


class TimedOut(Exception):
    pass


def on_alarm(signum, frame):
    raise TimedOut()


def sample(rng, values, low, high):
    return sorted(rng.sample(values, rng.randint(low, high)))


def compose(rng):
    """Return (dtstart, rule) for one random sub-daily rule."""
    freq = rng.choice(["HOURLY", "MINUTELY", "SECONDLY"])
    parts = [f"FREQ={freq}"]
    interval = rng.choice(INTERVALS[freq])
    if interval != 1:
        parts.append(f"INTERVAL={interval}")

    expands = 1
    if rng.random() < 0.35:
        parts.append("BYHOUR=" + ",".join(map(str, sample(rng, list(range(24)), 1, 5))))
    if rng.random() < 0.35:
        minutes = sample(rng, list(range(60)), 1, 4)
        parts.append("BYMINUTE=" + ",".join(map(str, minutes)))
        if freq == "HOURLY":
            expands *= len(minutes)
    if rng.random() < 0.3:
        seconds = sample(rng, list(range(60)), 1, 3)
        parts.append("BYSECOND=" + ",".join(map(str, seconds)))
        if freq != "SECONDLY":
            expands *= len(seconds)
    if rng.random() < 0.3:
        days = sample(rng, WEEKDAYS, 1, 3)
        if rng.random() < 0.2:
            days[0] = rng.choice(["1", "2", "-1"]) + days[0]
        parts.append("BYDAY=" + ",".join(days))
    if rng.random() < 0.2:
        parts.append("BYMONTH=" + ",".join(map(str, sample(rng, list(range(1, 13)), 1, 3))))
    if rng.random() < 0.15:
        parts.append("BYMONTHDAY=" + ",".join(map(str, sample(rng, list(range(1, 29)) + [-1, -2, -3], 1, 3))))
    if rng.random() < 0.08:
        parts.append("BYYEARDAY=" + ",".join(map(str, sample(rng, list(range(1, 366)) + [-1], 1, 2))))
    if expands > 1 and rng.random() < 0.4:
        parts.append("BYSETPOS=" + ",".join(map(str, sample(rng, [1, 2, -1, -2], 1, 2))))

    start = datetime(
        rng.randint(1990, 2035), rng.randint(1, 12), rng.randint(1, 28),
        rng.randint(0, 23), rng.randint(0, 59), rng.randint(0, 59))

    bound = rng.random()
    if bound < 0.25:
        parts.append(f"COUNT={rng.randint(1, 40)}")
    elif bound < 0.45:
        span = {
            "HOURLY": timedelta(hours=rng.randint(1, 2000)),
            "MINUTELY": timedelta(minutes=rng.randint(1, 20000)),
            "SECONDLY": timedelta(seconds=rng.randint(1, 200000)),
        }[freq]
        parts.append("UNTIL=" + (start + span).strftime("%Y%m%dT%H%M%S"))

    return start, ";".join(parts)


def main():
    rng = random.Random(SEED)
    signal.signal(signal.SIGALRM, on_alarm)
    rows = []
    unfinished = 0
    while len(rows) < ROWS:
        start, rule = compose(rng)
        flags = []
        signal.alarm(TIME_LIMIT_SECONDS)
        try:
            occurrences = list(islice(rrulestr(rule, dtstart=start), RECORDED))
        except ValueError:
            occurrences = []
            flags.append("empty-set")
        except TimedOut:
            unfinished += 1
            continue
        finally:
            signal.alarm(0)

        if len(occurrences) == RECORDED:
            flags.append("truncated")

        rows.append((start, rule, " ".join(flags), " ".join(o.strftime("%Y%m%dT%H%M%S") for o in occurrences)))

    here = os.path.dirname(os.path.abspath(__file__))
    path = os.path.join(here, "dateutil-subdaily-vectors.csv")
    with open(path, "w", encoding="utf-8", newline="\n") as out:
        out.write("# python-dateutil sub-daily recurrence vectors (generated)\n")
        out.write("# source-class: third-party-comparison (an implementation's output, not an authority)\n")
        out.write(f"# source: python-dateutil {dateutil.__version__}, dateutil.rrule.rrulestr, on Python "
                  f"{sys.version.split()[0]}\n")
        out.write(f"# generator: corpus/recurrence/dateutil/generate-dateutil-subdaily-vectors.py, seed {SEED}\n")
        out.write("# licence: the rows are dateutil's output for rules the generator composed; dateutil itself\n")
        out.write("#          (Apache-2.0 / BSD-3-Clause) is not redistributed.\n")
        out.write("# lineage: dateutil and rrule.js are one model family, so agreement is that family's reading,\n")
        out.write("#          not consensus; disagreements are classified, never silently resolved.\n")
        out.write(f"# rows: {ROWS}; each records up to the first {RECORDED} occurrences from DTSTART, wall clock.\n")
        out.write(f"# rules dateutil did not finish within {TIME_LIMIT_SECONDS} seconds, which were not "
                  f"recorded: {unfinished}\n")
        out.write(f"# flags: truncated = the rule continues past the {RECORDED} recorded occurrences\n")
        out.write("#        empty-set = dateutil rejects the rule because its interval never reaches a BY\n")
        out.write("#                    value at its own level; the expected stream is empty\n")
        out.write("id,description,dtstart,rrule,flags,expectedInstants\n")
        for number, (start, rule, flags, expected) in enumerate(rows, 1):
            out.write(f"{number},generated #{number},{start.strftime('%Y%m%dT%H%M%S')},\"{rule}\",{flags},{expected}\n")

    print(f"wrote {len(rows)} rows to {path}; {unfinished} rules did not finish in {TIME_LIMIT_SECONDS} s")


if __name__ == "__main__":
    main()
