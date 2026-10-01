# Security policy

## Supported versions

Security fixes are released as new versions of the affected packages, published to nuget.org. Fixes are
not backported to earlier release lines.

| Package tier | Receives security fixes |
|---|---|
| **Stable** (1.0 and later) | The latest release of the current major version |
| **Preview** (below 1.0) | The latest release only |

Each package's `README.md`, shown on its nuget.org page, states which tier it belongs to. Packages that
are built in this repository but not published to nuget.org are not covered.

## Reporting a vulnerability

Please **do not** report a security vulnerability through a public issue, pull request, or discussion.

Report it privately through GitHub's private vulnerability reporting instead:

1. Open the repository's [**Security** tab](https://github.com/bslater/bodu/security).
2. Select **Report a vulnerability**.
3. Fill in the advisory form.

A useful report includes:

- the affected package or packages, and the versions you tested;
- the target framework, operating system, and processor architecture, where they matter (several
  cryptographic primitives select a different SIMD kernel by processor);
- a description of the vulnerability and its impact;
- the smallest input or call sequence that reproduces it; and
- a suggested fix, if you have one.

## What to expect

- The maintainers aim to acknowledge a report within five business days, and to keep you informed while
  it is investigated.
- A confirmed vulnerability is fixed in a new release of each affected package. The fix is published with
  a GitHub security advisory, with a CVE requested where one is warranted.
- You are credited in the advisory unless you ask not to be.

Please give the maintainers a reasonable opportunity to release a fix before disclosing a vulnerability
publicly.

## Scope

Examples of what is in scope:

- **Cryptography** (`Bodu.Security.Cryptography`): output that does not match the specification or its
  published test vectors, a verification that accepts a forged tag, signature, or proof, timing that
  depends on secret data in an operation documented as constant-time, and key material left uncleared
  where the documentation says it is cleared.
- **Parsers and readers of untrusted input** (the compound-file, BIFF, Excel, Outlook and PST readers, the
  text-format readers and serializers, and the calendar-document loaders): a crash or exception other than
  the documented ones, an out-of-bounds read, or unbounded memory or CPU use from a crafted input.
- **Plugin loading** (`Bodu.Globalization.Calendar.Plugins`): loading an assembly that the configured
  trust policy should have rejected.

Examples of what is out of scope:

- Collisions or preimages in the non-cryptographic hashes and check digits of `Bodu.IO.Hashing`, which are
  documented as offering no security against an adversary.
- Known weaknesses of an algorithm that the documentation marks as legacy, broken, or for
  interoperability only, such as Skipjack or Snefru.
- Vulnerabilities in third-party dependencies that do not affect how Bodu uses them; please report those
  to the dependency's maintainers.
