#!/usr/bin/env python3
"""Flatten v1 European calendar resources into self-contained v2 cookbook resources.

Each v1 region file cherry-picks holidays from shared source resources (europe-common,
christian-gregorian/orthodox, global-*) via <UseFrom>/<Use> and adds a few national rules.
This script resolves those imports transitively and emits one self-contained v2
<NotableDateResource> per country, mirroring the hand-authored Americas/AsiaPacific packs.
"""
import os
import re
import xml.etree.ElementTree as ET

NS = "urn:bodu:globalization:calendar"


def q(tag):
    return f"{{{NS}}}{tag}"


ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LIB_RES = os.path.join(ROOT, "Bodu.Globalization.Calendar/src/Globalization.Calendar.Resources")
EU_RES = os.path.join(ROOT, "Bodu.Globalization.Calendar.Data.Europe/src/Resources")
OUT_RES = os.path.join(ROOT, "Bodu.Globalization.Calendar2.Data.Europe/src/Resources")

CATEGORY = {
    "Holiday": "PublicHoliday", "Observance": "Observance", "Remembrance": "Remembrance",
    "Cultural": "Cultural", "Religious": "Religious", "Civic": "Civic", "Bank": "BankHoliday",
    "Seasonal": "Seasonal", "School": "School", "Regional": "Regional", "None": "None", "Other": "Other",
}
ALGO_KEY = {
    "easter-sunday": "western-easter",
    "OrthodoxEaster": "orthodox-easter",
    "orthodox-easter-sunday": "orthodox-easter",
}

# Country code -> the orthodox/gregorian Easter anchor source already handled via the named ref.
ALREADY_DONE = {"de", "fr", "gb"}


def slugify(name):
    s = name.lower().replace("'", "").replace("’", "")
    s = re.sub(r"[^a-z0-9]+", "-", s).strip("-")
    # Identifiers must start with a letter; move a leading numeric token (for example a year) to the end.
    m = re.match(r"^(\d+)-(.*)$", s)
    if m:
        s = f"{m.group(2)}-{m.group(1)}"
    return s


def find_source(name):
    for base in (EU_RES, LIB_RES):
        p = os.path.join(base, name)
        if os.path.exists(p):
            return p
    raise FileNotFoundError(name)


def rule_record(notable, rule):
    """Capture a v1 <NotableDate>/<Rule> as a portable record."""
    strat = None
    for child in rule:
        if child.tag in (q("Fixed"), q("DayOfWeekInMonth"), q("WeekdayNearDate"),
                          q("RelativeWeekdayInMonth"), q("OffsetFromAnchor"), q("Algorithm")):
            strat = child
            break
    adjustments = [a for a in rule if a.tag == q("Adjustment")]
    return {
        "name": notable.get("name"),
        "category": rule.get("category", "Holiday"),
        "nonWorking": rule.get("nonWorking"),
        "firstYear": rule.get("firstYear"),
        "lastYear": rule.get("lastYear"),
        "calendarType": rule.get("calendarType"),
        "strategy": strat,
        "adjustments": adjustments,
    }


_CACHE = {}


def build_catalogue(filename):
    """Return {referenced-name: record} for a source resource, resolving its own imports."""
    if filename in _CACHE:
        return _CACHE[filename]
    cat = {}
    tree = ET.parse(find_source(filename))
    root = tree.getroot()
    for nd in root.findall(q("NotableDate")):
        rule = nd.find(q("Rule"))
        if rule is not None:
            cat[nd.get("name")] = rule_record(nd, rule)
    for group in root.findall(q("UseFrom")):
        child = build_catalogue(os.path.basename(group.get("resource")))
        for use in group.findall(q("Use")):
            src = use.get("name")
            if src not in child:
                continue
            rec = dict(child[src])
            apply_use(rec, use)
            cat[use.get("as", src)] = rec
    _CACHE[filename] = cat
    return cat


def apply_use(rec, use):
    if use.get("category"):
        rec["category"] = use.get("category")
    if use.get("nonWorking") is not None:
        rec["nonWorking"] = use.get("nonWorking")
    if use.get("firstYear"):
        rec["firstYear"] = use.get("firstYear")
    if use.get("lastYear"):
        rec["lastYear"] = use.get("lastYear")
    if use.get("clearAdjustments") == "true":
        rec["adjustments"] = []
    nested = use.find(q("Rule"))
    if nested is not None:
        adj = [a for a in nested if a.tag == q("Adjustment")]
        if adj:
            rec["adjustments"] = adj


