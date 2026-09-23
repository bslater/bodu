# Strong-name signing (Path A)

Bodu assemblies are **strong-named by default**, under public key token `66114d6b6c32ac58`. The
0.5.0 packages shipped under this identity, so it is now fixed: changing or dropping it would break
every consumer's assembly reference. This is *identity* signing only (a stable `PublicKeyToken`), not
publisher trust — for consumer-verifiable publisher identity, add a code-signing certificate and
author-sign the NuGet packages separately.

## Key management

| File | Where it lives | Notes |
|---|---|---|
| `bld/Bodu.public.snk` | **committed** | Public key only. All it takes to public-sign. Produced by `sn -p`. |
| `Bodu.snk` (full key pair) | **never in the repo** | Stored in a secret store / CI secret. `.gitignore` blocks `*.snk` except the public one. Needed only for a real-signing release. |

The default build model is **public signing**: every build stamps the correct public-key identity and
`PublicKeyToken` using the committed public key, with no real RSA signature — which .NET Core / .NET 5+
does not verify at load anyway. A real signature is optional and applied only by a release that opts in
with `-p:BoduRealSign=true` and supplies the private key.

## What is already wired

- `bld/Signing.props` — `BoduSignAssembly` (**on by default**), `SignAssembly`, `BoduStrongNameKeyFile`
  (→ committed `bld/Bodu.public.snk`), `PublicSign` (on unless `BoduRealSign=true`), and the
  `BoduPublicKey` hex string (the public key of `bld/Bodu.public.snk`).
- `bld/InternalsVisibleTo.targets` — `BoduAppendPublicKeyToInternalsVisibleTo` weaves
  `, PublicKey=$(BoduPublicKey)` into every `InternalsVisibleTo` grant when signing is on, so the bare
  grants in the project files need no per-project edits.
- `Directory.Build.targets` — pins `AssemblyVersion` to `Major.Minor.0.0` so a patch or out-of-band
  release never shifts the strong-name binding identity (`FileVersion` / `InformationalVersion` still
  carry the full patch).
- `.gitignore` — ignores `*.snk` except `bld/Bodu.public.snk`, so the private key cannot be committed
  by accident.

## How it is set up (public signing — the normal path)

All three steps below are already done; they are recorded here because the key material is the part
that cannot be reconstructed from the repository.

1. **The public key is committed**, the private key is not:

   ```bash
   git add -f bld/Bodu.public.snk     # -f because *.snk is gitignored
   ```

   The full private `Bodu.snk` lives in a secret store (here, the `BODU_SNK` GitHub Actions secret)
   and never in the working tree.

2. **`BoduPublicKey`** in `bld/Signing.props` holds the long hex `PublicKey` from
   `sn -tp bld/Bodu.public.snk` (the long key, not the 8-byte token). `bld/InternalsVisibleTo.targets`
   appends it to every grant.

3. **`BoduSignAssembly` defaults to `true`**, so an ordinary build already produces the shipped
   identity — which is what lets package validation compare a local pack against the published
   baseline (see [RELEASING.md](RELEASING.md)).

`SignAssembly` applies to **src and test** projects alike: test assemblies must carry the same public
key to receive the `InternalsVisibleTo` grants, so this is all-or-nothing across the solution — and
across `Bodu.CodeStyle`, which resolves the same `Directory.Build.props` / `Directory.Build.targets`
chain. A build can opt out wholesale with `-p:BoduSignAssembly=false`, which also drops the public-key
suffix from the grants so an unsigned build still compiles; such a build skips package validation,
because its assembly identity no longer matches the published baseline.

## Optional: a real, verifiable signature at release

Only needed if you want a full RSA signature (e.g. for .NET Framework consumers with verification on).
The release job materializes the private key from its secret to a path **outside** the working tree and
opts into real signing:

```bash
# e.g. in CI, $BODU_SNK_SECRET holds the base64 of the full Bodu.snk
printf '%s' "$BODU_SNK_SECRET" | base64 -d > "$RUNNER_TEMP/Bodu.snk"
dotnet pack bodu.slnx -c Release \
  -p:BoduSignAssembly=true \
  -p:BoduRealSign=true \
  -p:BoduStrongNameKeyFile="$RUNNER_TEMP/Bodu.snk"
```

## Three artifacts, one key pair

`bld/Bodu.public.snk`, the `BoduPublicKey` hex in `bld/Signing.props`, and the `BODU_SNK` release
secret are three views of the **same** key pair and must never drift apart:

- `BoduPublicKey` is stamped into every `InternalsVisibleTo` grant, so if it disagrees with the key the
  assemblies are actually signed with, the build fails with `CS0281` on every grant.
- The `BODU_SNK` secret is the private half used for real signing; if it is a *different* pair from the
  committed public key, releases ship under an unexpected identity.

The `Release` workflow guards both invariants before it packs: it checks `BoduPublicKey` against
`bld/Bodu.public.snk`, and (after decoding the secret) checks the `BODU_SNK` private key's modulus
against the same file. **When rotating the key, update all three together** — regenerate
`bld/Bodu.public.snk`, refresh `BoduPublicKey` from `sn -tp bld/Bodu.public.snk`, and replace the
`BODU_SNK` secret with the new private key.

## Notes

- Strong naming is **effectively permanent** for published packages — the key cannot change later
  without breaking every consumer's binding.
- The strong name is *identity*, not publisher trust. For consumer-verifiable publisher identity, add a
  code-signing certificate and author-sign the NuGet packages separately.
