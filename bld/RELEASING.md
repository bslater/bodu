# Releasing Bodu packages

How a Bodu NuGet release works, end to end. Companion to
[SIGNING.md](SIGNING.md) (strong-name signing details) and the shipping
manifest at [`release-manifest.txt`](release-manifest.txt).

## The model

- **Lock-step versioning.** Every library versions together at
  `BoduBaseVersion` ([`Versioning.props`](Versioning.props)), so a matched
  version number across `Bodu.*` packages is a coherent set. A single package
  breaks rank only via `BoduPackageVersionOverride` in its own csproj.
- **One tag releases the manifest.** Pushing a `v<version>` tag (e.g.
  `v1.0.0`) runs `.github/workflows/release.yml`: the whole solution is
  packed (real-signed), but **only packages listed in
  `bld/release-manifest.txt` are pushed to nuget.org**. Everything else is
  produced as a build artifact only.
- **Waves extend the manifest.** Releasing the next wave = append its
  package ids to the manifest, bump `BoduBaseVersion`, tag the new version.
  Earlier packages re-publish at the new coherent version;
  `--skip-duplicate` makes re-pushing an unchanged version a no-op.

## Preconditions

1. Repository secrets:
   - `BODU_SNK` (**required**) — base64 of the full private strong-name key.
     The workflow verifies it against the committed `bld/Bodu.public.snk`
     and fails fast on any mismatch.
   - `NUGET_API_KEY` (**required to publish**) — nuget.org push key. When
     unset the workflow still packs and uploads artifacts, so runs are safe
     dry runs.
2. `bld/release-manifest.txt` lists exactly the packages this release ships.
3. `BoduBaseVersion` in `bld/Versioning.props` is the version being cut.
4. Every manifest package has a project-root `README.md` (packed as the
   NuGet readme, carrying its API-stability tier) and an icon under
   `bld/icons/<PackageId>.png`.

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

## Post-publish

1. Verify each manifest package is listed on nuget.org at the new version
   and its README, icon, and license render.
2. Smoke-restore from a clean cache:
   `dotnet add package Bodu.Core` in a scratch project.
3. **Set the package-validation baseline**: in `bld/Versioning.props`, set
   `BoduPackageValidationBaseline` to the just-published version (e.g.
   `1.0.0`). From then on every pack of a manifest-listed package runs the
   strict ApiCompat comparison against the published baseline, catching
   accidental breaking changes at pack time. Verify with a local
   `dotnet pack` once nuget.org lists the packages (the baseline package is
   restored during validation).

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
