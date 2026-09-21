# Bodu.Security.Cryptography.Samples.HybridEncryption

RFC 9180 Hybrid Public Key Encryption (HPKE): encrypt to a public key with no prior handshake. Four scenarios —
the single-shot `Seal`/`Open` pair, the four establishment modes and what each one authenticates, the
multi-message sender and receiver contexts, and the suite surface with secret export.

The project is named for what it does rather than for the `Hpke` type, because a namespace ending in `Hpke` would
shadow that class inside the sample's own files.

```bash
dotnet run --project samples/Security.Cryptography/Bodu.Security.Cryptography.Samples.HybridEncryption
```

For NuGet consumers:

```bash
dotnet add package Bodu.Security.Cryptography
```

## A note on determinism

The two long-term X25519 keys are the RFC 7748 §6.1 vectors, imported rather than generated, so both identities are
fixed. What cannot be fixed is the **ephemeral key HPKE generates inside every `Setup` call** — that is the whole
point of the construction, and there is deliberately no API to inject it. Encapsulations and ciphertexts therefore
differ on every run.

So these scenarios print only what does not vary: round-trip results, byte sizes, suite identifiers, and rejection
outcomes. That keeps the sample deterministic (verified identical across three runs) without misrepresenting the
construction — printing a ciphertext here would document output that could never be reproduced. Scenario 1 makes the
variability itself an assertion: it seals the same plaintext twice and confirms both the encapsulation and the
ciphertext differ.

Every scenario opens by printing a **What / Why / Expect** banner — the same three things this README
records per scenario — so a transcript stands on its own and a reader can tell a correct run from a broken
one without opening the source. The `text` blocks below show the value lines only; run the sample to see
the banner above each of them.

## Scenario 1 — SingleShot

**Intent.** Show the base-mode single-shot API and the property that makes HPKE worth having: the sender needs no
key of its own and no prior exchange with the recipient. Then show that every input is authenticated, so a
corruption anywhere fails loudly instead of yielding wrong plaintext.

**What it does.** Exports the recipient's public key, seals a message with `Hpke.Seal`, opens it with `Hpke.Open`,
and prints the size relationship between plaintext and ciphertext. It then attacks the exchange five ways —
tampered ciphertext, tampered associated data, wrong `info`, tampered encapsulation, wrong recipient key — and
finishes by re-sealing the same plaintext to show the output differs but still opens.

**What to expect.** The ciphertext is 16 bytes longer than the plaintext: the AEAD tag. The encapsulation is a
32-byte ephemeral X25519 public key, the only thing the sender must transmit besides the ciphertext. Every attack is
rejected with `CryptographicException` — the tag check fails before any plaintext is returned:

```text
  suite         : KEM=X25519HkdfSha256, KDF=HkdfSha256, AEAD=Aes128Gcm
  recipient pk  : 32 bytes
  encapsulation : 32 bytes (an ephemeral X25519 public key)
  plaintext     : 40 bytes
  ciphertext    : 56 bytes (+16 = the AEAD tag)
  round-trip    : True
  recovered     : "transfer 250.00 AUD to account 0821-4417"
  tampered ciphertext: rejected (CryptographicException)
  tampered AAD       : rejected (CryptographicException)
  wrong info         : rejected (CryptographicException)
  tampered encapsulation: rejected (CryptographicException)
  wrong recipient key: rejected (CryptographicException)
  re-seal differs    : encapsulation True, ciphertext True
  but still opens    : True
```

The `wrong info` rejection is worth noticing: `info` is the application-context string bound into the key schedule,
so two applications sharing a recipient key cannot read each other's messages even with the right private key.

**APIs demonstrated.** `Hpke.Seal`, `Hpke.Open`, `HpkeSuite.X25519_HkdfSha256_Aes128Gcm`, `.Kem` / `.Kdf` / `.Aead`,
`X25519.ImportPrivateKey` / `.ExportPublicKey`.

## Scenario 2 — EstablishmentModes

**Intent.** Show all four modes side by side, because choosing between them is the main design decision an HPKE
caller makes — and the difference is precisely *who the recipient learns the message came from*.

