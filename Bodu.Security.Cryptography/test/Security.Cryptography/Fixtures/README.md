# Cryptography KAT Fixtures

Externally-acquired **known-answer test (KAT) vector files**, kept in their original upstream
form and organised as:

```
Fixtures/<Cipher>/<Source>/<original-file-name>
```

Each file is wired into the test project as an `<EmbeddedResource>` with an explicit
`LogicalName` (see `Bodu.Security.Cryptography.Test.csproj`). The `LogicalName` is the stable
manifest-resource identifier that readers pass to `GetManifestResourceStream(...)`, so the
on-disk folder layout and file names can change **without touching any test code**.

## Provenance

| Cipher / Algorithm | Source folder | File(s) | Origin |
|---|---|---|---|
| Ascon (AEAD/XOF/CXOF) | `NistLwc` | `LWC_*_KAT_128_*.txt` | NIST SP 800-232 reference (`ascon-c`) |
| SipHash | `Veorq` | `vectors.h` | github.com/veorq/SipHash |
| Snefru | `Merkle` | `testSnefru*.txt`, `correctSnefru*Output.txt` | Merkle Snefru 2.5a reference |
| Tiger | `Nessie` | `test-vectors-nessie-format.dat` | NESSIE Tiger vectors |
| Tiger2 | `Biham` | `biham-tiger2-vectors.txt` | Biham + NESSIE `m:/h:` reference |
| CubeHash | `Sha3Round2` | `ShortMsgKAT_{224,256,384,512}.txt` | SHA-3 Round 2 submission |
| Skein | `Skein1_3` | `skein_golden_kat_short.txt` | Skein 1.3 NIST submission |
| BLAKE2 | `Official` | `blake2-kat.json` | github.com/BLAKE2/BLAKE2 |
| BLAKE3 | `Official` | `test_vectors.json` | github.com/BLAKE3-team/BLAKE3 |
| Whirlpool | `OpenSsl` | `evpmd_whirlpool.txt` | OpenSSL evptests (ISO/IEC 10118-3) |
| Twofish | `AesSubmission` | `ecb_vk.txt`, `ecb_vt.txt`, `ecb_tbl.txt` | Twofish AES submission diskette |
| Rabbit | `Rfc4503` | `rfc4503-appendix-a.txt` | RFC 4503 Appendix A |
| Blowfish | `EricYoung` | `eric-young-vectors.txt` | Eric Young reference vectors |
| HC-128 | `Spec` | `hc128-spec-appendix-a.txt` | Hongjun Wu, "The Stream Cipher HC-128" Appendix A |
| ChaCha20 / Poly1305 | `Rfc8439` | `rfc8439.txt` | RFC 8439 (full text) |
| Salsa20 | `Ecrypt` | `salsa20-full-verified.test-vectors` | ECRYPT Stream Cipher Project verified vectors |
| XSalsa20 | `GoCrypto` | `salsa20_test.go` | golang.org/x/crypto/salsa20 (NaCl vectors) |
| XChaCha20 / XChaCha20-Poly1305 | `Draft` | `draft-arciszewski-xchacha-03.txt` | draft-arciszewski-xchacha-03 |
| X25519 | `Wycheproof` | `x25519_test.txt` | Project Wycheproof |
| Ed25519 | `Wycheproof` | `eddsa_test.txt` | Project Wycheproof |
| ML-KEM | `NistAcvp` | `ML-KEM-*.txt` | NIST ACVP |
| ML-DSA | `NistAcvp` | `ML-DSA-*.txt` | NIST ACVP |
| XTS-AES | `OpenSsl` | `ieee1619-2007-xts.txt` | IEEE Std 1619-2007 vectors, via OpenSSL `evpciph_aes_common.txt` |
| AES-SIV | `Wycheproof` | `aes_siv_cmac_test.txt` | Project Wycheproof (`aes_siv_cmac_test.json`; the valid rows with non-empty associated data) |
| EAX | `Wycheproof` | `aes_eax_test.txt` | Project Wycheproof (`aes_eax_test.json`; the 128-bit-nonce, 128-bit-tag groups) |
| AES-CCM | `Wycheproof` | `aes_ccm_test.txt` | Project Wycheproof (`aes_ccm_test.json`; the 96-bit-nonce, 128-bit-tag groups) |
| AES-GCM-SIV | `Wycheproof` | `aes_gcm_siv_test.txt` | Project Wycheproof (`aes_gcm_siv_test.json`; the 96-bit-nonce, 128-bit-tag groups) |
| scrypt | `OpenSsl` | `scrypt-corpus.txt` | Generated with OpenSSL 3.0.13 `EVP_PBE_scrypt` over an 80-row parameter grid; its header records the grid |
| HPKE | `Rfc9180` | `test-vectors.json` | RFC 9180 |
| Argon2 | `PhcReference` | `test.c`, `argon2{d,i,id}_v16` | github.com/P-H-C/phc-winner-argon2 at `f57e61e` (`src/test.c`, `kats/`); CC0 1.0 or Apache 2.0 |
| Argon2 | `Bodu100` | `argon2-1.0.0-corpus.txt` | Recorded from the published `Bodu.Security.Cryptography` 1.0.0 package - an in-tree regression baseline, not an external source; its header explains the shapes |

## Shared files

A few source documents cover more than one primitive. They live under their primary cipher and
are consumed by several test classes:

- **`ChaCha20/Rfc8439/rfc8439.txt`** - ChaCha20 (`Rfc8439VectorReader`, Appendix A.2), Poly1305
  (Appendix A.3), and the XChaCha20-Poly1305 AEAD framing.
- **`XChaCha20/Draft/draft-arciszewski-xchacha-03.txt`** - XChaCha20 (§2.2.1 HChaCha20 + A.3.2)
  and AEAD_XChaCha20_Poly1305 (A.3.1).
- **`XSalsa20/GoCrypto/salsa20_test.go`** - the Go Salsa20 test source; the XSalsa20 vectors are
  consumed by the XSalsa20 tests (the Salsa20 tests use the ECRYPT file instead).

## Adding a new vector file

1. Drop the original file under `Fixtures/<Cipher>/<Source>/` with its upstream name.
2. Add an `<EmbeddedResource>` entry with an explicit `LogicalName` in the test `.csproj`.
3. Load it in a reader via `Assembly.GetManifestResourceStream("<LogicalName>")`.
