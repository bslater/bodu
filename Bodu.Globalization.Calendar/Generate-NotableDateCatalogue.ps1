<#
.SYNOPSIS
    Generates the notable-date catalogue documentation pages under docs/guides/calendar/catalogue/.

.DESCRIPTION
    Parses every notable-date XML resource (the common catalogues in Bodu.Globalization.Calendar plus the
    three Calendar.Data.* region packs and the europe-common hub), resolves the import graph the same way the
    runtime loader does (NotableDateResourceLoader.ResolveImports / SelectImportedConcepts / ApplyUse), and
    renders a small set of crisp DocFX markdown pages. The pages catalogue WHAT notable dates exist (by theme
    and by region) and HOW regions and territories differ — deliberately without restating the calculation
    recipe logic: dates are summarised with a one-phrase "when" gloss (anchor name + signed offset, or the
    algorithm key verbatim), never the underlying math.

    The script is run locally; its markdown output is committed as documentation source and built by the
    existing docfx workflow. It mirrors the tools/Generate-CrcDocs.ps1 pattern.

.PARAMETER CommonResourcesPath
    Path to the common-catalogue Resources directory.

.PARAMETER AmericasPath
    Path to the Americas data-pack Resources directory.

.PARAMETER AsiaPacificPath
    Path to the Asia-Pacific data-pack Resources directory.

.PARAMETER EuropePath
    Path to the Europe data-pack Resources directory (also hosts europe-common.xml).

.PARAMETER OutputDir
    Directory the generated catalogue markdown pages are written to.

.PARAMETER TocPath
    Path to docs/guides/toc.yml; the script surgically inserts/updates the catalogue group under the calendar node.

.PARAMETER XsdPath
    Path to NotableDates.xsd, used only when -ValidateXsd is supplied.

.PARAMETER ValidateXsd
    When set, validates every resource against the schema and reports violations (off by default).

.PARAMETER AwarenessSampleCap
    Maximum number of sample observances listed per awareness catalogue before collapsing to "+N more".

.EXAMPLE
    pwsh ./Bodu.Globalization.Calendar/Generate-NotableDateCatalogue.ps1

.NOTES
    Requires PowerShell 7+. Output is UTF-8 (no BOM), LF line endings, matching the committed docs.
