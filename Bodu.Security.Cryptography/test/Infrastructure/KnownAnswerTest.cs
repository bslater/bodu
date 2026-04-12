using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Infrastructure
{
    public static class KnownAnswerTestExtensions
    {
        public static bool TryGet<T>(this KnownAnswerTest kat, string key, out T? value)
        {
            if (kat.Parameters.TryGetValue(key, out var obj) && obj is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }
    }

    public sealed class KnownAnswerTest
    {
        /// <summary>
        /// Gets or sets the expected output from the algorithm (hash or ciphertext).
        /// </summary>
        public byte[] ExpectedOutput { get; init; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the input message to be hashed or encrypted.
        /// </summary>
        public byte[] Input { get; init; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the name of the test case (e.g., "Empty Input", "ABC", "Block Size Input").
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets optional parameters used for the test case (e.g., Key, Tweak, IV, Flags, Lengths).
        /// </summary>
        public IDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
    }
}