// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlakeKernelDriver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography.Benchmarks;

/// <summary>
/// Compresses runs of blocks through one of the library's BLAKE2s, BLAKE2b and BLAKE3 compression kernels, named by the
/// caller, so the crypto harness can time every kernel the processor supports, whatever dispatch would select.
/// </summary>
/// <remarks>
/// <para>
/// The cores and their kernels are internal to the library, so a dynamic method bound to the library's module calls the
/// core's <c>Compress</c> overload that takes the kernel, once per block: <c>Blake2sCore</c>'s and <c>Blake2bCore</c>'s
/// with the finalization flag clear, and <c>Blake3Core</c>'s with a full block and no flags. The kernels are the
/// library's own compiled code; only the call around each is emitted here.
/// </para>
/// <para>
/// This is how the kernels that dispatch never selects are timed: on ARM64, the AdvSimd kernels of BLAKE2s, BLAKE2b and
/// BLAKE3's single block, which <c>SimdCapabilities.AdvSimdSingleState</c> holds back.
/// </para>
/// </remarks>
internal sealed class BlakeKernelDriver
{
    /// <summary>The emitted method: compresses one block through the named kernel.</summary>
    private readonly BlockFunction _compress;

    /// <summary>The chaining state the blocks are compressed into, as bytes: room for eight 64-bit words.</summary>
    private readonly byte[] _state = new byte[64];

    /// <summary>
    /// Initializes a new instance of the <see cref="BlakeKernelDriver" /> class.
    /// </summary>
    /// <param name="hash">The hash whose kernels the driver runs.</param>
    /// <param name="blockBytes">The length of the hash's block.</param>
    /// <param name="compress">The emitted method that compresses one block through a named kernel.</param>
    /// <param name="kernels">The kernels the processor supports, by name and value.</param>
    private BlakeKernelDriver(string hash, int blockBytes, BlockFunction compress, IReadOnlyList<(string Name, int Value)> kernels)
    {
        Hash = hash;
        BlockBytes = blockBytes;
        _compress = compress;
        Kernels = kernels;
    }

    /// <summary>
    /// Compresses one block through the kernel with the specified value.
    /// </summary>
    /// <param name="kernel">The kernel's value in the core's <c>KernelKind</c>.</param>
    /// <param name="state">The chaining state, as bytes.</param>
    /// <param name="block">The block.</param>
    /// <param name="counter">The block's counter.</param>
    private delegate void BlockFunction(int kernel, Span<byte> state, ReadOnlySpan<byte> block, ulong counter);

    /// <summary>
    /// Gets the name of the hash whose kernels the driver runs: <c>BLAKE2s</c>, <c>BLAKE2b</c> or <c>BLAKE3</c>.
    /// </summary>
    internal string Hash { get; }

    /// <summary>
    /// Gets the length, in bytes, of the hash's block.
    /// </summary>
    internal int BlockBytes { get; }

    /// <summary>
    /// Gets the kernels the processor supports, the scalar kernel first, each by its name and its value in the core's
    /// <c>KernelKind</c>.
    /// </summary>
    /// <remarks>
    /// BLAKE3's <c>Avx2</c> and <c>Avx512Wide</c> kinds are left out: a single block runs on the <c>Ssse3</c> and
    /// <c>Avx512</c> kernels in their place.
    /// </remarks>
    internal IReadOnlyList<(string Name, int Value)> Kernels { get; }

    /// <summary>
    /// Creates a driver over each of the library's three BLAKE cores.
    /// </summary>
    /// <returns>The drivers for BLAKE2s, BLAKE2b and BLAKE3, in that order.</returns>
    /// <exception cref="MissingMemberException">The library has no core of the shape a driver calls.</exception>
    internal static IReadOnlyList<BlakeKernelDriver> CreateAll() =>
    [
        Create("BLAKE2s", "Blake2sCore", 64, typeof(uint), isBlake3: false),
        Create("BLAKE2b", "Blake2bCore", 128, typeof(ulong), isBlake3: false),
        Create("BLAKE3", "Blake3Core", 64, typeof(uint), isBlake3: true),
    ];

