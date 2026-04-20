// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Crc" /> hash algorithm, anchored against the default
    /// <see cref="CrcStandard.CRC32_ISOHDLC" /> variant. Catalogue-wide parity tests live in
    /// <c>CrcTests.Catalog.cs</c> and <c>CrcTests.Reference.cs</c>.
    /// </summary>
    [TestClass]
    public partial class CrcTests
        : HashAlgorithmTests<CrcTests, Crc, SingleTestVariant>
    {
        /// <inheritdoc />
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default,
        };

        /// <inheritdoc />
        protected override Crc CreateAlgorithm() => new Crc();

        /// <inheritdoc />
        protected override Crc CreateAlgorithm(SingleTestVariant variant) => new Crc();

        /// <inheritdoc />
        protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
            new HashAlgorithmSpecification
            {
                HashSize = 32,
                InputBlockSize = 1,
                OutputBlockSize = 1,
                IsStateless = false,
            };

        /// <inheritdoc />
        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) =>
            new Dictionary<string, string>
            {
                // Hex strings are little-endian bytes as emitted by Crc.ComputeHash, not the integer CRC value.
                ["Empty"]            = "00000000",
                ["ABC"]              = "480383A3",
                ["QuickBrownFox"]    = "39A34F41",
                ["Zeros_16"]         = "554BBBEC",
                ["Sequential_0_255"] = "A09B2FD3",
            };

        /// <inheritdoc />
        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "00000000",  // []
            "8DEF02D2",  // [0x00]
            "6922DE36",  // [0x00, 0x01]
            "7F895408",  // [0x00, 0x01, 0x02]
            "1386B98B",  // [0x00, 0x01, 0x02, 0x03]
            "CCD35A51",  // [0x00, 0x01, 0x02, 0x03, 0x04]
            "4ACFEB30",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
            "F90958AD",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
            "9F68AA88",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
            "0243E1BC",  // ...
            "46D76C45",
            "E18E2DAD",
            "65C97092",
            "B846FEE6",
            "C856EF69",
            "5E676CA0",
            "88E2CECE",
        };

        /// <summary>
        /// Yields one test row per supported catalogue CRC standard for use with
        /// <see cref="DynamicDataAttribute" />-backed parameterised tests.
        /// </summary>
        /// <returns>Test data rows carrying a single <see cref="CrcStandard" /> instance each.</returns>
        /// <remarks>
        /// Drawn from <see cref="CrcStandard.All" />, which the generator already filters to exclude widths
        /// that cannot be represented in a 64-bit word (currently only <c>CRC-82/DARC</c>).
        /// </remarks>
        public static IEnumerable<object[]> CatalogStandards() =>
            CrcStandard.All.Select(s => new object[] { s });
    }
}
