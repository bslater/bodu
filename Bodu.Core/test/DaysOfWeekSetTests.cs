namespace Bodu
{
    [TestClass]
    public partial class DaysOfWeekSetTests
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Provides all bitmask permutations with symbol strings in Monday-first order, using the same bitmask
        /// and binary representation as the original Sunday-first test data.
        /// </summary>
        public static IEnumerable<object[]> GetAllBitmaskPermutationWithMondaySymbolsTestData()
        {
#if NETSTANDARD2_0
            return GetAllBitmaskPermutationTestData()
                .Select(o =>
                {
                    var symbol  = (string)o[1];
                    var rotated = symbol.Substring(1) + symbol[0]; // move Sunday to end
                    return new object[] { o[0], rotated, o[2] };
                });
#else
            return GetAllBitmaskPermutationTestData()
                .Select(o => new object[]
                {
                    o[0],                                     // byte mask (unchanged)
                    ((string)o[1])[1..] + ((string)o[1])[0], // rotated symbol string: Sunday → end
                    o[2]                                      // binary string (unchanged)
                });
#endif
        }

        /// <summary>
        /// Provides invalid format strings that are expected to throw or fail parsing explicitly due to format
        /// specifier errors.
        /// </summary>
        public static IEnumerable<object[]> GetInvalidFormatSpecifierTestData()
        {
            // Bad format specifiers
            yield return new object[] { "SMTWTFS", "X" };
            yield return new object[] { "SMTWTFS", "x" };
            yield return new object[] { "SMTWTFS", "SZ" }; // invalid unselected character
            yield return new object[] { "SMTWTFS", "sz" };
            yield return new object[] { "SMTWTFS", "Invalid" };
            yield return new object[] { "SMTWTFS", "" };
            yield return new object[] { "SMTWTFS", "123" };
            yield return new object[] { "SMTWTFS", "SUU" };

            // Wrong parsing mode
            yield return new object[] { "1010101", "S" }; // binary-looking but forced symbol parsing
            yield return new object[] { "1010101", "s" };
            yield return new object[] { "SMTWTFS", "B" }; // symbol-looking but forced binary parsing
            yield return new object[] { "smtwtfs", "b" };
        }

        /// <summary>
        /// Filters invalid parse test cases to only those where no explicit format is provided.
        /// </summary>
        public static IEnumerable<object[]> GetInvalidParseInputNoFormatTestData() =>
            GetInvalidParseInputTestData()
                .Where(o => o[1] == null)
                .Select(o => new[] { o[0] });

        /// <summary>
        /// Provides a comprehensive set of invalid input cases for <see cref="DaysOfWeekSet" /> parsing,
        /// including malformed strings, wrong formats, and invalid characters.
        /// </summary>
        public static IEnumerable<object[]> GetInvalidParseInputTestData()
        {
            // Wrong length
            yield return new object[] { "", null };
            yield return new object[] { "SMTWTF", null };
            yield return new object[] { "SMTWTFSS", null };

            // Incorrect order
            yield return new object[] { "ssmtwtf", null }; // incorrect Saturday order
            yield return new object[] { "s mtwtf", null }; // incorrect Sunday order
            yield return new object[] { "sm  wtf", null };
            yield return new object[] { "m twtfs", null };
            yield return new object[] { "m    fs", null };
            yield return new object[] { "   t fs", null };

            // Symbol mismatch cases
            yield return new object[] { "S_T-TFS", null }; // '-' unexpected for '_'
            yield return new object[] { "s_t-tfs", null };
            yield return new object[] { "M*TWTFS", null }; // '*' unexpected for '-'
            yield return new object[] { "m*twTfs", null };
            yield return new object[] { "ssmtwtf", null }; // incorrect Saturday order

            // Invalid characters
            yield return new object[] { "SXTWTFS", null }; // 'X' invalid
            yield return new object[] { "mxtwtfs", null }; // lower 'x' invalid
            yield return new object[] { "M1WTFSS", null }; // '1' unexpected
            yield return new object[] { "m1wtfss", null }; // lower '1'

            // Invalid binary input
            yield return new object[] { "1200101", "B" };
            yield return new object[] { "1200101", "b" };
            yield return new object[] { "ABCDEF1", "B" };
            yield return new object[] { "abcdef1", "b" };
            yield return new object[] { "11X1111", "B" };
            yield return new object[] { "11x1111", "b" };

            // Bad format specifiers
            yield return new object[] { "SMTWTFS", "X" };
            yield return new object[] { "SMTWTFS", "x" };
            yield return new object[] { "SMTWTFS", "SZ" }; // invalid unselected character
            yield return new object[] { "SMTWTFS", "sz" };

            // Symbol mismatch cases
            yield return new object[] { "S_T-TFS", "SU" }; // '-' unexpected for '_'
            yield return new object[] { "s_t-tfs", "su" };
            yield return new object[] { "M*TWTFS", "MD" }; // '*' unexpected for '-'
            yield return new object[] { "m*twTfs", "md" };
            yield return new object[] { "ssmtwtf", "s" }; // incorrect Saturday order

            // Wrong parsing mode
            yield return new object[] { "1010101", "S" }; // binary-looking but forced symbol parsing
            yield return new object[] { "1010101", "s" };
            yield return new object[] { "SMTWTFS", "B" }; // symbol-looking but forced binary parsing
            yield return new object[] { "smtwtfs", "b" };

            // Bad format strings
            yield return new object[] { "SMTWTFS", "Invalid" };
            yield return new object[] { "SMTWTFS", "" };
            yield return new object[] { "SMTWTFS", "123" };
            yield return new object[] { "SMTWTFS", "SUU" };
        }

        /// <summary>
        /// Filters invalid parse test cases to only those with an explicit non-null format.
        /// </summary>
        public static IEnumerable<object[]> GetInvalidParseInputWithExplicitFormatTestData() =>
            GetInvalidParseInputTestData()
                .Where(o => o[1] != null);

        public static IEnumerable<object[]> GetTryParseExactTestData() =>
            GetValidParseInputTestData()
                .Where(o => o[1] != null) // remove null format cases
                .Select(o => new object[] { o[0], o[1], o[2], true })
            .Union(
                GetInvalidParseInputTestData()
                    .Select(o => new object[] { o[0], o[1], (byte)0b0000000, false })
            );

        public static IEnumerable<object[]> GetTryParseTestData() =>
            GetValidParseInputTestData()
                .Select(o => new object[] { o[0], o[2], true })
            .Union(
                GetInvalidParseInputTestData()
                    .Where(o => o[1] == null) // only use null format cases
                    .Select(o => new object[] { o[0], (byte)0b0000000, false })
            );

        /// <summary>
        /// Provides a comprehensive set of valid test cases for parsing <see cref="DaysOfWeekSet" />,
        /// including letter-based, binary, and custom formats.
        /// </summary>
        public static IEnumerable<object[]> GetValidParseInputTestData()
        {
            // Default auto-detect cases (null format)
            yield return new object[] { "SMTWTFS", null, (byte)0b1111111 };  // Sunday-first
            yield return new object[] { "smtwtfs", null, (byte)0b1111111 };  // Sunday-first lowercase
            yield return new object[] { "MTWTFSS", null, (byte)0b1111111 };  // Monday-first
            yield return new object[] { "mtwtfss", null, (byte)0b1111111 };  // Monday-first lowercase
            yield return new object[] { "1010101", null, (byte)0b1010101 };  // Binary auto-detect

            // Explicit binary format
            yield return new object[] { "1010101", "B", (byte)0b1010101 };
            yield return new object[] { "1010101", "b", (byte)0b1010101 };   // lower-case 'b'
            yield return new object[] { "1110000", "B", (byte)0b1110000 };
            yield return new object[] { "0001111", "b", (byte)0b0001111 };

            // Sunday-first with explicit unselected characters
            yield return new object[] { "S_TWTFS", "SU", (byte)0b1011111 };
            yield return new object[] { "S_TWTFS", "su", (byte)0b1011111 };
            yield return new object[] { "S-TWTFS", "SD", (byte)0b1011111 };
            yield return new object[] { "S-TWTFS", "sd", (byte)0b1011111 };
            yield return new object[] { "S*TWTFS", "SA", (byte)0b1011111 };
            yield return new object[] { "S*TWTFS", "sa", (byte)0b1011111 };
            yield return new object[] { "S TWTFS", "SE", (byte)0b1011111 };
            yield return new object[] { "S TWTFS", "se", (byte)0b1011111 };

            // Monday-first with explicit unselected characters
            yield return new object[] { "M_WTF_S", "MU", (byte)0b1101110 };
            yield return new object[] { "M_WTF_S", "mu", (byte)0b1101110 };
            yield return new object[] { "M-WTF-S", "MD", (byte)0b1101110 };
            yield return new object[] { "M-WTF-S", "md", (byte)0b1101110 };
            yield return new object[] { "M*WTF*S", "MA", (byte)0b1101110 };
            yield return new object[] { "M*WTF*S", "ma", (byte)0b1101110 };
            yield return new object[] { "M WTF S", "ME", (byte)0b1101110 };
            yield return new object[] { "M WTF S", "me", (byte)0b1101110 };

            // Mixed selection examples
            yield return new object[] { "S_T_T_S", "SU", (byte)0b1010101 };
            yield return new object[] { "s_t_t_s", "su", (byte)0b1010101 };
            yield return new object[] { "M-W-F--", "MD", (byte)0b0101010 };
            yield return new object[] { "m-w-f--", "md", (byte)0b0101010 };
            yield return new object[] { "1010101", "B", (byte)0b1010101 };
            yield return new object[] { "1010101", "b", (byte)0b1010101 };
            yield return new object[] { "1111111", "B", (byte)0b1111111 };
            yield return new object[] { "0000000", "b", (byte)0b0000000 };

            // Explicit format overrides
            yield return new object[] { "SMTWTFS", "S", (byte)0b1111111 };
            yield return new object[] { "smtwtfs", "s", (byte)0b1111111 };
            yield return new object[] { "MTWTFSS", "M", (byte)0b1111111 };
            yield return new object[] { "mtwtfss", "m", (byte)0b1111111 };

            // Unselected format examples — Sunday-first only.
            // Note: single-char unselected specifiers ('U', 'D', 'E', 'A') imply Sunday-first ordering
            // and do not infer Monday-first from the input. Use two-char specifiers ('MU', 'MD', etc.)
            // for Monday-first inputs.
            yield return new object[] { "S_T_T_S", "U", (byte)0b1010101 };
            yield return new object[] { "S*T*T*S", "A", (byte)0b1010101 };
            yield return new object[] { "s t t s", "e", (byte)0b1010101 };

            // Empty set representations
            yield return new object[] { "       ", null, (byte)0b0000000 };
            yield return new object[] { "-------", null, (byte)0b0000000 };
            yield return new object[] { "*******", null, (byte)0b0000000 };
            yield return new object[] { "_______", null, (byte)0b0000000 };
            yield return new object[] { "0000000", "b", (byte)0b0000000 };
            yield return new object[] { "       ", "se", (byte)0b0000000 };
            yield return new object[] { "-------", "sD", (byte)0b0000000 };
            yield return new object[] { "*******", "sa", (byte)0b0000000 };
            yield return new object[] { "_______", "mu", (byte)0b0000000 };
            yield return new object[] { "       ", "e", (byte)0b0000000 };
            yield return new object[] { "-------", "D", (byte)0b0000000 };
            yield return new object[] { "*******", "a", (byte)0b0000000 };
            yield return new object[] { "_______", "u", (byte)0b0000000 };
        }

        public static IEnumerable<object[]> GetValidParseInputWithExplicitFormatTestData() =>
            GetValidParseInputTestData()
                .Where(o => o[1] != null);

        /// <summary>
        /// Generates all 128 valid permutations of <see cref="DaysOfWeekSet" /> values as test data,
        /// including both symbol and binary string formats.
        /// </summary>
        /// <returns>An enumerable of [byte bitmask, string symbolFormat, string binaryFormat].</returns>
        private static IEnumerable<object[]> GetAllBitmaskPermutationTestData()
        {
            char[] symbols = new[] { 'S', 'M', 'T', 'W', 'T', 'F', 'S' }; // Sunday-first

            for (int mask = 0b0000000; mask <= 0b1111111; mask++)
            {
                var symbolBuilder = new char[7];
                var binaryBuilder = new char[7];

                for (int i = 0; i < 7; i++)
                {
                    bool bitSet = ((mask >> (6 - i)) & 1) == 1;

                    symbolBuilder[i] = bitSet ? symbols[i] : '_';
                    binaryBuilder[i] = bitSet ? '1' : '0';
                }

                string symbol = new(symbolBuilder);
                string binary = new(binaryBuilder);

                yield return new object[] { (byte)mask, symbol, binary };
            }
        }
    }
}