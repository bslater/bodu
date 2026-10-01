// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305KernelDriver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Reflection.Emit;

namespace Bodu.Security.Cryptography.Benchmarks;

/// <summary>
/// Computes Poly1305 tags through the library's internal <c>Poly1305Core</c> with a kernel the caller names, so the
/// crypto harness can time every kernel at every length, whatever dispatch would select.
/// </summary>
/// <remarks>
/// The core and its kernels are internal to the library, so a dynamic method bound to the library's module calls
/// them: <c>Initialize</c> with the key, <c>Update</c> with the named kernel, and <c>Finish</c>. The kernels are the
/// library's own compiled code; only the three calls around them are emitted here.
/// </remarks>
internal sealed class Poly1305KernelDriver
{
    /// <summary>The emitted method: computes a tag through the named kernel.</summary>
    private readonly TagFunction _computeTag;

    /// <summary>
    /// Initializes a new instance of the <see cref="Poly1305KernelDriver" /> class.
    /// </summary>
    /// <param name="computeTag">The emitted method that computes a tag through a named kernel.</param>
    /// <param name="kernels">The kernels the processor supports, by name and value.</param>
    private Poly1305KernelDriver(TagFunction computeTag, IReadOnlyList<(string Name, int Value)> kernels)
    {
        _computeTag = computeTag;
        Kernels = kernels;
    }

    /// <summary>
    /// Computes a tag through the kernel with the specified value.
    /// </summary>
    /// <param name="key">The 32-byte one-time key.</param>
    /// <param name="kernel">The kernel's value in the library's <c>Poly1305Core.KernelKind</c>.</param>
    /// <param name="message">The message.</param>
    /// <param name="tag">The 16 bytes that receive the tag.</param>
    private delegate void TagFunction(ReadOnlySpan<byte> key, int kernel, ReadOnlySpan<byte> message, Span<byte> tag);

    /// <summary>
    /// Gets the kernels the processor supports, the scalar loop first, each by its name and its value in the library's
    /// <c>Poly1305Core.KernelKind</c>.
    /// </summary>
    internal IReadOnlyList<(string Name, int Value)> Kernels { get; }

    /// <summary>
    /// Creates a driver over the library's <c>Poly1305Core</c>.
    /// </summary>
    /// <returns>The driver.</returns>
    /// <exception cref="MissingMemberException">The library has no core of the shape the driver calls.</exception>
    internal static Poly1305KernelDriver Create()
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;

        Type core = typeof(Poly1305).Assembly.GetType("Bodu.Security.Cryptography.Poly1305Core")
            ?? throw new MissingMemberException("Bodu.Security.Cryptography.Poly1305Core");
        Type kind = core.GetNestedType("KernelKind", BindingFlags.NonPublic)
            ?? throw new MissingMemberException(core.FullName, "KernelKind");
        MethodInfo initialize = core.GetMethod("Initialize", Instance, [typeof(ReadOnlySpan<byte>)])
            ?? throw new MissingMethodException(core.FullName, "Initialize");
        MethodInfo update = core.GetMethod("Update", Instance, [kind, typeof(ReadOnlySpan<byte>)])
            ?? throw new MissingMethodException(core.FullName, "Update");
        MethodInfo finish = core.GetMethod("Finish", Instance, [typeof(Span<byte>)])
            ?? throw new MissingMethodException(core.FullName, "Finish");
        MethodInfo isSupported = core.GetMethod("IsSupported", Static, [kind])
            ?? throw new MissingMethodException(core.FullName, "IsSupported");

        var method = new DynamicMethod(
            "Poly1305KernelTag",
            typeof(void),
            [typeof(ReadOnlySpan<byte>), typeof(int), typeof(ReadOnlySpan<byte>), typeof(Span<byte>)],
            core.Module,
            skipVisibility: true);
        ILGenerator il = method.GetILGenerator();
        LocalBuilder state = il.DeclareLocal(core);
        il.Emit(OpCodes.Ldloca, state);
        il.Emit(OpCodes.Initobj, core);
        il.Emit(OpCodes.Ldloca, state);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, initialize);
        il.Emit(OpCodes.Ldloca, state);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, update);
        il.Emit(OpCodes.Ldloca, state);
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Call, finish);
        il.Emit(OpCodes.Ret);

        var kernels = new List<(string Name, int Value)>();
        foreach (string name in Enum.GetNames(kind))
        {
            object value = Enum.Parse(kind, name);
            if (name != "Auto" && (bool)isSupported.Invoke(null, [value])!)
                kernels.Add((name, Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)));
        }

        return new Poly1305KernelDriver((TagFunction)method.CreateDelegate(typeof(TagFunction)), kernels);
    }

    /// <summary>
    /// Computes a tag through the specified kernel.
    /// </summary>
    /// <param name="kernel">The kernel's value, from <see cref="Kernels" />.</param>
    /// <param name="key">The 32-byte one-time key.</param>
    /// <param name="message">The message.</param>
    /// <param name="tag">The 16 bytes that receive the tag.</param>
    internal void ComputeTag(int kernel, ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, Span<byte> tag) =>
        _computeTag(key, kernel, message, tag);
}
