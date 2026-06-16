# Strong-name signing (Path A)

Bodu assemblies ship **unsigned by default**. The infrastructure to strong-name them is staged and
inert; enabling it is a two-value change once a key exists. This is *identity* signing only (a stable
`PublicKeyToken`), not publisher trust — for consumer-verifiable publisher identity, add a code-signing
certificate and author-sign the NuGet packages separately.

## What is already wired

- `bld/Signing.props` — `BoduSignAssembly` (off), `SignAssembly`, `AssemblyOriginatorKeyFile`
  (→ `bld/Bodu.snk`), `PublicSign` (on for non-CI), and the `BoduPublicKey` hex string (empty).
- `bld/InternalsVisibleTo.targets` — `BoduAppendPublicKeyToInternalsVisibleTo` weaves
  `, PublicKey=$(BoduPublicKey)` into every `InternalsVisibleTo` grant when signing is on, so the bare
  grants in the project files need no per-project edits.
- `Directory.Build.targets` — pins `AssemblyVersion` to `Major.Minor.0.0` so a patch or out-of-band
  release never shifts the strong-name binding identity (`FileVersion` / `InformationalVersion` still
  carry the full patch).

## Enabling

1. **Generate the key pair** (needs `sn` from the .NET Framework SDK, or Mono's `sn` on Linux/macOS):

   ```bash
   sn -k 2048 bld/Bodu.snk            # private + public — keep the private half secret
   sn -p bld/Bodu.snk bld/Bodu.public.snk   # public-only, safe to commit
   sn -tp bld/Bodu.public.snk         # prints the full PublicKey hex (and the short token)
   ```

   Recommended model: commit `bld/Bodu.public.snk`, keep the private `Bodu.snk` in a CI secret, and
   let `PublicSign=true` cover local/dev builds. Real signing happens only in the release pipeline,
   which supplies the private key.

2. **Populate `BoduPublicKey`** in `bld/Signing.props` with the long hex `PublicKey` from `sn -tp`
   (not the 8-byte token).

3. **Turn it on** — repo-wide default in `bld/Signing.props`, or per build:

   ```bash
   dotnet build bodu.slnx -p:BoduSignAssembly=true
   ```

   `SignAssembly` applies to **src and test** projects alike (the test assemblies must be signed with
   the same key to receive the `InternalsVisibleTo` grants), so this is all-or-nothing across the
   solution.

## Notes

- Strong naming is **effectively permanent** for published packages — the key cannot change later
  without breaking every consumer's binding.
- On .NET Core / .NET 5+ the strong-name signature is **not verified at load**; it exists for identity,
  friend-assembly (`InternalsVisibleTo`) support, and consumers who are themselves strong-named.