def adj_policy(adj):
    when, action = adj.get("when"), adj.get("action")
    if when == "IfWeekend" and action == "MoveToNextWeekday":
        return "weekend-roll"
    if when in ("IfNonWorkingDay", "IfWeekend") and action == "MoveToNextWorkingDay":
        return "working-day-substitute"
    return "weekend-roll"


POLICY_XML = {
    "weekend-roll": (
        '    <AdjustmentPolicy id="weekend-roll" priority="100">\n'
        '      <Trigger type="IfWeekend" />\n'
        '      <Action type="MoveToNextWorkingDay" skipWeekends="true" skipNonWorkingDates="false" maxSearchDays="7" />\n'
        '      <Emission mode="ObservedOnly" reason="Substitute public holiday" />\n'
        '    </AdjustmentPolicy>'),
    "working-day-substitute": (
        '    <AdjustmentPolicy id="working-day-substitute" priority="100">\n'
        '      <Trigger type="IfWeekend" />\n'
        '      <Action type="MoveToNextWorkingDay" skipWeekends="true" skipNonWorkingDates="true" maxSearchDays="7" />\n'
        '      <Emission mode="ObservedOnly" reason="Substitute public holiday" />\n'
        '    </AdjustmentPolicy>'),
}

MONTHS = ["January", "February", "March", "April", "May", "June", "July",
          "August", "September", "October", "November", "December"]


def month_token(value):
    if value in MONTHS:
        return value
    try:
        return MONTHS[int(value) - 1]
    except (ValueError, IndexError):
        return value


def strategy_xml(rec, cc, anchor_ids):
    s = rec["strategy"]
    if s is None:
        return '<Fixed month="January" day="1" />'
    tag = s.tag.split("}")[-1]
    if tag == "Fixed":
        cal = rec.get("calendarType")
        extra = ""
        if s.get("skipLeapMonth") == "true":
            extra += ' skipLeapMonth="true"'
        if s.get("sweepCalendarYears") == "true":
            extra += ' sweepCalendarYears="true"'
        m = s.get("month") if cal else month_token(s.get("month"))
        return f'<Fixed month="{m}" day="{s.get("day")}"{extra} />'
    if tag == "DayOfWeekInMonth":
        return (f'<DayOfWeekInMonth month="{month_token(s.get("month"))}" '
                f'dayOfWeek="{s.get("dayOfWeek")}" weekOrdinal="{s.get("weekOrdinal")}" />')
    if tag == "WeekdayNearDate":
        return (f'<WeekdayNearDate month="{month_token(s.get("month"))}" day="{s.get("day")}" '
                f'dayOfWeek="{s.get("dayOfWeek")}" direction="{s.get("direction")}" />')
    if tag == "RelativeWeekdayInMonth":
        direction = s.get("direction", "After")
        return (f'<RelativeWeekdayInMonth month="{month_token(s.get("month"))}" '
                f'dayOfWeek="{s.get("dayOfWeek")}" weekOrdinal="{s.get("weekOrdinal")}" '
                f'relativeDayOfWeek="{s.get("relativeDayOfWeek")}" direction="{direction}" />')
    if tag == "OffsetFromAnchor":
        ref = anchor_ids.get(s.get("name"), slugify(s.get("name")))
        return (f'<OffsetFromRule notableDateRef="{ref}" ruleRef="{cc}" '
                f'offsetDays="{s.get("offset")}" />')
    if tag == "Algorithm":
        return f'<Algorithm key="{ALGO_KEY.get(s.get("key"), s.get("key"))}" />'
    return '<Fixed month="January" day="1" />'


def calendar_system(calendar_type):
    if not calendar_type:
        return None
    if "Hijri" in calendar_type:
        return "Hijri"
    if "UmAlQura" in calendar_type:
        return "UmmAlQura"
    if "Hebrew" in calendar_type:
        return "Hebrew"
    if "Persian" in calendar_type:
        return "Persian"
    if "Chinese" in calendar_type:
        return "ChineseLunisolar"
    return None


