// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetTests.DifferentialSweep.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BitSetTests
{
    /// <summary>The exclusive upper bound of the bit universe exercised by the differential sweeps.</summary>
    private const int SweepUniverse = 2048;

    /// <summary>
    /// Verifies that 10,000 seeded random mutations — single-bit set/clear/flip, the value overload, and the three
    /// range operations — leave the <see cref="BitSet" /> in exactly the state predicted by a <see cref="bool" />
    /// -array oracle, including full bit state, cardinality, length, next-set/next-clear walks, and enumeration
    /// order.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void BitSet_WhenRandomMutationSweepApplied_ShouldMatchBoolArrayOracle()
    {
        var random = new Random(20260705);
        var bits = new BitSet();
        var oracle = new bool[SweepUniverse];

        for (var op = 0; op < 10_000; op++)
        {
            var index = random.Next(SweepUniverse);
            switch (random.Next(7))
            {
                case 0:
                    bits.Set(index);
                    oracle[index] = true;
                    break;

                case 1:
                    bits.Clear(index);
                    oracle[index] = false;
                    break;

                case 2:
                    bits.Flip(index);
                    oracle[index] = !oracle[index];
                    break;

                case 3:
                    var value = random.Next(2) == 0;
                    bits.Set(index, value);
                    oracle[index] = value;
                    break;

                case 4:
                    var (setFrom, setTo) = NextRange(random);
                    bits.Set(setFrom, setTo);
                    Array.Fill(oracle, true, setFrom, setTo - setFrom);
                    break;

                case 5:
                    var (clearFrom, clearTo) = NextRange(random);
                    bits.Clear(clearFrom, clearTo);
                    Array.Fill(oracle, false, clearFrom, clearTo - clearFrom);
                    break;

                default:
                    var (flipFrom, flipTo) = NextRange(random);
                    bits.Flip(flipFrom, flipTo);
                    for (var i = flipFrom; i < flipTo; i++)
                        oracle[i] = !oracle[i];
                    break;
            }
        }

        AssertMatchesOracle(bits, oracle);
    }

    /// <summary>
    /// Verifies that each in-place logical operation — And, Or, Xor, AndNot — and the Intersects query produce
    /// exactly the oracle-computed result for seeded random operands of differing lengths, in both operand orders.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void BitSet_WhenLogicalOpsAppliedToRandomOperands_ShouldMatchOracle()
    {
        var random = new Random(19570301);

        for (var trial = 0; trial < 50; trial++)
        {
            var (leftBits, leftOracle) = CreateRandomOperand(random);
            var (rightBits, rightOracle) = CreateRandomOperand(random);

            var and = new BitSet(leftBits);
            and.And(rightBits);
            AssertMatchesOracle(and, CombineOracles(leftOracle, rightOracle, static (a, b) => a && b));

            var or = new BitSet(leftBits);
            or.Or(rightBits);
            AssertMatchesOracle(or, CombineOracles(leftOracle, rightOracle, static (a, b) => a || b));

            var xor = new BitSet(leftBits);
            xor.Xor(rightBits);
            AssertMatchesOracle(xor, CombineOracles(leftOracle, rightOracle, static (a, b) => a ^ b));

            var andNot = new BitSet(leftBits);
            andNot.AndNot(rightBits);
            AssertMatchesOracle(andNot, CombineOracles(leftOracle, rightOracle, static (a, b) => a && !b));

            var expectedIntersects = false;
            for (var i = 0; i < SweepUniverse; i++)
                expectedIntersects |= leftOracle[i] && rightOracle[i];

            Assert.AreEqual(expectedIntersects, leftBits.Intersects(rightBits));
            Assert.AreEqual(expectedIntersects, rightBits.Intersects(leftBits));
        }
    }

    /// <summary>
    /// Asserts that the specified <see cref="BitSet" /> agrees with the oracle on every observable surface: per-bit
    /// state, cardinality, logical length, the ascending enumeration, a full <see cref="BitSet.NextSetBit" /> walk,
    /// and <see cref="BitSet.NextClearBit" /> probes.
    /// </summary>
    /// <param name="bits">The set under test.</param>
    /// <param name="oracle">The expected per-bit state over the sweep universe.</param>
    private static void AssertMatchesOracle(BitSet bits, bool[] oracle)
    {
        var expectedIndices = new List<int>();
        for (var i = 0; i < SweepUniverse; i++)
        {
            Assert.AreEqual(oracle[i], bits.Get(i), $"Bit {i} disagrees with the oracle.");
            if (oracle[i])
                expectedIndices.Add(i);
        }

        Assert.AreEqual(expectedIndices.Count, bits.Cardinality);
        Assert.AreEqual(expectedIndices.Count == 0 ? 0 : expectedIndices[^1] + 1, bits.Length);
        CollectionAssert.AreEqual(expectedIndices, bits.ToArray());

        var walked = new List<int>();
        for (var i = bits.NextSetBit(0); i >= 0; i = bits.NextSetBit(i + 1))
            walked.Add(i);

        CollectionAssert.AreEqual(expectedIndices, walked);

        foreach (var probe in new[] { 0, 1, 63, 64, 65, SweepUniverse / 2, SweepUniverse - 1 })
        {
            var expectedClear = probe;
            while (expectedClear < SweepUniverse && oracle[expectedClear])
                expectedClear++;

            Assert.AreEqual(expectedClear, bits.NextClearBit(probe), $"NextClearBit({probe}) disagrees with the oracle.");
        }
    }

    /// <summary>
    /// Combines two oracles bit-wise with the specified operation.
    /// </summary>
    /// <param name="left">The left operand oracle.</param>
    /// <param name="right">The right operand oracle.</param>
    /// <param name="operation">The per-bit combination.</param>
    /// <returns>The combined oracle.</returns>
    private static bool[] CombineOracles(bool[] left, bool[] right, Func<bool, bool, bool> operation)
    {
        var result = new bool[SweepUniverse];
        for (var i = 0; i < SweepUniverse; i++)
            result[i] = operation(left[i], right[i]);

        return result;
    }

    /// <summary>
    /// Creates a random logical-op operand of random density and random length, together with its oracle.
    /// </summary>
    /// <param name="random">The seeded source of randomness.</param>
    /// <returns>The operand and the matching per-bit oracle.</returns>
    private static (BitSet Bits, bool[] Oracle) CreateRandomOperand(Random random)
    {
        var bits = new BitSet(random.Next(2) == 0 ? 0 : 64);
        var oracle = new bool[SweepUniverse];

        // A random exclusive bound makes operand lengths differ across trials, so both the
        // shorter-than and longer-than paths of every logical op are exercised in both orders.
        var bound = random.Next(1, SweepUniverse + 1);
        var density = random.NextDouble();
        for (var i = 0; i < bound; i++)
        {
            if (random.NextDouble() < density)
            {
                bits.Set(i);
                oracle[i] = true;
            }
        }

        return (bits, oracle);
    }

    /// <summary>
    /// Produces a random non-empty-or-empty half-open range within the sweep universe.
    /// </summary>
    /// <param name="random">The seeded source of randomness.</param>
    /// <returns>A pair where the first component is ≤ the second and both lie within the universe.</returns>
    private static (int From, int To) NextRange(Random random)
    {
        var a = random.Next(SweepUniverse + 1);
        var b = random.Next(SweepUniverse + 1);
        return a <= b ? (a, b) : (b, a);
    }
}
