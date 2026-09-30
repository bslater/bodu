// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2FillDriver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Reflection.Emit;

namespace Bodu.Security.Cryptography.Benchmarks;

/// <summary>
/// Derives Argon2id tags through the library's internal <c>Argon2Core</c> with fill options the caller names, so the
/// Argon2 sweep can divide slices among threads below the segment length at which the library starts to, and the kernel
/// cases can time each compression kernel on its own.
/// </summary>
/// <remarks>
/// The core and its <c>FillOptions</c> are internal to the library, so a dynamic method bound to the library's module
/// constructs the options and calls <c>DeriveTag</c>; the derivation itself is the library's own compiled code.
/// </remarks>
internal sealed class Argon2FillDriver
{
    /// <summary>The value of <c>Argon2Type.Argon2id</c>.</summary>
    private const int Argon2id = 2;

    /// <summary>The value of <c>Argon2Core.KernelKind.Auto</c>: the kernel dispatch selects.</summary>
    private const int AutoKernel = 0;

    /// <summary>The value of <c>FillOptions.DefaultMinimumParallelSegmentLength</c>.</summary>
    private const int DefaultMinimumParallelSegmentLength = 256;

    /// <summary>The emitted method: derives a tag with the named fill options.</summary>
    private readonly DeriveFunction _derive;

    /// <summary>
    /// Initializes a new instance of the <see cref="Argon2FillDriver" /> class.
    /// </summary>
    /// <param name="derive">The emitted method that derives a tag with named fill options.</param>
    /// <param name="kernels">The kernels the processor supports, by name and value.</param>
    private Argon2FillDriver(DeriveFunction derive, IReadOnlyList<(string Name, int Value)> kernels)
    {
        _derive = derive;
        Kernels = kernels;
    }

    /// <summary>
    /// Derives a tag with the specified fill options.
    /// </summary>
    /// <param name="type">The variant's value in the library's <c>Argon2Type</c>.</param>
    /// <param name="parameters">The cost parameters.</param>
    /// <param name="password">The password.</param>
    /// <param name="salt">The salt.</param>
    /// <param name="tag">Receives the tag; its length is the parameters' tag length.</param>
    /// <param name="maxDegreeOfParallelism">The greatest number of threads the fill may use, or -1 for the library's choice.</param>
    /// <param name="minimumParallelSegmentLength">The shortest segment, in blocks, whose slice is divided among threads.</param>
    /// <param name="kernel">The kernel's value in the library's <c>Argon2Core.KernelKind</c>.</param>
    private delegate void DeriveFunction(
        int type,
        Argon2Parameters parameters,
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        Span<byte> tag,
        int maxDegreeOfParallelism,
        int minimumParallelSegmentLength,
        int kernel);

    /// <summary>
    /// Gets the compression kernels the processor supports, by name and value, other than <c>Auto</c>.
    /// </summary>
    internal IReadOnlyList<(string Name, int Value)> Kernels { get; }

    /// <summary>
    /// Creates a driver over the library's <c>Argon2Core</c>.
    /// </summary>
    /// <returns>The driver.</returns>
    /// <exception cref="MissingMemberException">The library has no core of the shape the driver calls.</exception>
    internal static Argon2FillDriver Create()
    {
        const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;

        Assembly library = typeof(Argon2Parameters).Assembly;
        Type core = library.GetType("Bodu.Security.Cryptography.Argon2Core")
            ?? throw new MissingMemberException("Bodu.Security.Cryptography.Argon2Core");
        Type type = library.GetType("Bodu.Security.Cryptography.Argon2Type")
            ?? throw new MissingMemberException("Bodu.Security.Cryptography.Argon2Type");
        Type options = core.GetNestedType("FillOptions", BindingFlags.NonPublic)
            ?? throw new MissingMemberException(core.FullName, "FillOptions");
        Type kind = core.GetNestedType("KernelKind", BindingFlags.NonPublic)
            ?? throw new MissingMemberException(core.FullName, "KernelKind");
        ConstructorInfo create = options.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(int), typeof(int), kind])
            ?? throw new MissingMethodException(options.FullName, ".ctor");
        MethodInfo isSupported = core.GetMethod("IsSupported", Static, [kind])
            ?? throw new MissingMethodException(core.FullName, "IsSupported");
        MethodInfo deriveTag = core.GetMethod(
            "DeriveTag",
            Static,
            [type, typeof(Argon2Parameters), typeof(ReadOnlySpan<byte>), typeof(ReadOnlySpan<byte>), typeof(Span<byte>), options])
            ?? throw new MissingMethodException(core.FullName, "DeriveTag");

        var method = new DynamicMethod(
            "Argon2FillTag",
            typeof(void),
            [typeof(int), typeof(Argon2Parameters), typeof(ReadOnlySpan<byte>), typeof(ReadOnlySpan<byte>), typeof(Span<byte>), typeof(int), typeof(int), typeof(int)],
            core.Module,
            skipVisibility: true);
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Ldarg_S, (byte)4);
        il.Emit(OpCodes.Ldarg_S, (byte)5);
        il.Emit(OpCodes.Ldarg_S, (byte)6);
        il.Emit(OpCodes.Ldarg_S, (byte)7);
        il.Emit(OpCodes.Newobj, create);
        il.Emit(OpCodes.Call, deriveTag);
        il.Emit(OpCodes.Ret);

        var kernels = new List<(string Name, int Value)>();
        foreach (string name in Enum.GetNames(kind))
        {
            object value = Enum.Parse(kind, name);
            if (name != "Auto" && (bool)isSupported.Invoke(null, [value])!)
                kernels.Add((name, Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)));
        }

        return new Argon2FillDriver((DeriveFunction)method.CreateDelegate(typeof(DeriveFunction)), kernels);
    }

    /// <summary>
    /// Derives an Argon2id tag with the library's thread bound and kernel, dividing each slice among threads once its
    /// segments reach the specified length.
    /// </summary>
    /// <param name="parameters">The cost parameters.</param>
    /// <param name="password">The password.</param>
    /// <param name="salt">The salt.</param>
    /// <param name="minimumParallelSegmentLength">The shortest segment, in blocks, whose slice is divided among threads.</param>
    /// <returns>The tag.</returns>
    internal byte[] DeriveKey(Argon2Parameters parameters, ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int minimumParallelSegmentLength)
    {
        byte[] tag = new byte[parameters.TagLength];
        _derive(Argon2id, parameters, password, salt, tag, -1, minimumParallelSegmentLength, AutoKernel);
        return tag;
    }

    /// <summary>
    /// Derives an Argon2id tag on the calling thread through the specified compression kernel.
    /// </summary>
    /// <param name="parameters">The cost parameters.</param>
    /// <param name="password">The password.</param>
    /// <param name="salt">The salt.</param>
    /// <param name="kernel">The kernel's value, from <see cref="Kernels" />.</param>
    /// <returns>The tag.</returns>
    internal byte[] DeriveKeyWithKernel(Argon2Parameters parameters, ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int kernel)
    {
        byte[] tag = new byte[parameters.TagLength];
        _derive(Argon2id, parameters, password, salt, tag, 1, DefaultMinimumParallelSegmentLength, kernel);
        return tag;
    }
}
