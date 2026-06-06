# Common notable-date catalogues

These XML files are the shared, reusable notable-date catalogues bundled with
`Bodu.Globalization.Calendar`, expressed on the notable-date schema. They define
common observances **once** so territory packs can *inherit* them instead of
redefining each date inline.

All files in this folder are embedded as manifest resources
(`Bodu.Globalization.Calendar.Resources.<name>.xml`) via the `Resources\*.xml`
wildcard in the project file, and are resolved by name through
`CommonNotableDateResources.Resolver`.

## Catalogues

| Catalogue | Contents |
|---|---|
| `global-core` | Civil core: New Year's Day, International Workers' Day, New Year's Eve. |
| `christian-western` | Western (Gregorian) Christian feasts: the Easter Sunday anchor plus Good Friday, Maundy Thursday, Easter Monday, Ascension, Whit Sunday/Monday, Corpus Christi, All Saints', Christmas Eve/Day, Boxing Day. |
| `christian-orthodox` | Orthodox Easter anchor and its derived feasts, plus fixed Orthodox observances. |
| `catholic` | Catholic solemnities, Marian feast days, Easter-derived Catholic observances, and selected patronal/cultural feast days. |
| `christian-protestant` | Protestant-specific observances such as Reformation Day/Sunday, Transfiguration Sunday, All Saints' Sunday, and Aldersgate Day. |
| `christian-anglican` | Anglican apostles, evangelists, and fixed calendar feasts not already carried by `christian-western`. |
| `christian-oriental-orthodox` | Oriental Orthodox Pascha cluster plus common Coptic, Armenian, Ethiopian, and Eritrean fixed-date observances. |
| `global-islamic`, `global-islamic-umm-al-qura` | Hijri and Umm al-Qura festivals (Ramadan, Laylat al-Qadr, Eid al-Fitr, Hajj, Eid al-Adha, …). |
| `global-jewish` | Hebrew-calendar festivals (Passover, Rosh Hashanah, Yom Kippur, Sukkot, Simchat Torah, Hanukkah, …). |
| `global-hindu` | Solar-anchored lunar festivals (Vasant Panchami, Diwali, Holi, Navaratri, …). |
| `global-buddhist` | Vesak, Asalha Puja, Losar, Vassa, East Asian Buddha's Birthday, and related observances. |
| `global-sikh` | Sikh observances (Vaisakhi, Gurpurabs, Bandi Chhor Divas, Hola Mohalla, martyrdom days). |
| `global-jain` | Jain observances (Mahavir Jayanti, Paryushana, Samvatsari, Das Lakshan, Jain Diwali). |
| `global-bahai` | Baha'i holy days representable with the current equinox and offset strategies. |
| `global-lunar` | Chinese lunisolar festivals and solar terms (Lunar New Year, Qingming, Mid-Autumn, …). |
| `global-persian` | Persian-calendar observances (Nowruz, Sizdah Bedar, Yalda). |
| `global-zoroastrian` | Zoroastrian Fasli/Iranian-style observances using Persian-calendar dates. |
| `global-anchors` | Cross-tradition anchors (Lunar New Year, Ramadan start, Orthodox Easter) for offset rules. |
| `global-cultural`, `global-un`, `global-remembrance`, `global-health`, `global-food`, `global-science`, `global-environment`, `global-education`, `global-social`, `global-family`, `global-family-social`, `global-animals`, `global-multiday-normalization` | Gregorian observance families. |
| `global-all` | Aggregate that imports every catalogue above. |
| `default-minimal` | Single-concept bootstrap (New Year's Day). |

## How a territory pack inherits

A territory resource imports the concepts it observes and supplies its own
territory scope, category, non-working flag, and weekend adjustment through the
import `<Use>` directive. The shared concept carries the bare calculation
strategy under the stable rule id `default`; the territory specializes it:

```xml
<Imports>
  <Import resource="global-core">
    <Use notableDateRef="new-years-day" territory="US">
      <Adjustments>
        <Adjustment policyRef="saturday-to-friday" />
        <Adjustment policyRef="sunday-to-monday" />
      </Adjustments>
    </Use>
  </Import>
  <Import resource="christian-western">
    <Use notableDateRef="good-friday" territory="US" category="Religious" nonWorking="false" />
    <Use notableDateRef="christmas-day" territory="US">
      <Adjustments>
        <Adjustment policyRef="saturday-to-friday" />
        <Adjustment policyRef="sunday-to-monday" />
      </Adjustments>
    </Use>
  </Import>
</Imports>
```

This is what lets one shared Christmas be observed differently per territory
(United States Saturday→Friday / Sunday→Monday, United Kingdom next-working-day).
A rule that remains local but offsets from an imported concept references it by
the shared rule id, for example
`<OffsetFromRule notableDateRef="easter-sunday" ruleRef="default" offsetDays="-3" />`.

## Loading

Pass the resolver so imports resolve against these catalogues:

```csharp
NotableDateResource resource = NotableDateResourceLoader.Load(xml, CommonNotableDateResources.Resolver);
```

The data packs (`Bodu.Globalization.Calendar.*`) do this for every region
resource. The `CommonResourcesTests.AllBundledCatalogues_LoadAndValidate` test
loads and validates every catalogue here on each build.
