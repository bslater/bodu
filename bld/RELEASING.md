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

  A single package still breaks rank out of band by setting
  `BoduPackageVersionOverride` to a literal version; that is reported in the
  build log and, on a manifest package, will fail the stream check until the
  tier and the override agree.
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
  version a no-op.
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

Tag runs ignore the input: a tag releases the whole manifest at the tagged
version, which is what keeps the lock-step set coherent.

## Dry run

Actions → **Release** → *Run workflow* with `publish` unchecked. The run
real-signs and packs all packable projects, stages the manifest set, and
uploads two artifacts without pushing anything:

- `nuget-packages` — everything that packed (debugging aid).
- `nuget-packages-publish` — exactly what a tag would publish. Inspect this.

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
   `1.1.0`).
3. Tag `v<new-version>` and push. Existing packages re-publish at the new
   lock-step version; the new wave publishes for the first time.
4. After publish, bump `BoduPackageValidationBaseline` to the new version.

## Out-of-band single-package fix

Set `<BoduPackageVersionOverride>` in the affected csproj (reported in build
output so the divergence is auditable), release, then remove the override
when the next lock-step version catches up. Such a package may pin its own
`PackageValidationBaselineVersion` until then.
