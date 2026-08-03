# United Kingdom — GOV.UK bank-holiday feed

`bank-holidays-2019-2028.json` is the official GOV.UK bank-holiday dataset
(<https://www.gov.uk/bank-holidays.json>), archived **verbatim** as delivered on
2026-08-03 (the host is proxy-blocked in the build environment, so the bytes were
fetched user-side and handed back).

- **Source class**: `official-published`
- **SHA-256**: `4fc9d13d6f02cd9805b242d7d34621266de82c058aff0e2ed81facbe65e21107`
- **Licence**: Open Government Licence v3.0 (redistribution with attribution permitted,
  which is why the raw JSON is committed rather than link-and-hash only)
- **Coverage**: 2019–2028 across the three feed divisions `england-and-wales`,
  `scotland`, `northern-ireland` (83/94/103 events respectively), including the
  Scottish World Cup bank holiday added to the feed after November 2025

The regression vector table derived from this file lives with the Europe pack tests at
`Bodu.Globalization.Calendar.Data/Bodu.Globalization.Calendar.Europe/test/Globalization.Calendar/Fixtures/Vectors/GbBankHolidays-2019-2028.csv`;
its provenance header records the division→territory mapping, the generation-time
weekday/substitution re-verification, and the sixteen royal/proclamation one-off events
deliberately excluded from the rule-model sweep.