#>
#Requires -Version 7
[CmdletBinding()]
param(
    [string]$CommonResourcesPath = (Join-Path $PSScriptRoot 'src' 'Globalization.Calendar.Resources'),
    [string]$AmericasPath        = (Join-Path $PSScriptRoot '..' 'Bodu.Globalization.Calendar.Data' 'Bodu.Globalization.Calendar.Americas'    'src' 'Resources'),
    [string]$AsiaPacificPath     = (Join-Path $PSScriptRoot '..' 'Bodu.Globalization.Calendar.Data' 'Bodu.Globalization.Calendar.AsiaPacific' 'src' 'Resources'),
    [string]$EuropePath          = (Join-Path $PSScriptRoot '..' 'Bodu.Globalization.Calendar.Data' 'Bodu.Globalization.Calendar.Europe'      'src' 'Resources'),
    [string]$OutputDir           = (Join-Path $PSScriptRoot '..' 'docs' 'guides' 'calendar' 'catalogue'),
    [string]$TocPath             = (Join-Path $PSScriptRoot '..' 'docs' 'guides' 'toc.yml'),
    [string]$XsdPath             = (Join-Path $PSScriptRoot 'src' 'Globalization.Calendar' 'NotableDates.xsd'),
    [switch]$ValidateXsd,
    [int]$AwarenessSampleCap = 12
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# ---------------------------------------------------------------------------------------------------------------
# Static configuration: namespace, theme grouping, bundle order, vocabulary maps.
# ---------------------------------------------------------------------------------------------------------------

$Ns = 'urn:bodu:globalization:calendar'

# Theme pages: ordered list of { File; Title; Catalogues[] }. Each catalogue is a filename stem.
$ThemePages = @(
    @{ File = 'theme-civil-and-christian.md';     Title = 'Civil and Christian catalogues'
       Catalogues = @('global-core', 'christian-western', 'christian-orthodox', 'default-minimal', 'europe-common') }
    @{ File = 'theme-religious-non-gregorian.md';  Title = 'Non-Gregorian religious catalogues'
       Catalogues = @('global-anchors', 'global-islamic', 'global-islamic-umm-al-qura', 'global-jewish', 'global-hindu', 'global-buddhist', 'global-lunar', 'global-persian') }
    @{ File = 'theme-cultural-and-family.md';      Title = 'Cultural, family, and remembrance catalogues'
       Catalogues = @('global-cultural', 'global-family', 'global-family-social', 'global-remembrance') }
    @{ File = 'theme-awareness.md';                Title = 'Awareness and themed observances'
       Catalogues = @('global-un', 'global-health', 'global-environment', 'global-education', 'global-science', 'global-social', 'global-food', 'global-animals') }
    @{ File = 'theme-aggregates.md';               Title = 'Aggregate and utility catalogues'
       Catalogues = @('global-all', 'global-multiday-normalization') }
)
# Catalogues rendered in collapsed/sample form (recognition-only awareness noise).
$AwarenessCatalogues = [System.Collections.Generic.HashSet[string]]::new([string[]]@(
    'global-un', 'global-health', 'global-environment', 'global-education', 'global-science', 'global-social', 'global-food', 'global-animals'
), [System.StringComparer]::Ordinal)

# Region bundles: ordered { File; Title; Bundle; PathVar }.
$RegionPages = @(
    @{ File = 'region-americas.md';     Title = 'Americas region packs';     Bundle = 'Americas' }
    @{ File = 'region-asia-pacific.md'; Title = 'Asia-Pacific region packs'; Bundle = 'AsiaPacific' }
    @{ File = 'region-europe.md';       Title = 'Europe region packs';       Bundle = 'Europe' }
)

# Category display order / rank for grouping within a region.
$CategoryOrder = @('PublicHoliday', 'BankHoliday', 'Religious', 'Cultural', 'Observance', 'Remembrance', 'Civic', 'Seasonal', 'School', 'Regional', 'Other', 'None')

$MonthAbbrev = @{
    'January' = 'Jan'; 'February' = 'Feb'; 'March' = 'Mar'; 'April' = 'Apr'; 'May' = 'May'; 'June' = 'Jun'
    'July' = 'Jul'; 'August' = 'Aug'; 'September' = 'Sep'; 'October' = 'Oct'; 'November' = 'Nov'; 'December' = 'Dec'
}
$MonthNumber = @{
    'January' = 1; 'February' = 2; 'March' = 3; 'April' = 4; 'May' = 5; 'June' = 6
    'July' = 7; 'August' = 8; 'September' = 9; 'October' = 10; 'November' = 11; 'December' = 12
}
$DayAbbrev = @{ 'Monday' = 'Mon'; 'Tuesday' = 'Tue'; 'Wednesday' = 'Wed'; 'Thursday' = 'Thu'; 'Friday' = 'Fri'; 'Saturday' = 'Sat'; 'Sunday' = 'Sun' }
$OrdinalAbbrev = @{ 'First' = '1st'; 'Second' = '2nd'; 'Third' = '3rd'; 'Fourth' = '4th'; 'Fifth' = '5th'; 'Last' = 'last' }
$ProximityWord = @{ 'Before' = 'before'; 'OnOrBefore' = 'on/before'; 'Nearest' = 'nearest'; 'OnOrAfter' = 'on/after'; 'After' = 'after' }
# Islamic (Hijri / Umm al-Qura) month names for non-Gregorian Fixed glosses.
$HijriMonths = @{ '1' = 'Muharram'; '2' = 'Safar'; '3' = 'Rabi I'; '4' = 'Rabi II'; '5' = 'Jumada I'; '6' = 'Jumada II'
    '7' = 'Rajab'; '8' = 'Shaban'; '9' = 'Ramadan'; '10' = 'Shawwal'; '11' = 'Dhu al-Qadah'; '12' = 'Dhu al-Hijja' }
# Friendly short names for common offset anchors.
$AnchorShort = @{ 'easter-sunday' = 'Easter'; 'orthodox-easter-sunday' = 'Orthodox Easter'; 'lunar-new-year' = 'Lunar New Year'; 'ramadan' = 'Ramadan' }

# ---------------------------------------------------------------------------------------------------------------
# Section A — resource index (filename stem -> full path), across all four resource directories.
# ---------------------------------------------------------------------------------------------------------------

$script:Warnings = [System.Collections.Generic.List[string]]::new()
function Add-Warn([string]$msg) { [void]$script:Warnings.Add($msg); Write-Warning $msg }

$ResourceDirs = [ordered]@{
    Common      = $CommonResourcesPath
    Americas    = $AmericasPath
    AsiaPacific = $AsiaPacificPath
    Europe      = $EuropePath
}
foreach ($kv in $ResourceDirs.GetEnumerator()) {
    if (-not (Test-Path -LiteralPath $kv.Value)) {
        throw "Resource directory '$($kv.Key)' not found at '$($kv.Value)'. Point the script at the Calendar resources."
    }
}

# stem -> [pscustomobject]{ Path; Dir; Bundle }
$script:Index = @{}
$BundleOfDir = @{ Common = $null; Americas = 'Americas'; AsiaPacific = 'AsiaPacific'; Europe = 'Europe' }
foreach ($kv in $ResourceDirs.GetEnumerator()) {
    foreach ($f in (Get-ChildItem -LiteralPath $kv.Value -Filter '*.xml' -File | Sort-Object Name)) {
        $stem = [System.IO.Path]::GetFileNameWithoutExtension($f.Name)
        if ($script:Index.ContainsKey($stem)) {
            throw "Duplicate resource stem '$stem' in '$($f.FullName)' and '$($script:Index[$stem].Path)'."
        }
        $script:Index[$stem] = [pscustomobject]@{ Path = $f.FullName; DirKey = $kv.Key; Bundle = $BundleOfDir[$kv.Key] }
    }
}

# Region stems = "region-*" packs; everything else is a catalogue/hub.
$script:RegionStems = @($script:Index.Keys | Where-Object { $_ -like 'region-*' } | Sort-Object)

# ---------------------------------------------------------------------------------------------------------------
# Section B — memoized parse into a resource model (namespace-aware via XmlNamespaceManager).
# ---------------------------------------------------------------------------------------------------------------

$script:ResourceCache = @{}

function Get-Resource([string]$stem) {
    if ($script:ResourceCache.ContainsKey($stem)) { return $script:ResourceCache[$stem] }
    if (-not $script:Index.ContainsKey($stem)) { throw "Resource '$stem' is not in the index (referenced but not found)." }

    $path = $script:Index[$stem].Path
    $xml = [xml](Get-Content -LiteralPath $path -Raw)   # throws on malformed XML — the well-formedness gate
    $nsmgr = [System.Xml.XmlNamespaceManager]::new($xml.NameTable)
    $nsmgr.AddNamespace('nd', $Ns)
    $root = $xml.DocumentElement

    $imports = [System.Collections.Generic.List[object]]::new()
    foreach ($imp in $root.SelectNodes('nd:Imports/nd:Import', $nsmgr)) {
        $uses = [System.Collections.Generic.List[object]]::new()
        foreach ($u in $imp.SelectNodes('nd:Use', $nsmgr)) {
            [void]$uses.Add([pscustomobject]@{
                Ref        = $u.GetAttribute('notableDateRef')
                As         = $u.GetAttribute('as')
                Territory  = $u.GetAttribute('territory')
                Category   = $u.GetAttribute('category')
                NonWorking = $u.GetAttribute('nonWorking')
            })
        }
        [void]$imports.Add([pscustomobject]@{ Resource = $imp.GetAttribute('resource'); Uses = $uses.ToArray() })
    }

    $concepts = [System.Collections.Generic.List[object]]::new()
    foreach ($nd in $root.SelectNodes('nd:NotableDates/nd:NotableDate', $nsmgr)) {
        $rules = [System.Collections.Generic.List[object]]::new()
        foreach ($r in $nd.SelectNodes('nd:Rules/nd:Rule', $nsmgr)) {
            $app = $r.SelectSingleNode('nd:Applicability', $nsmgr)
            $territories = @()
            $calendar = ''; $fromYear = ''; $toYear = ''
            if ($app) {
                $calendar = $app.GetAttribute('calendar'); $fromYear = $app.GetAttribute('fromYear'); $toYear = $app.GetAttribute('toYear')
                $territories = @($app.SelectNodes('nd:Territory', $nsmgr) | ForEach-Object { $_.GetAttribute('code') })
            }
            $strat = $r.SelectSingleNode('nd:Strategy', $nsmgr)
            $kind = ''; $attrs = @{}
            if ($strat) {
                $se = @($strat.ChildNodes | Where-Object { $_.NodeType -eq 'Element' }) | Select-Object -First 1
                if ($se) { $kind = $se.LocalName; foreach ($a in $se.Attributes) { $attrs[$a.Name] = $a.Value } }
            }
            [void]$rules.Add([pscustomobject]@{
                Id = $r.GetAttribute('id'); Category = $r.GetAttribute('category'); NonWorking = $r.GetAttribute('nonWorking')
                DurationDays = $r.GetAttribute('durationDays'); Calendar = $calendar; FromYear = $fromYear; ToYear = $toYear
                Territories = $territories; StrategyKind = $kind; StrategyAttrs = $attrs
            })
        }
        [void]$concepts.Add([pscustomobject]@{
            Id = $nd.GetAttribute('id'); DisplayName = $nd.GetAttribute('displayName'); Category = $nd.GetAttribute('category')
            DefaultNonWorking = $nd.GetAttribute('defaultNonWorkingDay'); DefaultDuration = $nd.GetAttribute('defaultDurationDays')
            Rules = $rules.ToArray(); Source = 'inline'
        })
    }

    $model = [pscustomobject]@{
        Stem = $stem; ResourceId = $root.GetAttribute('resourceId'); Bundle = $script:Index[$stem].Bundle
        Imports = $imports.ToArray(); Concepts = $concepts.ToArray()
    }
    $script:ResourceCache[$stem] = $model
    return $model
}

# ---------------------------------------------------------------------------------------------------------------
# Section C — recursive import resolver. Mirrors NotableDateResourceLoader.ResolveImports / SelectImportedConcepts
# / ApplyUse: recurse into each <Import> (all source concepts when bare, else cherry-pick by ref), apply per-Use
# overrides, dedup imported first-source-wins, then local-wins merge. Bare (no-Use) resolution is memoized per stem.
# ---------------------------------------------------------------------------------------------------------------

$script:ResolvedCache = @{}

function Copy-Concept($c, [string]$id, [string]$category, [string]$nonWorking, [string]$territory, [string]$source) {
    # Produces a fresh concept; rules are cloned only when a territory override is applied (territory replaces, not merges).
    $rules = $c.Rules
    if ($territory) {
        $rules = @($c.Rules | ForEach-Object {
            [pscustomobject]@{ Id = $_.Id; Category = $_.Category; NonWorking = $_.NonWorking; DurationDays = $_.DurationDays
                Calendar = $_.Calendar; FromYear = $_.FromYear; ToYear = $_.ToYear; Territories = @($territory)
                StrategyKind = $_.StrategyKind; StrategyAttrs = $_.StrategyAttrs }
        })
    }
    [pscustomobject]@{
        Id = $id; DisplayName = $c.DisplayName; Category = $category; DefaultNonWorking = $nonWorking
        DefaultDuration = $c.DefaultDuration; Rules = $rules; Source = $source
    }
}

function Select-Imported($import, $sourceConcepts) {
    if ($import.Uses.Count -eq 0) {
        return @($sourceConcepts | ForEach-Object { Copy-Concept $_ $_.Id $_.Category $_.DefaultNonWorking '' $import.Resource })
    }
    $out = [System.Collections.Generic.List[object]]::new()
    foreach ($use in $import.Uses) {
        $c = $sourceConcepts | Where-Object { $_.Id -eq $use.Ref } | Select-Object -First 1
        if (-not $c) { Add-Warn "Import '$($import.Resource)' references unknown concept '$($use.Ref)'."; continue }
        $id = if ($use.As) { $use.As } else { $c.Id }
        $cat = if ($use.Category) { $use.Category } else { $c.Category }
        $nw = if ($use.NonWorking) { $use.NonWorking } else { $c.DefaultNonWorking }
        [void]$out.Add((Copy-Concept $c $id $cat $nw $use.Territory $import.Resource))
    }
    return $out.ToArray()
}

function Resolve-Bare([string]$stem, [System.Collections.Generic.HashSet[string]]$visiting) {
    if ($script:ResolvedCache.ContainsKey($stem)) { return $script:ResolvedCache[$stem] }
    if (-not $visiting.Add($stem)) { Add-Warn "Import cycle detected at '$stem'."; return @() }

    $res = Get-Resource $stem
    $imported = [System.Collections.Generic.List[object]]::new()
    $importedIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($imp in $res.Imports) {
        $src = Resolve-Bare $imp.Resource $visiting
        foreach ($c in (Select-Imported $imp $src)) {
            if ($importedIds.Add($c.Id)) { [void]$imported.Add($c) }
        }
    }
    $localIds = [System.Collections.Generic.HashSet[string]]::new([string[]]@($res.Concepts | ForEach-Object { $_.Id }), [System.StringComparer]::Ordinal)
    $result = @($imported | Where-Object { -not $localIds.Contains($_.Id) }) + @($res.Concepts)
    [void]$visiting.Remove($stem)
    $script:ResolvedCache[$stem] = $result
    return $result
}

# ---------------------------------------------------------------------------------------------------------------
# Section "when"-gloss + territory-scope summary helpers (no recipe math).
# ---------------------------------------------------------------------------------------------------------------

function Get-WhenGloss($rule) {
    $a = $rule.StrategyAttrs
    switch ($rule.StrategyKind) {
        'Fixed' {
            $m = [string]$a['month']; $d = [string]$a['day']
            if ($rule.Calendar -and $rule.Calendar -ne 'Gregorian') {
                $mn = if (($rule.Calendar -in 'Hijri', 'UmmAlQura') -and $HijriMonths.ContainsKey($m)) { $HijriMonths[$m] } else { "month $m" }
                return "$d $mn ($($rule.Calendar))"
            }
            $ma = if ($MonthAbbrev.ContainsKey($m)) { $MonthAbbrev[$m] } else { $m }
            return "Fixed $d $ma"
        }
        'OffsetFromRule' {
            $anchor = [string]$a['notableDateRef']
            $short = if ($AnchorShort.ContainsKey($anchor)) { $AnchorShort[$anchor] } else { $anchor }
            $off = [int]([string]$a['offsetDays'])
            $sign = if ($off -ge 0) { "+$off" } else { "$off" }
            return "$short $sign"
        }
        'DayOfWeekInMonth' {
            $ord = $OrdinalAbbrev[[string]$a['weekOrdinal']]; $dw = $DayAbbrev[[string]$a['dayOfWeek']]; $mo = $MonthAbbrev[[string]$a['month']]
            return "$ord $dw $mo"
        }
        'WeekdayNearDate' {
            $dw = $DayAbbrev[[string]$a['dayOfWeek']]; $dir = $ProximityWord[[string]$a['direction']]
            $mo = $MonthAbbrev[[string]$a['month']]; $d = [string]$a['day']
            return "$dw $dir $d $mo"
        }
        'RelativeWeekdayInMonth' {
            $ord = $OrdinalAbbrev[[string]$a['weekOrdinal']]; $dw = $DayAbbrev[[string]$a['dayOfWeek']]; $mo = $MonthAbbrev[[string]$a['month']]
            return "$ord $dw $mo (relative)"
        }
        'Algorithm' { return "Algorithm: $([string]$a['key'])" }
        default { return '—' }
    }
}

function Get-SortKey($concept) {
    # Representative rule = national-ish (no territory) first, else first rule.
    $rule = ($concept.Rules | Where-Object { $_.Territories.Count -eq 0 } | Select-Object -First 1)
    if (-not $rule) { $rule = $concept.Rules | Select-Object -First 1 }
    if (-not $rule) { return @{ M = 13; D = 32; Rule = $null } }
    $m = 13; $d = 32
    if ($rule.StrategyKind -eq 'Fixed' -and (-not $rule.Calendar -or $rule.Calendar -eq 'Gregorian')) {
        $mn = [string]$rule.StrategyAttrs['month']
        if ($MonthNumber.ContainsKey($mn)) { $m = $MonthNumber[$mn] }
        $d = [int]([string]$rule.StrategyAttrs['day'])
    }
    elseif ($rule.StrategyKind -in 'DayOfWeekInMonth', 'WeekdayNearDate', 'RelativeWeekdayInMonth') {
        $mn = [string]$rule.StrategyAttrs['month']
        if ($MonthNumber.ContainsKey($mn)) { $m = $MonthNumber[$mn]; $d = 15 }
    }
    return @{ M = $m; D = $d; Rule = $rule }
}

function Get-TerritoryScope($concept, [string]$country) {
    $subs = [System.Collections.Generic.List[string]]::new()
    $hasNational = $false
    foreach ($r in $concept.Rules) {
        if ($r.Territories.Count -eq 0) { $hasNational = $true; continue }
        foreach ($t in $r.Territories) {
            if ($t -eq $country) { $hasNational = $true }
            elseif ($t -like "$country-*") { [void]$subs.Add($t.Substring($country.Length + 1)) }
            else { [void]$subs.Add($t) }   # foreign/other code (rare)
        }
    }
    $subList = ($subs | Select-Object -Unique) -join ', '
    if ($hasNational -and $subList) { return "National + $subList" }
    if ($subList) { return $subList }
    return 'National'
}

function Get-IsNonWorking($concept) {
    # Effective non-working: any rule's nonWorking override, else the concept default. "true"/"false" strings.
    foreach ($r in $concept.Rules) { if ($r.NonWorking) { if ($r.NonWorking -eq 'true') { return $true } } }
    return ($concept.DefaultNonWorking -eq 'true')
}

function Get-Calendar($concept) {
    foreach ($r in $concept.Rules) { if ($r.Calendar -and $r.Calendar -ne 'Gregorian') { return $r.Calendar } }
    return 'Gregorian'
}

# ---------------------------------------------------------------------------------------------------------------
# Section D — per-region effective concept model (region inline + imported-with-overrides, local wins).
# ---------------------------------------------------------------------------------------------------------------

function Build-RegionModel([string]$regionStem) {
    $res = Get-Resource $regionStem
    $country = ($regionStem -replace '^region-', '').ToUpperInvariant()
    $imported = [System.Collections.Generic.List[object]]::new()
    $importedIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($imp in $res.Imports) {
        $src = Resolve-Bare $imp.Resource ([System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal))
        foreach ($c in (Select-Imported $imp $src)) {
            if ($importedIds.Add($c.Id)) { [void]$imported.Add($c) }
        }
    }
    $localIds = [System.Collections.Generic.HashSet[string]]::new([string[]]@($res.Concepts | ForEach-Object { $_.Id }), [System.StringComparer]::Ordinal)
    $all = @($imported | Where-Object { -not $localIds.Contains($_.Id) }) + @($res.Concepts)

    # Subdivisions present anywhere in the file (for the call-out).
    $subs = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($c in $all) { foreach ($r in $c.Rules) { foreach ($t in $r.Territories) { if ($t -like "$country-*") { [void]$subs.Add($t) } } } }

    [pscustomobject]@{ Country = $country; Stem = $regionStem; ResourceId = $res.ResourceId; Subdivisions = @($subs); Concepts = $all }
}

# ---------------------------------------------------------------------------------------------------------------
# Section G — rendering helpers.
# ---------------------------------------------------------------------------------------------------------------

$RegeneratedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

function Esc([string]$s) { if ($null -eq $s) { return '' } return ($s -replace '\|', '\|') }
function YesNo([bool]$b) { if ($b) { 'Yes' } else { '—' } }
function Format-Anchor([string]$heading) {
    # markdig heading-slug: lower, spaces->-, drop non-alnum/-.
    $s = $heading.ToLowerInvariant() -replace "[^a-z0-9 \-]", '' -replace '\s+', '-'
    return ($s -replace '-+', '-').Trim('-')
}

function New-Header([string]$title) {
    $l = [System.Collections.Generic.List[string]]::new()
    [void]$l.Add('---'); [void]$l.Add("title: $title"); [void]$l.Add('---'); [void]$l.Add('')
    return , $l   # unary comma prevents PowerShell from unrolling the List to an array
}
function New-Footer {
    $l = [System.Collections.Generic.List[string]]::new()
    [void]$l.Add('---'); [void]$l.Add('')
    [void]$l.Add('*Generated from the notable-date XML resources by `Bodu.Globalization.Calendar/Generate-NotableDateCatalogue.ps1`. ' +
        "Regenerated (UTC): $RegeneratedUtc.* " +
        'For the calculation recipes deliberately omitted here, see [Territories and regional composition](../territories.md), ' +
        '[Working with non-Gregorian calendars](../non-gregorian-calendars.md), and [Holiday patterns](../holiday-patterns.md); ' +
        'for the API, the <xref:Bodu.Globalization.Calendar> namespace.')
    [void]$l.Add('')
    return , $l
}

# Render one catalogue's concept table (theme pages). Returns lines. $observedBy = list of region links.
function Render-CatalogueSection([string]$stem, [string]$themeFile) {
    $l = [System.Collections.Generic.List[string]]::new()
    $res = Get-Resource $stem
    [void]$l.Add("## $stem")
    [void]$l.Add('')

    if ($stem -eq 'global-all') {
        [void]$l.Add("Aggregate catalogue (`$($res.ResourceId)`): imports every other catalogue with no cherry-picks, so each contributes its full concept set. Identifiers shared across sources are de-duplicated first-source-wins. Intended for consumers that want every shared observance at once; territory packs cherry-pick instead.")
        [void]$l.Add('')
        [void]$l.Add('| Imports catalogue |')
        [void]$l.Add('|---|')
        foreach ($imp in $res.Imports) { [void]$l.Add("| ``$($imp.Resource)`` |") }
        [void]$l.Add('')
        return $l
    }
    if ($stem -eq 'europe-common') {
        [void]$l.Add("Pan-European hub (`$($res.ResourceId)`): re-exports the common civil, Christian, family, and cultural concepts from the catalogues below, and defines the two Catholic feasts the catalogues do not carry. The 28 European region packs import their shared observances from here.")
        [void]$l.Add('')
        $reexp = @($res.Imports | ForEach-Object { $_.Resource } | Select-Object -Unique | ForEach-Object { '`' + $_ + '`' })
        $reexpList = $reexp -join ', '
        [void]$l.Add("Re-exports from: $reexpList.")
        [void]$l.Add('')
        [void]$l.Add('Defines inline:')
        [void]$l.Add('')
        [void]$l.Add('| Concept | Category | When |')
        [void]$l.Add('|---|---|---|')
        foreach ($c in $res.Concepts) {
            $sk = Get-SortKey $c
            $when = if ($sk.Rule) { Get-WhenGloss $sk.Rule } else { '—' }
            [void]$l.Add("| $(Esc $c.DisplayName) | $($c.Category) | $when |")
        }
        [void]$l.Add('')
        return $l
    }

    $concepts = @($res.Concepts)
    $anyNonGreg = @($concepts | Where-Object { (Get-Calendar $_) -ne 'Gregorian' }).Count -gt 0

    if ($AwarenessCatalogues.Contains($stem)) {
        $sorted = @($concepts | Sort-Object @{ E = { (Get-SortKey $_).M } }, @{ E = { (Get-SortKey $_).D } }, DisplayName)
        $sample = @($sorted | Select-Object -First $AwarenessSampleCap)
        [void]$l.Add("$($concepts.Count) recognition-only observance(s). Sample (see ``${stem}.xml`` for the full list):")
        [void]$l.Add('')
        [void]$l.Add('| Observance | When |')
        [void]$l.Add('|---|---|')
        foreach ($c in $sample) {
            $sk = Get-SortKey $c; $when = if ($sk.Rule) { Get-WhenGloss $sk.Rule } else { '—' }
            [void]$l.Add("| $(Esc $c.DisplayName) | $when |")
        }
        if ($concepts.Count -gt $sample.Count) { [void]$l.Add("| _+ $($concepts.Count - $sample.Count) more_ | |") }
        [void]$l.Add('')
        return $l
    }

    $sorted = @($concepts | Sort-Object @{ E = { (Get-SortKey $_).M } }, @{ E = { (Get-SortKey $_).D } }, DisplayName)
    if ($anyNonGreg) {
        [void]$l.Add('| Concept | Category | Non-working | Calendar | When |')
        [void]$l.Add('|---|---|---|---|---|')
    } else {
        [void]$l.Add('| Concept | Category | Non-working | When |')
        [void]$l.Add('|---|---|---|---|')
    }
    foreach ($c in $sorted) {
        $sk = Get-SortKey $c; $when = if ($sk.Rule) { Get-WhenGloss $sk.Rule } else { '—' }
        $nw = YesNo (Get-IsNonWorking $c)
        if ($anyNonGreg) { [void]$l.Add("| $(Esc $c.DisplayName) | $($c.Category) | $nw | $(Get-Calendar $c) | $when |") }
        else { [void]$l.Add("| $(Esc $c.DisplayName) | $($c.Category) | $nw | $when |") }
    }
    [void]$l.Add('')
    return $l
}

function Render-ThemePage($page, $observedByMap) {
    $l = New-Header $page.Title
    [void]$l.Add("# $($page.Title)")
    [void]$l.Add('')
    [void]$l.Add('Concepts defined by the shared catalogues in this theme. A region pack imports the concepts it observes and supplies its own territory scope and non-working status — see the [region pages](index.md#by-region). The **When** column is a one-phrase gloss, not the calculation recipe.')
    [void]$l.Add('')
    foreach ($stem in $page.Catalogues) {
        if (-not $script:Index.ContainsKey($stem)) { Add-Warn "Theme '$($page.Title)' lists missing catalogue '$stem'."; continue }
        $l.AddRange([System.Collections.Generic.List[string]](Render-CatalogueSection $stem $page.File))
        if ($observedByMap.ContainsKey($stem) -and $observedByMap[$stem].Count -gt 0) {
            $links = @($observedByMap[$stem] | Sort-Object | ForEach-Object { "[$_]($($RegionFileOf[$_]))" }) -join ', '
            [void]$l.Add("_Observed by:_ $links")
            [void]$l.Add('')
        }
    }
    $l.AddRange((New-Footer))
    return $l
}

function Render-RegionPage($page, $models) {
    $l = New-Header $page.Title
    [void]$l.Add("# $($page.Title)")
    [void]$l.Add('')
    [void]$l.Add("Notable dates observed by each country in the **$($page.Bundle)** data pack, grouped by category. **Territory scope** shows national vs subdivision scoping; **Source** is the direct origin (``inline`` or the imported catalogue/hub). See the [comparison matrix](comparison-matrix.md) for a cross-region overview.")
    [void]$l.Add('')
    foreach ($m in $models) {
        [void]$l.Add("## $($m.Country)")
        [void]$l.Add('')
        if ($m.Subdivisions.Count -gt 0) {
            [void]$l.Add("**Subdivisions:** $($m.Subdivisions -join ', '). A national ``$($m.Country)`` query does not return subdivision-only rules.")
            [void]$l.Add('')
        }
        $anyNonGreg = @($m.Concepts | Where-Object { (Get-Calendar $_) -ne 'Gregorian' }).Count -gt 0
        $byCat = $m.Concepts | Group-Object { if ($_.Category) { $_.Category } else { 'Other' } } | Sort-Object @{ E = { $i = $CategoryOrder.IndexOf($_.Name); if ($i -lt 0) { 99 } else { $i } } }
        foreach ($g in $byCat) {
            [void]$l.Add("### $($g.Name)")
            [void]$l.Add('')
            if ($anyNonGreg) {
                [void]$l.Add('| Concept | Non-working | Territory scope | Calendar | Source | When |')
                [void]$l.Add('|---|---|---|---|---|---|')
            } else {
                [void]$l.Add('| Concept | Non-working | Territory scope | Source | When |')
                [void]$l.Add('|---|---|---|---|---|')
            }
            $rows = @($g.Group | Sort-Object @{ E = { (Get-SortKey $_).M } }, @{ E = { (Get-SortKey $_).D } }, DisplayName)
            foreach ($c in $rows) {
                $sk = Get-SortKey $c; $when = if ($sk.Rule) { Get-WhenGloss $sk.Rule } else { '—' }
                $nw = YesNo (Get-IsNonWorking $c)
                $scope = Get-TerritoryScope $c $m.Country
                $src = if ($c.Source -eq 'inline') { 'inline' } else { "[← $($c.Source)]($(Get-SourceLink $c.Source))" }
                if ($anyNonGreg) { [void]$l.Add("| $(Esc $c.DisplayName) | $nw | $scope | $(Get-Calendar $c) | $src | $when |") }
                else { [void]$l.Add("| $(Esc $c.DisplayName) | $nw | $scope | $src | $when |") }
            }
            [void]$l.Add('')
        }
    }
    $l.AddRange((New-Footer))
    return $l
}

# Map a source catalogue stem to a "themeFile#anchor" link (for region Source column).
$script:ThemeOfCatalogue = @{}
foreach ($tp in $ThemePages) { foreach ($cat in $tp.Catalogues) { $script:ThemeOfCatalogue[$cat] = $tp.File } }
function Get-SourceLink([string]$stem) {
    if ($script:ThemeOfCatalogue.ContainsKey($stem)) { return "$($script:ThemeOfCatalogue[$stem])#$(Format-Anchor $stem)" }
    return "index.md"
}

# ---------------------------------------------------------------------------------------------------------------
# Build region models, then the observed-by map and matrix.
# ---------------------------------------------------------------------------------------------------------------

$RegionFileOf = @{}   # country code -> region page file
foreach ($rp in $RegionPages) {
    foreach ($stem in ($script:RegionStems | Where-Object { $script:Index[$_].Bundle -eq $rp.Bundle })) {
        $RegionFileOf[($stem -replace '^region-', '').ToUpperInvariant()] = $rp.File
    }
}

$AllModels = @{}
foreach ($stem in $script:RegionStems) { $AllModels[$stem] = Build-RegionModel $stem }

# Observed-by: catalogue stem -> set of country codes whose region imports a concept from it (direct hop).
$ObservedBy = @{}
foreach ($stem in $script:RegionStems) {
    $m = $AllModels[$stem]
    $srcset = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($c in $m.Concepts) { if ($c.Source -ne 'inline') { [void]$srcset.Add($c.Source) } }
    foreach ($s in $srcset) {
        if (-not $ObservedBy.ContainsKey($s)) { $ObservedBy[$s] = [System.Collections.Generic.List[string]]::new() }
        [void]$ObservedBy[$s].Add($m.Country)
    }
}

# ---------------------------------------------------------------------------------------------------------------
# Section F — comparison matrix (selective: top-N most-observed concepts, three per-bundle tables).
# ---------------------------------------------------------------------------------------------------------------

function Get-CellState($concept, [string]$country) {
    if (Get-IsNonWorking $concept) { return 'N' }
    $scope = Get-TerritoryScope $concept $country
    if ($scope -ne 'National' -and $scope -notlike 'National*') { return 'S' }
    return 'O'
}

function Build-Matrix {
    # concept id -> @{ Display; Countries = @{ CC = state }; Count }
    $tally = @{}
    foreach ($stem in $script:RegionStems) {
        $m = $AllModels[$stem]
        foreach ($c in $m.Concepts) {
            # pscustomobject (not hashtable) so the 'Count' note-property is not shadowed by Hashtable.Count.
            if (-not $tally.ContainsKey($c.Id)) { $tally[$c.Id] = [pscustomobject]@{ Display = $c.DisplayName; Countries = @{}; Count = 0 } }
            $tally[$c.Id].Countries[$m.Country] = (Get-CellState $c $m.Country)
        }
    }
    foreach ($k in @($tally.Keys)) { $tally[$k].Count = $tally[$k].Countries.Count }
    return $tally
}

function Render-MatrixPage($tally, [int]$topN) {
    $l = New-Header 'Cross-region comparison matrix'
    [void]$l.Add('# Cross-region comparison matrix')
    [void]$l.Add('')
    [void]$l.Add("The most widely-shared notable dates across the region packs, by bundle. Cells: **N** non-working public holiday · **O** observed (working) · **S** subdivision-only · **—** not in the pack. The top $topN concepts by country count are shown; per-country detail is on the [region pages](index.md#by-region), concept definitions on the [theme pages](index.md#by-theme).")
    [void]$l.Add('')
    $top = @($tally.GetEnumerator() | Sort-Object @{ E = { -$_.Value.Count } }, @{ E = { $_.Value.Display } } | Select-Object -First $topN)
    foreach ($rp in $RegionPages) {
        $countries = @($script:RegionStems | Where-Object { $script:Index[$_].Bundle -eq $rp.Bundle } | ForEach-Object { ($_ -replace '^region-', '').ToUpperInvariant() } | Sort-Object)
        [void]$l.Add("## $($rp.Bundle)")
        [void]$l.Add('')
        [void]$l.Add("| Concept | $($countries -join ' | ') |")
        [void]$l.Add("|---|$(($countries | ForEach-Object { ':--:' }) -join '|')|")
        foreach ($e in $top) {
            $cells = @($countries | ForEach-Object { $cc = $_; if ($e.Value.Countries.ContainsKey($cc)) { $e.Value.Countries[$cc] } else { '—' } })
            [void]$l.Add("| $(Esc $e.Value.Display) | $($cells -join ' | ') |")
        }
        [void]$l.Add('')
    }
    $l.AddRange((New-Footer))
    return $l
}

# ---------------------------------------------------------------------------------------------------------------
# Render the overview index page.
# ---------------------------------------------------------------------------------------------------------------

function Render-IndexPage([int]$catCount, [int]$regionCount, [int]$conceptCount, [int]$matrixRows) {
    $l = New-Header 'Notable-date catalogue'
    [void]$l.Add('# Notable-date catalogue')
    [void]$l.Add('')
    [void]$l.Add('What notable dates the calendar data ships, and how regions and territories differ. This catalogue is generated from the `Bodu.Globalization.Calendar` XML resources; it lists the dates and their scope, not the calculation recipes (for those, see the linked guides).')
    [void]$l.Add('')
    [void]$l.Add('Concepts are authored once in a **shared catalogue** and a **region pack** imports the ones it observes, supplying its own territory scope and non-working status. European packs import through the `europe-common` hub, which itself re-exports from the catalogues. The pages below present the same data along two axes.')
    [void]$l.Add('')
    [void]$l.Add('## How to read these pages')
    [void]$l.Add('')
    [void]$l.Add('| Column | Meaning |')
    [void]$l.Add('|---|---|')
    [void]$l.Add('| Category | `PublicHoliday`, `Religious`, `Cultural`, `Observance`, `Remembrance`, … |')
    [void]$l.Add('| Non-working | `Yes` = a non-working public holiday for the scope shown; `—` = a working observance |')
    [void]$l.Add('| Territory scope | `National`, a subdivision list (e.g. `ENG, WLS, NIR`), or `National + …` |')
    [void]$l.Add('| Calendar | shown only when non-Gregorian (`Hijri`, `Hebrew`, `Persian`, `ChineseLunisolar`, …) |')
    [void]$l.Add('| Source | `inline` (defined in the region pack) or `← catalogue` (the direct import) |')
    [void]$l.Add('| When | a one-phrase gloss: `Fixed 25 Dec`, `Easter +1`, `1st Mon May`, `Algorithm: western-easter` — never the recipe |')
    [void]$l.Add('')
    [void]$l.Add('## By theme')
    [void]$l.Add('')
    [void]$l.Add('| Page | Catalogues |')
    [void]$l.Add('|---|---|')
    foreach ($tp in $ThemePages) {
        $cats = @($tp.Catalogues | ForEach-Object { "``$_``" }) -join ', '
        [void]$l.Add("| [$($tp.Title)]($($tp.File)) | $cats |")
    }
    [void]$l.Add('')
    [void]$l.Add('## By region')
    [void]$l.Add('')
    [void]$l.Add('| Page | Countries |')
    [void]$l.Add('|---|---|')
    foreach ($rp in $RegionPages) {
        $ccs = @($script:RegionStems | Where-Object { $script:Index[$_].Bundle -eq $rp.Bundle } | ForEach-Object { ($_ -replace '^region-', '').ToUpperInvariant() } | Sort-Object) -join ', '
        [void]$l.Add("| [$($rp.Title)]($($rp.File)) | $ccs |")
    }
    [void]$l.Add('')
    [void]$l.Add("See also the [cross-region comparison matrix](comparison-matrix.md).")
    [void]$l.Add('')
    [void]$l.Add('## Coverage')
    [void]$l.Add('')
    [void]$l.Add("- **Catalogues:** $catCount")
    [void]$l.Add("- **Region packs:** $regionCount")
    [void]$l.Add("- **Distinct concepts (catalogues):** $conceptCount")
    [void]$l.Add("- **Comparison-matrix rows:** $matrixRows")
    [void]$l.Add("- **This page regenerated (UTC):** $RegeneratedUtc")
    [void]$l.Add('')
    $l.AddRange((New-Footer))
    return $l
}

# ---------------------------------------------------------------------------------------------------------------
# Section I — optional XSD validation.
# ---------------------------------------------------------------------------------------------------------------

if ($ValidateXsd) {
    if (-not (Test-Path -LiteralPath $XsdPath)) { throw "XSD not found at '$XsdPath'." }
    $schemas = [System.Xml.Schema.XmlSchemaSet]::new()
    [void]$schemas.Add($Ns, $XsdPath)
    foreach ($stem in ($script:Index.Keys | Sort-Object)) {
        $doc = [xml](Get-Content -LiteralPath $script:Index[$stem].Path -Raw)
        $doc.Schemas = $schemas
        try { $doc.Validate({ param($s, $e) Add-Warn "XSD ($stem): $($e.Message)" }) }
        catch { Add-Warn "XSD ($stem): $($_.Exception.Message)" }
    }
}

# ---------------------------------------------------------------------------------------------------------------
# Section J — write pages.
# ---------------------------------------------------------------------------------------------------------------

if (-not (Test-Path -LiteralPath $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }

function Write-Page([string]$file, $lines) {
    $path = Join-Path $OutputDir $file
    Set-Content -LiteralPath $path -Value (($lines -join "`n") + "`n") -Encoding utf8 -NoNewline
    Write-Host "  wrote $file"
}

$catCount = @($script:Index.Keys | Where-Object { $_ -notlike 'region-*' }).Count
$conceptCount = (@($script:Index.Keys | Where-Object { $_ -notlike 'region-*' } | ForEach-Object { (Get-Resource $_).Concepts.Count }) | Measure-Object -Sum).Sum
$matrix = Build-Matrix
$matrixRows = [Math]::Min(18, $matrix.Count)

Write-Host "Generating notable-date catalogue ($($script:RegionStems.Count) regions, $catCount catalogues):"
Write-Page 'index.md' (Render-IndexPage $catCount $script:RegionStems.Count $conceptCount $matrixRows)
foreach ($tp in $ThemePages) { Write-Page $tp.File (Render-ThemePage $tp $ObservedBy) }
foreach ($rp in $RegionPages) {
    $models = @($script:RegionStems | Where-Object { $script:Index[$_].Bundle -eq $rp.Bundle } | Sort-Object | ForEach-Object { $AllModels[$_] })
    Write-Page $rp.File (Render-RegionPage $rp $models)
}
Write-Page 'comparison-matrix.md' (Render-MatrixPage $matrix $matrixRows)

# ---------------------------------------------------------------------------------------------------------------
# Section H — surgical, idempotent TOC update. Insert/replace the catalogue group under the calendar node.
# ---------------------------------------------------------------------------------------------------------------

$tocEntries = @(
    [pscustomobject]@{ Name = 'Catalogue overview'; Href = 'index.md' }
    [pscustomobject]@{ Name = 'Civil and Christian catalogues'; Href = 'theme-civil-and-christian.md' }
    [pscustomobject]@{ Name = 'Non-Gregorian religious catalogues'; Href = 'theme-religious-non-gregorian.md' }
    [pscustomobject]@{ Name = 'Cultural, family, and remembrance catalogues'; Href = 'theme-cultural-and-family.md' }
    [pscustomobject]@{ Name = 'Awareness and themed observances'; Href = 'theme-awareness.md' }
    [pscustomobject]@{ Name = 'Aggregate and utility catalogues'; Href = 'theme-aggregates.md' }
    [pscustomobject]@{ Name = 'Americas region packs'; Href = 'region-americas.md' }
    [pscustomobject]@{ Name = 'Asia-Pacific region packs'; Href = 'region-asia-pacific.md' }
    [pscustomobject]@{ Name = 'Europe region packs'; Href = 'region-europe.md' }
    [pscustomobject]@{ Name = 'Cross-region comparison matrix'; Href = 'comparison-matrix.md' }
)
$groupName = 'Bodu.Globalization.Calendar — Notable-date catalogue'
$grp = [System.Collections.Generic.List[string]]::new()
[void]$grp.Add("    - name: $groupName")
[void]$grp.Add('      items:')
foreach ($e in $tocEntries) {
    [void]$grp.Add("        - name: $($e.Name)")
    [void]$grp.Add("          href: calendar/catalogue/$($e.Href)")
}

$tocLines = [System.Collections.Generic.List[string]](Get-Content -LiteralPath $TocPath)
# Remove any prior generated group (sentinel = the group name line through to the next line at <=4-space indent).
$startIdx = -1
for ($i = 0; $i -lt $tocLines.Count; $i++) { if ($tocLines[$i].TrimEnd() -eq "    - name: $groupName") { $startIdx = $i; break } }
if ($startIdx -ge 0) {
    $endIdx = $tocLines.Count
    for ($i = $startIdx + 1; $i -lt $tocLines.Count; $i++) {
        $line = $tocLines[$i]
        if ($line -match '^\S' -or $line -match '^    - name: ' -or $line -match '^- name: ') { $endIdx = $i; break }
    }
    $tocLines.RemoveRange($startIdx, $endIdx - $startIdx)
} else { $startIdx = -1 }

# Insert after the calendar Data.* node: find "- name: Bodu.Globalization.Calendar" top-level node, then the next
# top-level "- name:" sibling, and insert just before it (end of the calendar node's children).
$calNode = -1
for ($i = 0; $i -lt $tocLines.Count; $i++) { if ($tocLines[$i] -eq '- name: Bodu.Globalization.Calendar') { $calNode = $i; break } }
if ($calNode -lt 0) { throw "Could not find the 'Bodu.Globalization.Calendar' node in $TocPath." }
$insertAt = $tocLines.Count
for ($i = $calNode + 1; $i -lt $tocLines.Count; $i++) { if ($tocLines[$i] -match '^- name: ') { $insertAt = $i; break } }
$tocLines.InsertRange($insertAt, $grp)
Set-Content -LiteralPath $TocPath -Value (($tocLines -join "`n") + "`n") -Encoding utf8 -NoNewline
Write-Host "  updated $TocPath"

# ---------------------------------------------------------------------------------------------------------------
# Summary.
# ---------------------------------------------------------------------------------------------------------------

Write-Host ''
Write-Host "Done. Pages: $($ThemePages.Count + $RegionPages.Count + 2). Catalogues: $catCount. Regions: $($script:RegionStems.Count). Catalogue concepts: $conceptCount. Matrix rows: $matrixRows."
if ($script:Warnings.Count -gt 0) { Write-Host "Warnings: $($script:Warnings.Count) (see above)." } else { Write-Host 'Warnings: 0.' }
