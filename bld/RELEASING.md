# Releasing Bodu packages

How a Bodu NuGet release works, end to end. Companion to
[SIGNING.md](SIGNING.md) (strong-name signing details) and the shipping
manifest at [`release-manifest.txt`](release-manifest.txt).

## The model

- **Lock-step versioning, in two streams.** Packages version together at
  `BoduBaseVersion` ([`Versioning.props`](Versioning.props)), so a matched
  version number across `Bodu.*` packages is a coherent set. Since the 1.0.0
  cut there are **two** such streams, split along the API-stability tier each
  package's README declares:

  | Tier | Ships at | How |
  |---|---|---|
  | Stable | `BoduBaseVersion` | the default — no per-project setting |
  | Preview / Experimental | `BoduPreviewVersion` | `<BoduPackageVersionOverride>$(BoduPreviewVersion)</BoduPackageVersionOverride>` in its own csproj |

  The split exists so a 1.0 compatibility promise is made only by packages
  that can keep it: the preview tier stays below 1.0 until its API settles.
  Reference the `$(BoduPreviewVersion)` property rather than pinning a literal,
  so moving the preview stream stays a one-line edit — `check-release-manifest.sh`
  enforces both that and the tier↔stream agreement, because nothing else
  reconciles a package's README tier with the version it actually publishes at.

  A Stable package can also break rank out of band, shipping ahead of
  `BoduBaseVersion` at a literal `BoduPackageVersionOverride`: see
  [Out-of-band single-package release](#out-of-band-single-package-release).
  The stream check accepts that override only while it is ahead of
  `BoduBaseVersion`.
- **The tag is a label, not a version.** Nothing passes a version to
  `dotnet pack` — each package's version comes from the properties above. A run
  tagged `v1.0.0` therefore publishes the Stable tier at 1.0.0 and the preview
  tier at `BoduPreviewVersion`. Name the tag after `BoduBaseVersion`.
- **One tag releases the manifest.** Pushing a `v<version>` tag (e.g.
  `v1.0.0`) runs `.github/workflows/release.yml`: the whole solution is
  packed (real-signed), but **only packages listed in
  `bld/release-manifest.txt` are pushed to nuget.org**. Everything else is
  produced as a build artifact only.
- **Waves extend the manifest.** Releasing the next wave = append its
  package ids to the manifest **with the version they will first ship at**,
  bump `BoduBaseVersion`, tag the new version. Earlier packages re-publish at
  the new coherent version; `--skip-duplicate` makes re-pushing an unchanged
  version a no-op. The run lists every package it skips that way, and stops a
  release that would publish nothing: see
  [Versions already on nuget.org](#versions-already-on-nugetorg).
- **The manifest records first-shipped versions.** Each line is
  `<PackageId> <first-shipped-version>`. That version is *not* what the run
  publishes (`BoduBaseVersion` is) — it is a permanent note of when the
  package first reached nuget.org, written once and never edited. The
  package-validation baseline uses it to decide whether a package has a
  published predecessor to compare against: without it, appending a wave
  while the baseline still points at the previous release asks ApiCompat to
  restore a package that was never published, and the pack fails with
  `NU1102` before comparing any API.

## Preconditions

1. Repository secrets:
   - `BODU_SNK` (**required**) — base64 of the full private strong-name key.
     The workflow verifies it against the committed `bld/Bodu.public.snk`
     and fails fast on any mismatch.
   - There is **no push key**. Publishing uses nuget.org **trusted
     publishing** (OIDC): the job exchanges this run's GitHub OIDC token for a
     short-lived (~1 hour) key via `NuGet/login`. nuget.org issues it only when
     the request's claims match the trusted-publishing policy — this
     repository, `release.yml`, and the `production` environment — which is why
     the workflow declares `id-token: write` and the job sets
     `environment: production`. Change either and the exchange is refused.
     A run that does not opt in to publishing never performs the exchange, so
     dry runs need no credential at all.
2. `bld/release-manifest.txt` lists exactly the packages this release ships,
   each with the version it first shipped at (new entries take the version
   being cut).
3. `BoduBaseVersion` in `bld/Versioning.props` is the version being cut.
4. Every manifest package has a project-root `README.md` (packed as the
   NuGet readme, carrying its API-stability tier) and an icon under
   `bld/icons/<PackageId>.png`.

`bld/check-release-manifest.sh` enforces preconditions 2 and 4 mechanically,
and runs in `build-test.yml` on every pull request — so a malformed manifest
entry, or a shipping package missing its README, tier banner or icon, fails
there rather than in a release run after a tag has been pushed. Run it locally
before cutting a release:

```bash
bash bld/check-release-manifest.sh
```

## Publishing a subset

A manual run takes an optional `packages` input: space- or comma-separated
package ids to publish **instead of** the whole manifest, e.g.
`Bodu.Core Bodu.IO.Biff`. Leave it empty to stage the full manifest.

Every requested id must appear in `release-manifest.txt`; an unknown id (a typo,
or a package that packs but has not been approved to ship) **fails the run**
rather than being skipped. Publishing a different set than the operator asked
for is the failure worth spending a build on, because a nuget.org version cannot
be withdrawn afterwards — only delisted.

An entry may also be a wildcard pattern (`*`, `?`, `[...]`). A pattern is
matched against the ids in `release-manifest.txt` only, so it never selects a
package the manifest does not list: `Bodu.Numerics*` selects `Bodu.Numerics` and
`Bodu.Numerics.Serialization.Json`, and `Bodu.*` selects the whole manifest. A
pattern that matches nothing fails the run, a package selected more than once is
staged once, and the run log lists what each pattern matched. To see what a
pattern selects before publishing, run with `publish` unchecked.

Tag runs ignore the input: a tag releases the whole manifest at the tagged
version, which is what keeps the lock-step set coherent.

## Dry run

Actions → **Release** → *Run workflow* with `publish` unchecked. The run
real-signs and packs all packable projects, stages the manifest set, and
uploads two artifacts without pushing anything:

- `nuget-packages` — everything that packed (debugging aid).
- `nuget-packages-publish` — exactly what a tag would publish. Inspect this.

It also looks up every staged package on nuget.org and reports which versions a
publish would push and which it would skip, with a warning when it would publish
nothing at all (see
[Versions already on nuget.org](#versions-already-on-nugetorg)). Check that
before tagging.

A local (public-signed) dry run of the same pack:

```shell
dotnet pack bodu.slnx -c Release -p:BoduShipping=true -o ./artifacts
```

## Release

```shell
git tag v1.0.0
git push origin v1.0.0
```

The tag run packs, stages the manifest set, and pushes each `.nupkg` (and
its `.snupkg`) to nuget.org.

**The `v` must be lowercase.** `release.yml` triggers on `tags: ['v*']`, and
GitHub Actions tag filters are case-sensitive, so a tag pushed as `V1.0.0`
matches nothing and starts **no run at all** — no failure, no annotation, and a
tag plus a GitHub Release that both look correct. Nothing can report the miss,
because nothing runs. This happened on 1.0.0, and the only symptom was an absent
release run. After pushing a tag, confirm the Release workflow actually started
before assuming the release is under way; if it did not, check the tag's case
first.

### Versions already on nuget.org

nuget.org never accepts a version twice, and the push passes `--skip-duplicate`,
so a staged version nuget.org already holds is skipped rather than failing the
run. That is deliberate: it is how a release leaves a stream it did not bump
unchanged, and how a re-run finishes a partial publish. It would also let a
release whose versions were never bumped skip every package and still report
success, so before requesting a key the run looks up each staged package on
nuget.org, together with the commit the published version was built from:

- **Not on nuget.org:** the push publishes it.
- **On nuget.org from this commit:** an earlier attempt of this run, or an
  earlier run on this commit, already pushed it, and the push skips it.
- **On nuget.org from another commit:** an earlier release. The push skips it,
  and a notice on the run lists it.

If nothing staged is new and nothing was pushed from this commit, the release
would publish nothing: a publish run fails at that point, before a key is
requested, and a dry run warns. Bump `BoduBaseVersion`, `BoduPreviewVersion` or
the package's `BoduPackageVersionOverride` first. A package that cannot be looked
up is reported as well, and stops a publish run only when it leaves the run
unable to confirm that anything is new.

## Post-publish

1. Verify each manifest package is listed on nuget.org at the new version
   and its README, icon, and license render.
2. Smoke-restore from a clean cache:
   `dotnet add package Bodu.Core` in a scratch project.
3. **Set the package-validation baseline**: in `bld/Versioning.props`, set
   `BoduPackageValidationBaseline` to the just-published version (e.g.
   `1.0.0`). From then on every pack of a manifest-listed package runs the
   strict ApiCompat comparison against the published baseline, catching
   accidental breaking changes at pack time. Verify once nuget.org lists the
   packages (the baseline package is restored during validation):

   ```bash
   dotnet pack Bodu.Core/src/Bodu.Core.csproj -c Release
   ```

   No extra properties: signing is on by default (`bld/Signing.props`), so an
   ordinary build already carries the published strong-name identity that
   ApiCompat compares against. That matters because ApiCompat checks identity
   *before* API surface — against an unsigned build it stops at CP0003
   (public key token `null` vs the published token) and never reaches the
   comparison. A build that deliberately turns signing off
   (`-p:BoduSignAssembly=false`) therefore skips the baseline rather than
   failing on identity.

## Documentation for a release

`.github/workflows/docfx-build-publish.yml` publishes the DocFX site to the
`gh-pages` branch, versioned:

| URL | Contents | Written when |
|---|---|---|
| `/` | the latest release | on its `v*` tag |
| `/<series>/` | that release, archived (`1.0`, `1.1`, …) | once, on its tag |
| `/dev/` | the current master build | every merge to master |

A tag run therefore publishes the docs as well as the packages, with no extra
step. Only a plain `vMAJOR.MINOR.PATCH` tag does so — a prerelease is skipped
rather than allowed to replace the root, since it is not the latest release.

Seeding or repairing a slot out of band is a manual run: Actions → *Build and
Publish DocFX Sites* → *Run workflow* from **master**, with `publish_slot` set to
the slot (`1.0`, `dev`). Use it for a series whose tag predates this workflow, or
a root that needs rebuilding before the next tag is cut.

Dispatching the release **tag** is not the shortcut it looks like, and does not
work. `workflow_dispatch` runs the workflow as defined at the ref you dispatch,
so a tag cut before this publishing existed has no such step and the run would
publish nothing; an old tag's docs may also no longer build, since the guard
rails have tightened since. Dispatch from master with `publish_slot` instead.

### If the Pages source ever has to be re-pointed

The site is served from the `gh-pages` branch (Settings → Pages → *Deploy from a
branch*, `/ (root)`). Should that ever need changing, order the steps so no run
goes red and the site never goes dark:

1. Seed or verify the branch content first — flipping to a branch whose root is
   empty serves a 404.
2. Change the workflow **before** the setting, never after. A job that deploys to
   Pages from Actions fails once Pages is branch-sourced, so flipping first turns
   every docs run red until the workflow catches up; changing the workflow first
   merely leaves the site briefly serving its last published build.

That ordering is the lesson from the original cutover, where the reverse was
written down first and would have produced a window of failing runs.

## Next waves

1. Append the wave's package ids to `bld/release-manifest.txt`.
2. Bump `BoduBaseVersion` (e.g. the coordinated Calendar wave is slated
   `1.2.0`; `Bodu.Security.Cryptography` ships `1.1.0` out of band, so the
   lock-step stream has to move past it).
3. Tag `v<new-version>` and push. Existing packages re-publish at the new
   lock-step version; the new wave publishes for the first time.
4. After publish, bump `BoduPackageValidationBaseline` to the new version.

## Out-of-band single-package release

A Stable package can ship ahead of the lock-step version when it has changes
worth releasing on their own, whether a fix or a feature release.
`Bodu.Security.Cryptography` 1.1.0 is the first.

1. In the package's csproj, set `<BoduPackageVersionOverride>` to the literal
   version, above `BoduBaseVersion`. Pin `PackageValidationBaselineVersion`
   to the package's own last release, so the strict ApiCompat comparison runs
   against what its consumers have now rather than the shared baseline.
2. `check-release-manifest.sh` accepts the override while it is ahead of
   `BoduBaseVersion` and prints it as a notice; the build log reports it too.
3. Release the package on its own from master: Actions → **Release** → *Run
   workflow*, with `packages` set to the package id. Run it first with
   `publish` unchecked and inspect `nuget-packages-publish`, then again with
   `publish` checked. **Do not push a `v*` tag**: a tag releases the whole
   manifest at `BoduBaseVersion`.
4. Once nuget.org lists the package, move the pinned
   `PackageValidationBaselineVersion` up to the version just published. Until
   then the strict comparison runs against the release before it, so removing
   an API the out-of-band release added would still pass it.
5. For release notes, tag the released commit with a name that does not start
   with `v` (e.g. `Bodu.Security.Cryptography-1.1.0`), so neither the release
   nor the docs workflow triggers, and attach a GitHub Release to that tag.
6. The next lock-step release must move past the out-of-band version, because
   that version is already on nuget.org for this package. When
   `BoduBaseVersion` reaches it, the manifest check fails until the override
   and the pinned baseline are removed.
