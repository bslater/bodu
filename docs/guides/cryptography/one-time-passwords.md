---
title: Using HOTP and TOTP
---

# Using HOTP and TOTP

<xref:Bodu.Security.Cryptography.Hotp> and <xref:Bodu.Security.Cryptography.Totp> implement the two one-time-password algorithms used for two-factor authentication: the counter-based **HOTP** of RFC 4226 and the time-based **TOTP** of RFC 6238. Both derive a short decimal code from a shared secret; an authenticator application (Google Authenticator, Authy, 1Password, …) and your server hold the same secret and compute the same code.

> [!NOTE]
> These are message-authentication constructions built on HMAC. The default `OtpHashAlgorithm.Sha1` is mandated by RFC 4226 and is the most widely interoperable choice; SHA-1's collision weaknesses do not apply to its use as an HMAC here. Like the rest of the library, this implementation is not independently audited and offers best-effort, not guaranteed, side-channel resistance.

## The relationship between the two

TOTP is HOTP with a counter derived from the clock. HOTP advances an explicit counter by one on each use; TOTP uses `counter = (now − epoch) / periodSeconds`, so the code changes every step (30 seconds by default). Everything else — the HMAC, the RFC 4226 §5.3 dynamic truncation, and the modulo-`10^digits` reduction — is identical, and <xref:Bodu.Security.Cryptography.Totp> delegates to <xref:Bodu.Security.Cryptography.Hotp> for it.

Both types are static and take the secret as raw bytes:

| Type | Generate | Verify |
|---|---|---|
| `Hotp` | `GenerateCode(secret, counter, digits, algorithm)` | `VerifyCode(secret, code, counter, …)` and a look-ahead resync overload |
| `Totp` | `GenerateCode(secret, timestamp, digits, periodSeconds, algorithm)` | `VerifyCode(secret, code, timestamp, window, …)` |

`digits` is 6–8 (default 6). A code is returned as a **string**, not an integer, because leading zeros are significant — a computed value of 84204 is the six-digit code `"084204"`.

## The secret and provisioning

The generate and verify methods take the secret as raw key bytes, so this type takes no dependency on a particular text encoding. Authenticator applications, however, exchange the secret as a **Base32** string inside an `otpauth://` URI encoded in a QR code. Decode it to bytes with [`Base32`](../text-encoding/index.md) from `Bodu.Text.Encoding` before calling these methods:

```csharp
using Bodu.Security.Cryptography;
using Bodu.Text.Encoding;

// Secret from an otpauth://totp/... ?secret=JBSWY3DPEHPK3PXP URI
byte[] secret = Base32.Decode("JBSWY3DPEHPK3PXP");

string code = Totp.GenerateCode(secret, DateTimeOffset.UtcNow);
```

RFC 4226 recommends a secret of at least 128 bits, and 160 bits (the SHA-1 output length) for full strength. Generate one with `RandomNumberGenerator.GetBytes(20)`.

## TOTP — the common case

Generate the current code, and verify a user-supplied one allowing for a little clock drift:

```csharp
byte[] secret = Base32.Decode(userSecret);

// On the authenticator (or for display):
string code = Totp.GenerateCode(secret, DateTimeOffset.UtcNow);   // 6 digits, 30 s, SHA-1

// On the server, verifying what the user typed:
bool ok = Totp.VerifyCode(secret, userInput, DateTimeOffset.UtcNow);
```

The `window` parameter (default `1`) accepts codes from adjacent time steps, tolerating drift between the client and server clocks. A window of `1` accepts the current step and one on each side — roughly ±30 seconds. Keep it small: each extra step is another code that is valid at the same moment. The overload with an `out int matchedStepOffset` reports which step matched (`0` is on time, negative is a slow client clock, positive is fast), which is useful for detecting persistent drift.

```csharp
if (Totp.VerifyCode(secret, userInput, DateTimeOffset.UtcNow, window: 1, out int offset) && offset != 0)
{
    // Accepted, but the client's clock is drifting — consider prompting the user to re-sync.
}
```

The 8-digit, SHA-256/SHA-512, and non-default-period configurations are supported through the remaining parameters, and an explicit-epoch overload covers the rare RFC 6238 `T0 ≠ Unix epoch` case.

## HOTP — counters and resynchronization

HOTP has no clock; the server stores the next expected counter and advances it on each success. Because a client can generate codes the server never sees (a mis-press), verification supports a bounded **look-ahead**:

```csharp
long stored = LoadCounterForUser();

if (Hotp.VerifyCode(secret, userInput, stored, lookAhead: 3, out long matched))
{
    // Accept, and resynchronize: the next expected counter is one past the one that matched.
    SaveCounterForUser(matched + 1);
}
```

The look-ahead scans `[counter, counter + lookAhead]` and reports the matching counter. Keep the window small and throttle failed attempts — a wide look-ahead admits more candidate codes at once. Per RFC 4226 §7.4, resynchronization should be combined with rate limiting.

## Constant-time verification

`VerifyCode` compares the candidate against the computed code with <xref:System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.ReadOnlySpan{System.Byte},System.ReadOnlySpan{System.Byte})>, so a wrong-but-well-formed code is not distinguishable from a correct one by comparison timing. A candidate whose length differs from `digits` is rejected immediately — the length is not secret. The windowed overloads scan the whole window without short-circuiting, so the loop's duration does not reveal which step or counter matched.

## Where to go next

- [Using HKDF](hkdf.md) — the HMAC-based KDF that backs higher-level key schedules.
- [Bodu.Text.Encoding guides](../text-encoding/index.md) — Base32 and the other alphabets for decoding `otpauth://` secrets.
- [Bodu.Security.Cryptography API reference](xref:Bodu.Security.Cryptography) — full type-by-type docs.
