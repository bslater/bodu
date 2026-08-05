# Coverage baseline

> **Not yet measured.** This page is a generated artifact and no full-solution
> collection has been published to it yet. Regenerate it with the commands below;
> do not hand-edit.

Once generated, this page carries per-package line and branch coverage for every
packable Bodu package, plus the evidence for the run it was measured from — the
commit, the collecting host's `Avx512F.IsSupported`, and the number of phantom
rows discarded.

```bash
bld/collect-coverage.sh          # every test project in bodu.slnx (long; run it overnight)
bld/merge-coverage.sh            # -> artifacts/coverage/report/
pwsh tools/New-CoverageMatrix.ps1
```

Read the numbers with [Code coverage strategy](code-coverage.md) alongside — in
particular, a package can only be read from the **merged** report. A single test
project's Cobertura file lists every assembly that project touched transitively,
most of them at a near-zero rate that says nothing about the package's own
coverage.

A partial collection is not a baseline: a package whose own test project was not
part of the run reports whatever incidental coverage its dependents produced,
which is why this page is left explicitly unmeasured rather than filled in from a
subset.
