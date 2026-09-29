// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakSponge4Tests.Squeeze.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class KeccakSponge4Tests
{
    /// <summary>
    /// Verifies that squeezing one, two or three blocks, in one call or across several, continues each sponge's stream
    /// exactly as a scalar sponge squeezes it, through each kernel and both SHAKE functions, over seeded messages of
    /// several lengths.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="rate">The rate: 168 for SHAKE128, 136 for SHAKE256.</param>
    [TestMethod]
    [DataRow("Scalar", 168)]
    [DataRow("Scalar", 136)]
    [DataRow("Avx2", 168)]
    [DataRow("Avx2", 136)]
    [DataRow("Avx512", 168)]
    [DataRow("Avx512", 136)]
    public void Squeeze_WhenBlocksAreSqueezedInOneCallOrSeveral_ForEachKernel_ShouldMatchFourScalarSponges(string kernel, int rate)
    {
        KeccakPermutation.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x4A_1000 + rate);
        int[][] splits = [[1], [2], [3], [1, 1], [1, 2], [2, 1], [1, 1, 1]];

        foreach (int length in new[] { 0, 1, 33, 34, 66, rate - 1 })
        {
            byte[][] messages = Messages(random, length);
            foreach (int[] split in splits)
            {
                int total = split.Sum() * rate;
                byte[][] streams = Enumerable.Range(0, KeccakSponge4.Ways).Select(_ => new byte[total]).ToArray();

                KeccakSponge4 sponge = Create(rate, kind);
                sponge.Absorb(messages[0], messages[1], messages[2], messages[3]);

                int offset = 0;
                foreach (int blocks in split)
                {
                    int bytes = blocks * rate;
                    sponge.Squeeze(
                        streams[0].AsSpan(offset, bytes),
                        streams[1].AsSpan(offset, bytes),
                        streams[2].AsSpan(offset, bytes),
                        streams[3].AsSpan(offset, bytes));
                    offset += bytes;
                }

                for (int lane = 0; lane < KeccakSponge4.Ways; lane++)
                    CollectionAssert.AreEqual(ScalarStream(rate, messages[lane], total), streams[lane], $"length {length}, split {string.Join('+', split)}, sponge {lane}");
            }
        }
    }

    /// <summary>
    /// Verifies that a squeeze that is not a whole, positive number of blocks is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> naming the first destination.
    /// </summary>
    /// <param name="length">The rejected length.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(135)]
    [DataRow(137)]
    public void Squeeze_WhenLengthIsNotAPositiveMultipleOfTheRate_ShouldThrowArgumentOutOfRangeException(int length)
    {
        byte[] destination = new byte[length];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            KeccakSponge4 sponge = KeccakSponge4.CreateShake256(KeccakPermutation.KernelKind.Scalar);
            sponge.Absorb([], [], [], []);
            sponge.Squeeze(destination, destination, destination, destination);
        });

        Assert.AreEqual("destination0", ex.ParamName);
    }

    /// <summary>
    /// Verifies that destinations of different lengths are rejected with <see cref="ArgumentException" /> naming the
    /// first that differs from the first destination.
    /// </summary>
    /// <param name="shortDestination">The position of the destination one block shorter: 1, 2 or 3.</param>
    /// <param name="expectedParamName">The name of the parameter that destination is passed as.</param>
    [TestMethod]
    [DataRow(1, "destination1")]
    [DataRow(2, "destination2")]
    [DataRow(3, "destination3")]
    public void Squeeze_WhenDestinationsDifferInLength_ShouldThrowArgumentException(int shortDestination, string expectedParamName)
    {
        int rate = KeccakSponge.Shake128RateBytes;
        byte[][] destinations = Enumerable.Range(0, KeccakSponge4.Ways).Select(lane => new byte[(lane == shortDestination ? 1 : 2) * rate]).ToArray();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            KeccakSponge4 sponge = KeccakSponge4.CreateShake128(KeccakPermutation.KernelKind.Scalar);
            sponge.Absorb([], [], [], []);
            sponge.Squeeze(destinations[0], destinations[1], destinations[2], destinations[3]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that every four-way kernel, each of its sponges absorbing the same FIPS 202 message, squeezes the
    /// published SHAKE128 and SHAKE256 output in every lane.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void Squeeze_WhenAbsorbingFips202Messages_ForEachKernel_ShouldMatchThePublishedOutput(string kernel)
    {
        KeccakPermutation.KernelKind kind = ParseSupportedKernel(kernel);
        (int Rate, string Message, string Expected)[] vectors =
        [
            (KeccakSponge.Shake128RateBytes, string.Empty, "7F9C2BA4E88F827D616045507605853ED73B8093F6EFBC88EB1A6EACFA66EF26"),
            (KeccakSponge.Shake128RateBytes, "00", "0B784469A0628E03861CD8A196DFAFA0E9E8056D04CDDCC49F0746B9AD43CCB2"),
            (KeccakSponge.Shake128RateBytes, "00010203", "0B0CC28E60E37698B411234B1158A5D42636440432A28E8B8DF5BE04208878F9"),
            (KeccakSponge.Shake128RateBytes, "616263", "5881092DD818BF5CF8A3DDB793FBCBA74097D5C526A6D35F97B83351940F2CC8"),
            (KeccakSponge.Shake256RateBytes, string.Empty, "46B9DD2B0BA88D13233B3FEB743EEB243FCD52EA62B81B82B50C27646ED5762F"),
            (KeccakSponge.Shake256RateBytes, "00", "B8D01DF855F7075882C636F6DDEACF41E5DE0BBF30042EF0A86E36F4B8600D546C516501A6A3C821678D3D9943FA9E74B9B99FCCD47AECC91DD1F4946B8355B3"),
            (KeccakSponge.Shake256RateBytes, "00010203", "48B8D57A5F8C29D0326049216380AA85D2D7A58B784F5A49E980CA93409E3D4BAC25509371F937EF3224820EDA0AF0915C10D07E2DF78BAFE7208D23F36388A9"),
            (KeccakSponge.Shake256RateBytes, "616263", "483366601360A8771C6863080CC4114D8DB44530F8F1E1EE4F94EA37E78B5739D5A15BEF186A5386C75744C0527E1FAA9F8726E462A12A4FEB06BD8801E751E4"),
        ];

        foreach ((int rate, string message, string expected) in vectors)
        {
            byte[] bytes = Convert.FromHexString(message);
            byte[][] blocks = Enumerable.Range(0, KeccakSponge4.Ways).Select(_ => new byte[rate]).ToArray();
            KeccakSponge4 sponge = Create(rate, kind);

            sponge.Absorb(bytes, bytes, bytes, bytes);
            sponge.Squeeze(blocks[0], blocks[1], blocks[2], blocks[3]);

            for (int lane = 0; lane < blocks.Length; lane++)
                Assert.AreEqual(expected, Convert.ToHexString(blocks[lane], 0, expected.Length / 2), $"rate {rate}, message \"{message}\", lane {lane}");
        }
    }
}