    /// <summary>
    /// Compresses every whole block of the input through the specified kernel.
    /// </summary>
    /// <param name="kernel">The kernel's value, from <see cref="Kernels" />.</param>
    /// <param name="input">The blocks.</param>
    internal void CompressAll(int kernel, ReadOnlySpan<byte> input)
    {
        Span<byte> state = _state;
        for (int offset = 0; offset + BlockBytes <= input.Length; offset += BlockBytes)
            _compress(kernel, state, input.Slice(offset, BlockBytes), (ulong)(offset + BlockBytes));
    }

    /// <summary>
    /// Creates a driver over one of the library's BLAKE cores.
    /// </summary>
    /// <param name="hash">The hash's name.</param>
    /// <param name="coreName">The name of the core's type in the library.</param>
    /// <param name="blockBytes">The length of the hash's block.</param>
    /// <param name="word">The type of the chaining state's words.</param>
    /// <param name="isBlake3">
    /// <see langword="true" /> for BLAKE3's core, whose <c>Compress</c> takes a block length and flags; otherwise,
    /// <see langword="false" /> for a BLAKE2 core's, which takes the finalization flag.
    /// </param>
    /// <returns>The driver.</returns>
    /// <exception cref="MissingMemberException">The library has no core of the shape the driver calls.</exception>
    private static BlakeKernelDriver Create(string hash, string coreName, int blockBytes, Type word, bool isBlake3)
    {
        const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;

        Type core = typeof(Blake2s).Assembly.GetType("Bodu.Security.Cryptography." + coreName)
            ?? throw new MissingMemberException("Bodu.Security.Cryptography." + coreName);
        Type kind = core.GetNestedType("KernelKind", BindingFlags.NonPublic)
            ?? throw new MissingMemberException(core.FullName, "KernelKind");
        Type state = typeof(Span<>).MakeGenericType(word);
        Type[] parameters = isBlake3
            ? [kind, state, typeof(ReadOnlySpan<byte>), typeof(ulong), typeof(uint), typeof(uint)]
            : [kind, state, typeof(ReadOnlySpan<byte>), typeof(ulong), typeof(bool)];
        MethodInfo compress = core.GetMethod("Compress", Static, parameters)
            ?? throw new MissingMethodException(core.FullName, "Compress");
        MethodInfo isSupported = core.GetMethod("IsSupported", Static, [kind])
            ?? throw new MissingMethodException(core.FullName, "IsSupported");
        MethodInfo cast = typeof(MemoryMarshal).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Single(m => m.Name == nameof(MemoryMarshal.Cast) && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Span<>))
            .MakeGenericMethod(typeof(byte), word);

        var method = new DynamicMethod(
            hash + "KernelBlock",
            typeof(void),
            [typeof(int), typeof(Span<byte>), typeof(ReadOnlySpan<byte>), typeof(ulong)],
            core.Module,
            skipVisibility: true);
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, cast);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldarg_3);
        if (isBlake3)
        {
            // A full block, and no flags.
            il.Emit(OpCodes.Ldc_I4, blockBytes);
            il.Emit(OpCodes.Ldc_I4_0);
        }
        else
        {
            // Not the final block.
            il.Emit(OpCodes.Ldc_I4_0);
        }

        il.Emit(OpCodes.Call, compress);
        il.Emit(OpCodes.Ret);

        var kernels = new List<(string Name, int Value)>();
        foreach (string name in Enum.GetNames(kind))
        {
            object value = Enum.Parse(kind, name);
            bool alias = isBlake3 && name is "Avx2" or "Avx512Wide";
            if (name != "Auto" && !alias && (bool)isSupported.Invoke(null, [value])!)
                kernels.Add((name, Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)));
        }

        return new BlakeKernelDriver(hash, blockBytes, (BlockFunction)method.CreateDelegate(typeof(BlockFunction)), kernels);
    }
}
