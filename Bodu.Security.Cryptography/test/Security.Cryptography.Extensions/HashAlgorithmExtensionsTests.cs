using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography.Extensions
{
    [TestClass]
    public partial class HashAlgorithmExtensionsTests
    {
        private static readonly byte[] SampleData = { 1, 2, 3, 4 };
        private static readonly byte[] SampleHash = BitConverter.GetBytes((uint)(1 + 2 + 3 + 4));
        private static readonly string SampleHex = Convert.ToHexString(SampleHash);
        private static readonly string SampleString = "abcd"; // ASCII 97+98+99+100 = 394
        private static readonly byte[] SampleStringHash = BitConverter.GetBytes((uint)(97 + 98 + 99 + 100));
        private static readonly Encoding SampleEncoding = Encoding.ASCII;

        private static MonitoringHashAlgorithm CreateAlgorithm() => new();
    }
}