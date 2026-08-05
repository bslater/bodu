"""Extract the RFC 5545 section 3.8.5.3 worked recurrence examples into a machine-readable corpus.

The RFC states expected occurrences as prose (month names, day ranges, comma lists, and a
"(YYYY 9:00 AM EDT)" year marker). This script transcribes that notation into concrete dates and
self-checks the transcription: every rule bounded by COUNT=n must expand to exactly n dates.
"""
import re
import json
import pathlib
import datetime

MONTHS = {m: i + 1 for i, m in enumerate(
    ["January", "February", "March", "April", "May", "June", "July",
     "August", "September", "October", "November", "December"])}
MONTH_RE = re.compile(r'\b(' + '|'.join(MONTHS) + r')\b')


def plain_text(html_path):
    import html as htmlmod
    src = pathlib.Path(html_path).read_text(encoding="utf-8", errors="replace")
    s = re.sub(r'(?is)<(script|style)\b.*?</\1>', ' ', src)
    s = re.sub(r'(?s)<[^>]+>', '', s)
    return htmlmod.unescape(s).replace(' ', ' ')


def example_lines(text):
    start = text.find("3.8.5.3.  Recurrence Rule", 100000)
    sec = text[start:]
    sec = sec[:sec.find("3.8.6.")]
    keep = [ln for ln in sec.split("\n")
            if not re.match(r'^\s*(Desruisseaux\s+Standards Track|RFC 5545\s+iCalendar)', ln)
            and '\f' not in ln]
    body = "\n".join(keep)
    body = body[body.find("Example:  All examples assume"):]
    # Unfold RFC 5545 line folding on property lines.
    out, buf = [], None
    for ln in body.split("\n"):
        # A folded continuation is indented past the property and is not the "==>" expectation marker.
        if (buf is not None and re.match(r'^\s*(RRULE|DTSTART|EXDATE|RDATE)', buf)
                and re.match(r'^\s{8,}\S', ln) and not ln.strip().startswith('==')):
            buf += ln.strip()
            continue
        if buf is not None:
            out.append(buf)
        buf = ln
    if buf is not None:
        out.append(buf)
    return out


OCCURRENCE_ONLY = re.compile(
    r'[\s;,\.\d\-]*(?:(?:' + '|'.join(MONTHS) + r')[\s;,\.\d\-]*)*')


def is_expected_continuation(t):
    """A continuation of an expected block, as opposed to the next example's description.

    Descriptions can themselves contain a month name ("...an invalid date (i.e., February 30)..."),
    so the test is structural: once parentheticals are removed, an occurrence line consists only of
    month names, digits and separators.
    """
    if t.startswith("("):
        return True
    if t.endswith(":") or t.startswith("3.8."):
        return False
    return OCCURRENCE_ONLY.fullmatch(re.sub(r'\(.*?\)', '', t)) is not None


def expand(expected):
    """Expand the RFC's prose occurrence notation into (year, month, day) tuples."""
    out, year = [], None
    parts = re.split(r'\((\d{4})[^)]*\)', expected)
    stream = []
    if parts[0].strip():
        stream.append((None, parts[0]))
    for i in range(1, len(parts) - 1, 2):
        stream.append((int(parts[i]), parts[i + 1]))
    for yr, chunk in stream:
        if yr:
            year = yr
        if year is None:
            continue
        for grp in chunk.split(";"):
            grp = grp.strip()
            m = MONTH_RE.search(grp)
            if not m:
                continue
            month = MONTHS[m.group(1)]
            rest = re.sub(r'\(.*?\)', '', grp[m.end():])
            for tok in rest.split(","):
                tok = tok.strip().rstrip('.').strip()
                rng = re.fullmatch(r'(\d{1,2})\s*-\s*(\d{1,2})', tok)
                if rng:
                    out.extend((year, month, d) for d in range(int(rng.group(1)), int(rng.group(2)) + 1))
                elif re.fullmatch(r'\d{1,2}', tok):
                    out.append((year, month, int(tok)))
    return out


def parse(html_path):
    unf = example_lines(plain_text(html_path))
    idx = [i for i, ln in enumerate(unf) if ln.strip().startswith("DTSTART")]
    rows = []
    for n, i in enumerate(idx):
        desc_parts = []
        for j in range(i - 1, max(-1, i - 14), -1):
            t = unf[j].strip()
            if not t:
                if desc_parts:
                    break
                continue
            if t.startswith(("DTSTART", "RRULE", "==>", "(", "or")):
                break
            desc_parts.insert(0, t)
            if t.endswith(":") or len(desc_parts) > 3:
                break
        desc = " ".join(desc_parts).rstrip(":").strip()
        stop = idx[n + 1] if n + 1 < len(idx) else len(unf)
        rrules, exp, inexp = [], [], False
        for ln in unf[i + 1:stop]:
            t = ln.strip()
            if not t:
                continue
            if t.startswith("RRULE"):
                rrules.append(t)
                continue
            if t.startswith("==>"):
                inexp = True
                exp.append(t[3:].strip())
                continue
            if inexp:
                if is_expected_continuation(t):
                    exp.append(t)
                else:
                    inexp = False
        if not (rrules and exp):
            continue
        expected = " ".join(exp)
        flags = []
        if "..." in expected:
            flags.append("elided")
        if any(re.search(r'UNTIL=\d+T\d+Z', r) for r in rrules):
            flags.append("utc-until")
        if any(re.search(r'FREQ=(HOURLY|MINUTELY|SECONDLY)', r) for r in rrules):
            flags.append("sub-daily")
        if any(re.search(r'BY(HOUR|MINUTE|SECOND)=', r) for r in rrules):
            flags.append("time-expansion")
        rows.append({
            "id": len(rows) + 1,
            "description": desc,
            "dtstart": re.search(r':(\d{8}T\d{6})', unf[i].strip()).group(1),
            "rrule": rrules[0],
            "alternateRules": rrules[1:],
            "expectedText": expected,
            "dates": expand(expected),
            "flags": flags,
        })
    return rows


def selfcheck(rows):
    problems = []
    for r in rows:
        if "elided" in r["flags"] or "time-expansion" in r["flags"] or "sub-daily" in r["flags"]:
            continue
        c = re.search(r'COUNT=(\d+)', r["rrule"])
        if c and len(r["dates"]) != int(c.group(1)):
            problems.append((r["id"], r["description"][:46], f'COUNT={c.group(1)} expanded={len(r["dates"])}'))
        for (y, m, d) in r["dates"]:
            try:
                datetime.date(y, m, d)
            except ValueError:
                problems.append((r["id"], r["description"][:46], f"impossible date {y}-{m:02d}-{d:02d}"))
    return problems


if __name__ == "__main__":
    import sys
    rows = parse(sys.argv[1])
    problems = selfcheck(rows)
    print(f"examples: {len(rows)}   expanded dates: {sum(len(r['dates']) for r in rows)}")
    print(f"self-check problems: {len(problems)}")
    for p in problems:
        print("   ", p)
    pathlib.Path("rfc-corpus.json").write_text(json.dumps(rows, indent=1))
