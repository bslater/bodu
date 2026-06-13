// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DebugViewContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Reflection;

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Reusable behavioural contract test base for the debugger-display surface of a collection. Concrete
/// subclasses override <see cref="Create" /> to plug their type into the inherited tests.
/// </summary>
/// <typeparam name="TCollection">The collection type under test.</typeparam>
/// <remarks>
/// <para>
/// Asserts the standard Bodu debugger-display contract: every public collection type that surfaces in the
/// debugger should carry a <see cref="DebuggerDisplayAttribute" /> and a <see cref="DebuggerTypeProxyAttribute" />
/// declaring an inner type that snapshots the collection's elements for the debugger's eager-evaluation
/// pipeline. The base does not assert on the *contents* of the snapshot — that varies per collection — but
/// does assert that the attribute is present and that the proxy is constructible from an instance of the
/// collection.
/// </para>
/// </remarks>
public abstract class DebugViewContractTests<TCollection>
{
    /// <summary>
    /// Creates a representative instance of the collection under test. The instance should be non-empty so
    /// the inherited tests can exercise the snapshot path.
    /// </summary>
    /// <returns>A representative collection instance for debugger-view inspection.</returns>
    protected abstract TCollection Create();

    /// <summary>
    /// Verifies that the collection type carries a <see cref="DebuggerDisplayAttribute" />.
    /// </summary>
    [TestMethod]
    public void Type_ShouldDeclareDebuggerDisplayAttribute()
    {
        DebuggerDisplayAttribute? display = typeof(TCollection).GetCustomAttribute<DebuggerDisplayAttribute>();

        Assert.IsNotNull(display);
        Assert.IsFalse(string.IsNullOrEmpty(display.Value));
    }

    /// <summary>
    /// Verifies that the collection type carries a <see cref="DebuggerTypeProxyAttribute" /> referencing a
    /// proxy type.
    /// </summary>
    [TestMethod]
    public void Type_ShouldDeclareDebuggerTypeProxyAttribute()
    {
        DebuggerTypeProxyAttribute? proxy = typeof(TCollection).GetCustomAttribute<DebuggerTypeProxyAttribute>();

        Assert.IsNotNull(proxy);
        Assert.IsNotNull(proxy.ProxyTypeName);
    }

    /// <summary>
    /// Verifies that the debugger type proxy declared on the collection can be instantiated with an
    /// instance of the collection.
    /// </summary>
    [TestMethod]
    public void DebuggerTypeProxy_WhenConstructedFromInstance_ShouldNotThrow()
    {
        DebuggerTypeProxyAttribute proxyAttribute = typeof(TCollection).GetCustomAttribute<DebuggerTypeProxyAttribute>()
            ?? throw new AssertFailedException("DebuggerTypeProxyAttribute is required for this contract.");

        Type proxyType = ResolveProxyType(proxyAttribute, typeof(TCollection));
        TCollection collection = Create();

        var proxy = Activator.CreateInstance(proxyType, [collection]);
        Assert.IsNotNull(proxy);
    }

    private static Type ResolveProxyType(DebuggerTypeProxyAttribute attribute, Type ownerType)
    {
        if (attribute.Target is not null)
            return attribute.Target;

        var typeName = attribute.ProxyTypeName
            ?? throw new AssertFailedException("DebuggerTypeProxyAttribute carried neither a Target nor a ProxyTypeName.");

        // The proxy may be a nested type expressed as "Outer+Proxy"; try the owner's assembly first.
        Type? resolved = ownerType.Assembly.GetType(typeName, throwOnError: false)
                       ?? Type.GetType(typeName, throwOnError: false);

        if (resolved is null && typeName.StartsWith(ownerType.Namespace ?? string.Empty, StringComparison.Ordinal))
        {
            // The framework's AssemblyQualifiedName is sometimes truncated for generic proxies; try the
            // declaring-type-relative lookup as a fallback.
            var shortName = typeName[(ownerType.Namespace!.Length + 1)..];
            resolved = ownerType.Assembly.GetType($"{ownerType.Namespace}.{shortName}", throwOnError: false);
        }

        if (resolved is null)
            throw new AssertFailedException($"DebuggerTypeProxy type '{typeName}' could not be resolved.");

        if (resolved.IsGenericTypeDefinition && ownerType.IsGenericType)
            resolved = resolved.MakeGenericType(ownerType.GetGenericArguments());

        return resolved;
    }
}