**What it does.** Runs one seal/open round trip per mode: `Base`, `Psk` (a shared symmetric secret mixed into the
key schedule), `Auth` (the sender contributes its own static key, so the KEM derives from two DH operations), and
`AuthPsk` (both). It then supplies a wrong PSK, a wrong PSK identifier, and a wrong expected sender, and finally
tries to open each ciphertext under the *wrong mode*.

**What to expect.** All four round-trip. Every mode-specific input is authenticated, so getting one wrong is a
rejection rather than a garbled result. The last two lines are the load-bearing ones: the mode identifier itself is
bound into the key schedule, so an auth-mode ciphertext cannot be opened as base mode and vice versa — the modes are
not interchangeable:

```text
  suite         : KEM=X25519HkdfSha256, KDF=HkdfSha256, AEAD=ChaCha20Poly1305
  Base     (0x00): round-trip True - no sender authentication
  Psk      (0x01): round-trip True - proves PSK possession
  Auth     (0x02): round-trip True - authenticates the sender's static key
  AuthPsk  (0x03): round-trip True - both
  Each mode's extra input is authenticated:
    psk: wrong key    : rejected (CryptographicException)
    psk: wrong id     : rejected (CryptographicException)
    auth: wrong sender: rejected (CryptographicException)
    auth read as base : rejected (CryptographicException)
    base read as psk  : rejected (CryptographicException)
```

`auth: wrong sender` *is* the authentication property: in auth mode the recipient must name the sender it expects,
and a message from anyone else does not decrypt. Note what PSK mode does and does not give you — it proves the
sender holds the group's pre-shared key, not which member of the group it is.

**APIs demonstrated.** `Hpke.Seal` / `.Open`, `.SealPsk` / `.OpenPsk`, `.SealAuth` / `.OpenAuth`, `.SealAuthPsk` /
`.OpenAuthPsk`, `HpkeMode.Base` / `.Psk` / `.Auth` / `.AuthPsk`,
`HpkeSuite.X25519_HkdfSha256_ChaCha20Poly1305`.

## Scenario 3 — MultiMessageContext

**Intent.** Show the stateful contexts, which are what you want for a stream rather than a single message: one
public-key operation is amortised across every message, and the caller never manages a nonce.

**What it does.** Sets up an `HpkeSender` once (yielding one encapsulation), seals four frames, then sets up an
`HpkeReceiver` from that single encapsulation and opens them in order. It seals identical plaintext twice to show
the sequence number advancing, then uses two fresh receivers to try skipping a frame and replaying a frame. It
finishes by doing the same thing in auth mode to show the context shape is identical.

**What to expect.** One 32-byte encapsulation serves all four messages. Each `Seal` advances an internal sequence
number, from which the nonce is derived — so identical plaintext yields different ciphertext, and the caller cannot
accidentally reuse a nonce. The stream is order-bound in both directions: skipping a frame and replaying one both
fail:

```text
  suite         : KEM=X25519HkdfSha256, KDF=HkdfSha256, AEAD=Aes256Gcm
  encapsulation : 32 bytes, sent once for 4 messages
  sealed        : 4 frames, sizes 39, 40, 40, 39
  all round-trip: True
    [0] "frame 1: session opened"
    [1] "frame 2: 128 rows staged"
    [2] "frame 3: commit accepted"
    [3] "frame 4: session closed"
  same plaintext twice differs: True (the sequence number advanced)
  skipping frame 0: rejected (CryptographicException)
  replaying frame 0: rejected (CryptographicException)
  auth-mode context : True
```

The frame sizes (39, 40, 40, 39) are plaintext length + 16; they differ only because the messages do. Switching a
stream from anonymous to sender-authenticated changes exactly one line — the `Setup` call.

**APIs demonstrated.** `HpkeSender.SetupBase` / `.SetupAuth` / `.Seal` / `.Dispose`, `HpkeReceiver.SetupBase` /
`.SetupAuth` / `.Open` / `.Dispose`, `HpkeSuite.X25519_HkdfSha256_Aes256Gcm`.

