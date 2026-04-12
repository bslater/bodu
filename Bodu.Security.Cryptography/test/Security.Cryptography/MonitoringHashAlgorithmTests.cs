using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public partial class MonitoringHashAlgorithmTests
        : Security.Cryptography.HashAlgorithmTests<MonitoringHashAlgorithmTests, MonitoringHashAlgorithm, SingleTestVariant>
    {
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };

        protected override IEnumerable<string> GetFieldsToExcludeFromDisposeValidation()
        {
            var list = new List<string>(base.GetFieldsToExcludeFromDisposeValidation());
            list.AddRange([
                "<DisposeCallCount>k__BackingField"
            ]);

            return list;
        }

        /// <inheritdoc />
        protected override MonitoringHashAlgorithm CreateAlgorithm() => new MonitoringHashAlgorithm();

        protected override MonitoringHashAlgorithm CreateAlgorithm(SingleTestVariant variant) => new MonitoringHashAlgorithm();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "00000000",
            "00000000",
            "01000000",
            "03000000",
            "06000000",
            "0A000000",
            "0F000000",
            "15000000",
            "1C000000",
            "24000000",
            "2D000000",
            "37000000",
            "42000000",
            "4E000000",
            "5B000000",
            "69000000",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "00000000",
            ["ABC"] = "C6000000",
            ["Zeros_16"] = "00000000",
            ["QuickBrownFox"] = "D90F0000",
        };
    }
}