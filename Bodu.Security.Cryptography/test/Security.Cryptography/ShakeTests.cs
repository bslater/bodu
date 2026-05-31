// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShakeTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Shake" /> extendable output function.
/// </summary>
[TestClass]
public partial class ShakeTests
    : HashAlgorithmTests<ShakeTests, Shake, ShakeTests.ShakeVariant>
{
    /// <summary>Identifies the security-level variants of <see cref="Shake" />.</summary>
    public enum ShakeVariant
    {
        /// <summary>SHAKE128 (security level = 128 bits, rate = 168 bytes, 256-bit default output).</summary>
        Shake128,

        /// <summary>SHAKE256 (security level = 256 bits, rate = 136 bytes, 256-bit default output).</summary>
        Shake256,
    }

    /// <inheritdoc />
    protected override ShakeVariant DefaultVariant => ShakeVariant.Shake128;

    /// <inheritdoc />
    public override IEnumerable<ShakeVariant> GetHashAlgorithmVariants() =>
    [
        ShakeVariant.Shake128,
        ShakeVariant.Shake256,
    ];

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(ShakeVariant variant) =>
        variant switch
        {
            // Known-answer test vectors from NIST FIPS 202 (256-bit output, empty-string vectors from
            // the standard; non-empty vectors computed against the NIST reference implementation).
            ShakeVariant.Shake128 => new HashAlgorithmSpecification
            {
                HashSize = 256,
                HashBlockSize = 168,
                LongInputLength = 336,
                BoundaryLengths = [1, 167, 168, 169, 336],
                MinNonZeroBytesForLongInput = 30,
                KnownAnswers = new()
                {
                    Empty = "7F9C2BA4E88F827D616045507605853ED73B8093F6EFBC88EB1A6EACFA66EF26",
                    Abc = "BF4D8D1045ED9C4E79459C167503489EA1CDB92F155849322703126A8794BC1E",
                    Zeros16 = "8F8E4F612E61FFB9D78C3EA707E3776805A4F86E1D7371F4C7FEA77A668C8B84",
                    Sequential0To255 = "9DC7650D682956137D374C9BCD2F121912758F90441E3501DE49818BCEA4DCBB",
                },
            },
            ShakeVariant.Shake256 => new HashAlgorithmSpecification
            {
                HashSize = 256,
                HashBlockSize = 168,
                LongInputLength = 272,
                BoundaryLengths = [1, 135, 136, 137, 272],
                MinNonZeroBytesForLongInput = 30,
                KnownAnswers = new()
                {
                    Empty = "46B9DD2B0BA88D13233B3FEB743EEB243FCD52EA62B81B82B50C27646ED5762F",
                    Abc = "29891B30E23953EBDB326187CDADFDC5549ECF70528712455988D24D8AE66660",
                    Zeros16 = "D570B23C455F4F43C4BF34AA6F2B7628C93DD6178DE7CBD32E81AEB879630326",
                    Sequential0To255 = "D33ED6D180EA0408AEF7D32B530BEA6B5D57B963516A4F601F85E954005A9CE7",
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override Shake CreateAlgorithm(ShakeVariant variant) =>
        variant switch
        {
            ShakeVariant.Shake128 => new Shake(256, 128),
            ShakeVariant.Shake256 => new Shake(256, 256),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(ShakeVariant variant) =>
        Array.Empty<string>();
}