## Scenario 4 — SuitesAndExport

**Intent.** Show that a suite is the wire-level identity of the algorithms in use, and that every size a caller
might otherwise hard-code is derived from it. Then show the export interface, which is how HPKE bootstraps keys for
something other than its own AEAD — including the export-only suite that has no AEAD at all.

**What it does.** Prints the parameters of the three preset suites, builds a custom triple (HKDF-SHA512 with
AES-256-GCM) and round-trips a message under it. It then sets up a context pair and exports a 32-byte secret on both
sides, exports under a different label, exports at three lengths, re-exports under the first label, and finally
builds an `ExportOnly` suite to show export working while `Seal` refuses.

**What to expect.** Sizes follow from the AEAD choice (AES-128-GCM takes a 16-byte key, the other two 32), while the
encapsulation and shared-secret sizes follow from the KEM, which is X25519 throughout. Both sides of a context derive
the same exported secret; a different label gives an independent one. The `independent` line is a detail worth
knowing — RFC 9180's labeled expand binds the requested length into the derivation, so a 16-byte export is *not* a
truncation of the 32-byte one:

```text
  Preset suites:
    Aes128Gcm         : key 16B, nonce 12B, tag 16B, encap 32B, shared secret 32B, exportOnly=False
    Aes256Gcm         : key 32B, nonce 12B, tag 16B, encap 32B, shared secret 32B, exportOnly=False
    ChaCha20Poly1305  : key 32B, nonce 12B, tag 16B, encap 32B, shared secret 32B, exportOnly=False
  custom triple : KDF=HkdfSha512 with AEAD=Aes256Gcm -> key 32B
  round-trip    : True
  export agree  : True (both sides derive the same 32 bytes)
  label matters : True (a different label is a different secret)
  lengths       : 16B, 32B and 64B all available under one label
  independent   : True (16B is not a prefix of 32B - the length is bound in)
  repeatable    : True
  ExportOnly    : IsExportOnly=True, key 0B, tag 0B
  export works  : True
  Seal refused  : rejected (NotSupportedException)
```

Unlike `Seal`, `Export` does not advance the sequence number, so it is repeatable within a context. The
`ExportOnly` suite reports a 0-byte key and tag and refuses `Seal` with `NotSupportedException` — it exists for
using HPKE purely as a key-agreement and key-derivation step while the application does its own encryption.

**APIs demonstrated.** `HpkeSuite(HpkeKem, HpkeKdf, HpkeAead)`, the three presets, `.Kem` / `.Kdf` / `.Aead` /
`.AeadKeySizeInBytes` / `.AeadNonceSizeInBytes` / `.AeadTagSizeInBytes` / `.EncapsulationSizeInBytes` /
`.SharedSecretSizeInBytes` / `.IsExportOnly`, `HpkeKem.X25519HkdfSha256`, `HpkeKdf.HkdfSha512`,
`HpkeAead.ExportOnly`, `HpkeSender.Export`, `HpkeReceiver.Export`.

## Layout

```text
Bodu.Security.Cryptography.Samples.HybridEncryption/
  Program.cs                          # runs the scenarios in order
  SampleConsole.cs                    # the What / Why / Expect banner every scenario prints through
  Hex.cs                              # lowercase-hex encode/decode
  Parties.cs                          # the fixed RFC 7748 keys, info, and PSK material
  Scenarios/SingleShot.cs
  Scenarios/EstablishmentModes.cs
  Scenarios/MultiMessageContext.cs
  Scenarios/SuitesAndExport.cs
```

## Related

- `Bodu.Security.Cryptography.Samples.AsymmetricKeys` — the X25519 key agreement HPKE's KEM is built on, plus
  Ed25519, ML-KEM and ML-DSA.
- `Bodu.Security.Cryptography.Samples.SymmetricAndAead` — the AEAD constructions HPKE seals with, driven directly.
- `Bodu.Security.Cryptography.Samples.HashingMacAndKdf` — HKDF, the KDF behind every HPKE key schedule.