def collect(region_file, cc):
    """Return an ordered list of (original_name, record, territory) for a region file."""
    items = []
    tree = ET.parse(os.path.join(EU_RES, region_file))
    root = tree.getroot()
    for node in root:
        if node.tag == q("UseFrom"):
            child = build_catalogue(os.path.basename(node.get("resource")))
            for use in node.findall(q("Use")):
                src = use.get("name")
                if src not in child:
                    print(f"  WARN {cc}: unresolved Use '{src}' from {node.get('resource')}")
                    continue
                rec = dict(child[src])
                apply_use(rec, use)
                items.append((use.get("as", src), rec, use.get("territory", cc.upper())))
        elif node.tag == q("NotableDate"):
            rule = node.find(q("Rule"))
            if rule is not None:
                rec = rule_record(node, rule)
                items.append((node.get("name"), rec, rule.get("territory", cc.upper())))
    return items


def emit(region_file, cc):
    items = collect(region_file, cc)
    # Map each emitted concept's ORIGINAL source name to its id, so offset anchors resolve after renames.
    anchor_ids = {}
    for name, rec, _terr in items:
        anchor_ids[name] = slugify(name)

    policies = []
    for _name, rec, _terr in items:
        for adj in rec["adjustments"]:
            p = adj_policy(adj)
            if p not in policies:
                policies.append(p)

    seen = set()
    blocks = []
    for name, rec, terr in items:
        cid = slugify(name)
        if cid in seen:
            continue
        seen.add(cid)
        cat = CATEGORY.get(rec["category"], "PublicHoliday")
        non_working = "true" if rec.get("nonWorking") == "true" else "false"
        app_attrs = ""
        cal = calendar_system(rec.get("calendarType"))
        if cal:
            app_attrs += f' calendar="{cal}"'
        if rec.get("firstYear"):
            app_attrs += f' fromYear="{rec["firstYear"]}"'
        if rec.get("lastYear"):
            app_attrs += f' toYear="{rec["lastYear"]}"'
        # Easter-derived rules need the 1583 lower bound the v1 anchors carried.
        strat = strategy_xml(rec, cc, anchor_ids)
        if ("western-easter" in strat or "orthodox-easter" in strat or "OffsetFromRule" in strat) and "fromYear" not in app_attrs:
            app_attrs += ' fromYear="1583"'
        terr_xml = "".join(f'<Territory code="{t.strip()}" />' for t in terr.split(","))
        adj_xml = ""
        if rec["adjustments"]:
            refs = "".join(f'<Adjustment policyRef="{adj_policy(a)}" />' for a in rec["adjustments"])
            adj_xml = f"\n        <Adjustments>{refs}</Adjustments>"
        blocks.append(
            f'    <NotableDate id="{cid}" displayName="{xml_escape(name)}" category="{cat}" defaultNonWorkingDay="{non_working}">\n'
            f'      <Rules><Rule id="{cc}"><Applicability{app_attrs}>{terr_xml}</Applicability>\n'
            f'        <Strategy>{strat}</Strategy>{adj_xml}</Rule></Rules>\n'
            f'    </NotableDate>')

    policy_block = ""
    if policies:
        policy_block = "\n  <AdjustmentPolicies>\n" + "\n".join(POLICY_XML[p] for p in policies) + "\n  </AdjustmentPolicies>\n"

    out = (
        '<?xml version="1.0" encoding="utf-8"?>\n'
        f'<!--\n  {cc.upper()} notable-date resource, migrated from the v1 region-{cc}.xml to the v2 cookbook schema by\n'
        '  tools/migrate_europe.py. UseFrom imports (europe-common, Christian, and global observances) are flattened\n'
        '  into self-contained rules.\n-->\n'
        f'<NotableDateResource xmlns="{NS}" schemaVersion="1.0" resourceId="data.{cc}">\n'
        '\n  <ResolutionPolicy duplicatePolicy="Error" sameDayCollisionPolicy="KeepAll" priorityDirection="HigherWins" />\n'
        f'{policy_block}'
        '\n  <NotableDates>\n\n' + "\n\n".join(blocks) + '\n\n  </NotableDates>\n</NotableDateResource>\n')

    with open(os.path.join(OUT_RES, region_file), "w", encoding="utf-8") as f:
        f.write(out)
    return len(blocks)


def xml_escape(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def main():
    os.makedirs(OUT_RES, exist_ok=True)
    region_files = sorted(f for f in os.listdir(EU_RES)
                          if f.startswith("region-") and f.endswith(".xml"))
    total = 0
    for rf in region_files:
        cc = rf[len("region-"):-len(".xml")]
        if cc in ALREADY_DONE:
            continue
        n = emit(rf, cc)
        total += n
        print(f"  {cc}: {n} concepts")
    print(f"Total concepts across {len(region_files) - len(ALREADY_DONE)} countries: {total}")


if __name__ == "__main__":
    main()
