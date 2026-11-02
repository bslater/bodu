// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoaderTests.LoadFrom.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class NotableDatePluginLoaderTests
{
    /// <summary>
    /// Verifies that a trusted, attributed assembly yields its plugin with the expected identity.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenTrustedAndAttributed_ActivatesThePlugin()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(TestAssembly, new AllowAllPluginTrustPolicy());

        Assert.IsInstanceOfType<TestPlugin>(plugin);
        Assert.AreEqual(
            ("Test Plugin", new Version(1, 2, 3)),
            (plugin.Name, plugin.Version));
    }

    /// <summary>
    /// Verifies that a rejecting trust policy prevents activation.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenTrustRejected_Throws()
    {
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(_ => PluginTrustResult.Rejected("blocked"));

        PluginNotTrustedException ex = Assert.ThrowsExactly<PluginNotTrustedException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(TestAssembly, policy);
        });

        Assert.AreEqual("blocked", ex.Reason);
    }

    /// <summary>
    /// Verifies that a composite policy rejects when any member rejects.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenCompositePolicyHasRejection_Throws()
    {
        IPluginTrustPolicy policy = new CompositePluginTrustPolicy(
            new AllowAllPluginTrustPolicy(),
            new DelegatingPluginTrustPolicy(_ => PluginTrustResult.Rejected("second policy")));

        PluginNotTrustedException ex = Assert.ThrowsExactly<PluginNotTrustedException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(TestAssembly, policy);
        });

        Assert.AreEqual("second policy", ex.Reason);
    }

    /// <summary>
    /// Verifies that an assembly without the plugin attribute is rejected.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenAssemblyHasNoAttribute_Throws()
    {
        Assert.ThrowsExactly<PluginMissingAttributeException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(typeof(string).Assembly, new AllowAllPluginTrustPolicy());
        });
    }

    /// <summary>
    /// Verifies that the loader populates the trust context with the assembly name and the strong-name public-key token
    /// (lowercase hexadecimal, or <see langword="null" /> when the assembly is not signed).
    /// </summary>
    [TestMethod]
    public void LoadFrom_ShouldPopulateTrustContextWithNameAndToken()
    {
        PluginTrustContext? captured = null;
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(context =>
        {
            captured = context;
            return PluginTrustResult.Trusted();
        });

        _ = NotableDatePluginLoader.LoadFrom(TestAssembly, policy);

        byte[]? tokenBytes = TestAssembly.GetName().GetPublicKeyToken();
        string? expectedToken = tokenBytes is null || tokenBytes.Length == 0
            ? null
            : Convert.ToHexString(tokenBytes).ToLowerInvariant();

        Assert.IsNotNull(captured);
        Assert.AreEqual(
            (TestAssembly.GetName().Name, (string?)expectedToken),
            ((string?)captured!.AssemblyName, captured.PublicKeyToken));
    }

    /// <summary>
    /// Verifies that the file-path <see cref="NotableDatePluginLoader.LoadFrom(string, IPluginTrustPolicy, Microsoft.Extensions.Logging.ILogger)" /> overload
    /// loads the assembly from disk and activates its plugin.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenGivenAssemblyPath_ShouldActivateThePlugin()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(TestAssembly.Location, new AllowAllPluginTrustPolicy());

        Assert.AreEqual("Test Plugin", plugin.Name);
    }

    /// <summary>
    /// Verifies that a trust policy rejecting without a reason still prevents activation, names the assembly, and
    /// leaves the exception's reason unset.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenTrustRejectedWithoutAReason_ShouldThrowWithNullReason()
    {
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(_ => new PluginTrustResult(false, null));

        PluginNotTrustedException ex = Assert.ThrowsExactly<PluginNotTrustedException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(TestAssembly, policy);
        });

        Assert.AreEqual(TestAssembly.GetName().Name, ex.AssemblyName);
        Assert.IsNull(ex.Reason);
    }

    /// <summary>
    /// Verifies that an assembly loaded from its bytes, which has no location, reaches the trust policy with a
    /// <see langword="null" /> path and no file hash while keeping its name and strong-name token.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenAssemblyIsLoadedFromBytes_ShouldPassNullPathAndHashToTrustPolicy()
    {
        Assembly assembly = LoadFixturePluginFromBytes();
        Assert.AreEqual(string.Empty, assembly.Location, "An assembly loaded from bytes has no location.");
        byte[]? tokenBytes = assembly.GetName().GetPublicKeyToken();
        string? expectedToken = tokenBytes is null || tokenBytes.Length == 0
            ? null
            : Convert.ToHexString(tokenBytes).ToLowerInvariant();
        PluginTrustContext? captured = null;
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(context =>
        {
            captured = context;
            return PluginTrustResult.Trusted();
        });

        _ = NotableDatePluginLoader.LoadFrom(assembly, policy);

        Assert.IsNotNull(captured);
        Assert.AreEqual(
            ("Bodu.Globalization.Calendar.Plugins.TestPlugin", (string?)null, (byte[]?)null, expectedToken),
            (captured!.AssemblyName, captured.AssemblyPath, captured.FileHash, captured.PublicKeyToken));
    }

    /// <summary>
    /// Verifies that a trusted assembly loaded from its bytes activates the plugin declared by that in-memory
    /// image rather than by any copy of the fixture on disk.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenAssemblyIsLoadedFromBytes_ShouldActivateThePluginFromThatImage()
    {
        Assembly assembly = LoadFixturePluginFromBytes();

        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(assembly, new AllowAllPluginTrustPolicy());

        Assert.AreEqual(("Fixture Plugin", new Version(1, 0, 0)), (plugin.Name, plugin.Version));
        Assert.AreSame(assembly, plugin.GetType().Assembly, "The plugin must come from the in-memory image.");
    }

    /// <summary>
    /// Verifies that a trusted dynamic assembly activates the plugin type it defines, so an entry point that exists
    /// only in memory is activated like one from a file.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenDynamicAssemblyIsTrusted_ShouldActivateItsPlugin()
    {
        (AssemblyBuilder assembly, Type entryPoint) = DefineDynamicPluginAssembly(
            "Bodu.Globalization.Calendar.Plugins.Test.Dynamic.Plugin",
            DefinePluginEntryPoint);

        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(assembly, new AllowAllPluginTrustPolicy());

        Assert.AreEqual(("Dynamic Plugin", new Version(2, 0, 1)), (plugin.Name, plugin.Version));
        Assert.AreSame(entryPoint, plugin.GetType());
        Assert.IsTrue(plugin.GetType().Assembly.IsDynamic, "The plugin must be an instance of the dynamic type.");
    }

    /// <summary>
    /// Verifies that a dynamic assembly, which is unsigned and has no location, reaches the trust policy with its
    /// name alone: a <see langword="null" /> token for its empty public-key token, and no path or hash.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenDynamicAssemblyIsUnsigned_ShouldPassNullTokenPathAndHashToTrustPolicy()
    {
        const string name = "Bodu.Globalization.Calendar.Plugins.Test.Dynamic.Unsigned";
        (AssemblyBuilder assembly, _) = DefineDynamicPluginAssembly(name, DefinePluginEntryPoint);
        byte[]? tokenBytes = assembly.GetName().GetPublicKeyToken();
        Assert.IsNotNull(tokenBytes);
        Assert.AreEqual(0, tokenBytes!.Length, "An unsigned dynamic assembly reports an empty token, not a missing one.");
        Assert.AreEqual(string.Empty, assembly.Location, "A dynamic assembly has no location.");
        PluginTrustContext? captured = null;
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(context =>
        {
            captured = context;
            return PluginTrustResult.Trusted();
        });

        _ = NotableDatePluginLoader.LoadFrom(assembly, policy);

        Assert.IsNotNull(captured);
        Assert.AreEqual(
            (name, (string?)null, (byte[]?)null, (string?)null),
            (captured!.AssemblyName, captured.AssemblyPath, captured.FileHash, captured.PublicKeyToken));
    }

    /// <summary>
    /// Verifies that a dynamic entry point whose constructor throws is reported as
    /// <see cref="PluginActivationException" /> naming the type, carrying it as
    /// <see cref="PluginActivationException.PluginType" /> and the constructor's failure in the inner exception
    /// chain.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenDynamicPluginConstructorThrows_ShouldThrowPluginActivationExceptionWithTheCause()
    {
        (AssemblyBuilder assembly, Type entryPoint) = DefineDynamicPluginAssembly(
            "Bodu.Globalization.Calendar.Plugins.Test.Dynamic.Faulting",
            DefineFaultingEntryPoint);

        PluginActivationException ex = Assert.ThrowsExactly<PluginActivationException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(assembly, new AllowAllPluginTrustPolicy());
        });

        Assert.AreSame(entryPoint, ex.PluginType);
        Assert.AreEqual(
            string.Format(
                CultureInfo.CurrentCulture,
                PluginsResourceStrings.Op_Invalid_PluginActivation,
                "Dynamic.FaultingPlugin"),
            ex.Message);
        Assert.IsInstanceOfType<TargetInvocationException>(ex.InnerException);
        Assert.IsInstanceOfType<InvalidOperationException>(ex.InnerException!.InnerException);
        Assert.AreEqual("The dynamic plugin's constructor faults.", ex.InnerException.InnerException!.Message);
    }

    /// <summary>
    /// Verifies that a dynamic entry point that activates but does not implement <see cref="INotableDatePlugin" />
    /// is rejected with <see cref="PluginActivationException" /> naming the type, with no inner exception because
    /// nothing failed.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenDynamicEntryPointIsNotAPlugin_ShouldThrowPluginActivationExceptionWithoutCause()
    {
        (AssemblyBuilder assembly, Type entryPoint) = DefineDynamicPluginAssembly(
            "Bodu.Globalization.Calendar.Plugins.Test.Dynamic.NotAPlugin",
            DefinePlainEntryPoint);

        PluginActivationException ex = Assert.ThrowsExactly<PluginActivationException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(assembly, new AllowAllPluginTrustPolicy());
        });

        Assert.AreSame(entryPoint, ex.PluginType);
        Assert.AreEqual(
            string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginType, "Dynamic.NotAPlugin"),
            ex.Message);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that an assembly whose plugin attribute names an open generic type definition, which
    /// <see cref="Activator.CreateInstance(Type)" /> rejects with <see cref="ArgumentException" />, is reported as
    /// <see cref="PluginActivationException" /> naming the type, carrying it as
    /// <see cref="PluginActivationException.PluginType" /> and the rejection as the inner exception.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenPluginTypeIsAnOpenGenericDefinition_ShouldThrowPluginActivationExceptionWithTheCause()
    {
        (AssemblyBuilder assembly, Type entryPoint) = DefineDynamicPluginAssembly(
            "Bodu.Globalization.Calendar.Plugins.Test.Dynamic.OpenGeneric",
            _ => typeof(List<>));
        Assert.IsTrue(entryPoint.ContainsGenericParameters, "The attribute must name the open definition.");

        PluginActivationException ex = Assert.ThrowsExactly<PluginActivationException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(assembly, new AllowAllPluginTrustPolicy());
        });

        Assert.AreSame(entryPoint, ex.PluginType);
        Assert.AreEqual(
            string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginActivation, entryPoint.FullName),
            ex.Message);
        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual(typeof(ArgumentException), ex.InnerException.GetType());
    }

    /// <summary>
    /// Verifies that an assembly whose plugin attribute names <see langword="void" />, which
    /// <see cref="Activator.CreateInstance(Type)" /> rejects with <see cref="NotSupportedException" />, is reported
    /// as <see cref="PluginActivationException" /> naming the type, carrying it as
    /// <see cref="PluginActivationException.PluginType" /> and the rejection as the inner exception.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenPluginTypeIsVoid_ShouldThrowPluginActivationExceptionWithTheCause()
    {
        (AssemblyBuilder assembly, Type entryPoint) = DefineDynamicPluginAssembly(
            "Bodu.Globalization.Calendar.Plugins.Test.Dynamic.Void",
            _ => typeof(void));

        PluginActivationException ex = Assert.ThrowsExactly<PluginActivationException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(assembly, new AllowAllPluginTrustPolicy());
        });

        Assert.AreSame(entryPoint, ex.PluginType);
        Assert.AreEqual(
            string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginActivation, "System.Void"),
            ex.Message);
        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual(typeof(NotSupportedException), ex.InnerException.GetType());
    }

    /// <summary>
    /// Verifies that an assembly whose plugin attribute names a byref-like struct, which
    /// <see cref="Activator.CreateInstance(Type)" /> rejects with <see cref="NotSupportedException" /> because its
    /// instances cannot be boxed, is reported as <see cref="PluginActivationException" /> naming the type, carrying
    /// it as <see cref="PluginActivationException.PluginType" /> and the rejection as the inner exception.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenPluginTypeIsByRefLike_ShouldThrowPluginActivationExceptionWithTheCause()
    {
        (AssemblyBuilder assembly, Type entryPoint) = DefineDynamicPluginAssembly(
            "Bodu.Globalization.Calendar.Plugins.Test.Dynamic.ByRefLike",
            _ => typeof(Span<int>));
        Assert.IsTrue(entryPoint.IsByRefLike, "The attribute must name a byref-like struct.");

        PluginActivationException ex = Assert.ThrowsExactly<PluginActivationException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(assembly, new AllowAllPluginTrustPolicy());
        });

        Assert.AreSame(entryPoint, ex.PluginType);
        Assert.AreEqual(
            string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginActivation, entryPoint.FullName),
            ex.Message);
        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual(typeof(NotSupportedException), ex.InnerException.GetType());
    }

    /// <summary>
    /// Verifies that an assembly whose <see cref="AssemblyName" /> carries no simple name is identified to the trust
    /// policy by its full name instead.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenAssemblyNameHasNoName_ShouldIdentifyTheAssemblyByFullName()
    {
        AssemblyName nameless = new();
        Assert.IsNull(nameless.Name, "A default AssemblyName carries no simple name.");
        StubAssembly assembly = new(nameless, "Stub, Version=1.0.0.0", string.Empty, typeof(TestPlugin));
        PluginTrustContext? captured = null;
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(context =>
        {
            captured = context;
            return PluginTrustResult.Trusted();
        });

        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(assembly, policy);

        Assert.IsNotNull(captured);
        Assert.AreEqual("Stub, Version=1.0.0.0", captured!.AssemblyName);
        Assert.IsInstanceOfType<TestPlugin>(plugin);
    }

    /// <summary>
    /// Verifies that an assembly with neither a simple name nor a full name is identified to the trust policy, and
    /// in a rejection, by the <c>&lt;unknown&gt;</c> placeholder.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenAssemblyHasNeitherNameNorFullName_ShouldIdentifyTheAssemblyAsUnknown()
    {
        StubAssembly assembly = new(new AssemblyName(), null, string.Empty, typeof(TestPlugin));
        PluginTrustContext? captured = null;
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(context =>
        {
            captured = context;
            return PluginTrustResult.Rejected("blocked");
        });

        PluginNotTrustedException ex = Assert.ThrowsExactly<PluginNotTrustedException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(assembly, policy);
        });

        Assert.IsNotNull(captured);
        Assert.AreEqual("<unknown>", captured!.AssemblyName);
        Assert.AreEqual("<unknown>", ex.AssemblyName);
    }

    /// <summary>
    /// Verifies that an assembly whose location names a file that does not exist reaches the trust policy with that
    /// path but no file hash, since there is nothing to hash.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenLocationNamesAMissingFile_ShouldPassThePathWithoutAHash()
    {
        string location = Path.Combine(AppContext.BaseDirectory, "PluginFixtures", "Missing", "Missing.dll");
        Assert.IsFalse(File.Exists(location), "The location must name a file that does not exist.");
        StubAssembly assembly = new(new AssemblyName("Missing"), "Missing", location, typeof(TestPlugin));
        PluginTrustContext? captured = null;
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(context =>
        {
            captured = context;
            return PluginTrustResult.Trusted();
        });

        _ = NotableDatePluginLoader.LoadFrom(assembly, policy);

        Assert.IsNotNull(captured);
        Assert.AreEqual(
            ("Missing", location, (byte[]?)null),
            (captured!.AssemblyName, captured.AssemblyPath, captured.FileHash));
    }

    /// <summary>
    /// Verifies that an assembly whose name carries no public-key token at all reaches the trust policy with a
    /// <see langword="null" /> token.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenPublicKeyTokenIsAbsent_ShouldPassNullToken()
    {
        AssemblyName name = new("Unsigned");
        Assert.IsNull(name.GetPublicKeyToken(), "A name given no key reports no token.");
        StubAssembly assembly = new(name, "Unsigned", string.Empty, typeof(TestPlugin));
        PluginTrustContext? captured = null;
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(context =>
        {
            captured = context;
            return PluginTrustResult.Trusted();
        });

        _ = NotableDatePluginLoader.LoadFrom(assembly, policy);

        Assert.IsNotNull(captured);
        Assert.AreEqual(("Unsigned", (string?)null), (captured!.AssemblyName, captured.PublicKeyToken));
    }

    /// <summary>
    /// Verifies that an assembly whose name carries an empty public-key token, the form an unsigned loaded assembly
    /// reports, reaches the trust policy with a <see langword="null" /> token rather than an empty string.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenPublicKeyTokenIsEmpty_ShouldPassNullToken()
    {
        AssemblyName name = new("Unsigned");
        name.SetPublicKeyToken([]);
        Assert.AreEqual(0, name.GetPublicKeyToken()!.Length, "The name must report an empty token.");
        StubAssembly assembly = new(name, "Unsigned", string.Empty, typeof(TestPlugin));
        PluginTrustContext? captured = null;
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(context =>
        {
            captured = context;
            return PluginTrustResult.Trusted();
        });

        _ = NotableDatePluginLoader.LoadFrom(assembly, policy);

        Assert.IsNotNull(captured);
        Assert.AreEqual(("Unsigned", (string?)null), (captured!.AssemblyName, captured.PublicKeyToken));
    }

    /// <summary>
    /// Verifies that a plugin type with no <see cref="Type.FullName" /> is activated and logged by its simple name.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenPluginTypeHasNoFullName_ShouldLogActivationBySimpleName()
    {
        NamelessType pluginType = new(typeof(TestPlugin));
        Assert.IsNull(pluginType.FullName);
        StubAssembly assembly = new(new AssemblyName("Stub"), "Stub", string.Empty, pluginType);
        CapturingLogger logger = new();

        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(assembly, new AllowAllPluginTrustPolicy(), logger);

        Assert.IsInstanceOfType<TestPlugin>(plugin);
        Assert.Contains(
            e => e.Level == LogLevel.Information && e.EventId.Id == 2003
                && e.Message == "Activated notable-date plugin 'TestPlugin' from assembly 'Stub'",
            logger.Entries);
    }

    /// <summary>
    /// Verifies that a plugin type with no <see cref="Type.FullName" /> whose constructor throws is reported as
    /// <see cref="PluginActivationException" /> naming the type by its simple name, carrying that type and the
    /// constructor's failure.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenPluginTypeHasNoFullNameAndConstructorThrows_ShouldNameTheTypeBySimpleName()
    {
        NamelessType pluginType = new(typeof(FaultingPlugin));
        StubAssembly assembly = new(new AssemblyName("Stub"), "Stub", string.Empty, pluginType);

        PluginActivationException ex = Assert.ThrowsExactly<PluginActivationException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(assembly, new AllowAllPluginTrustPolicy());
        });

        Assert.AreSame(pluginType, ex.PluginType);
        Assert.AreEqual(
            string.Format(
                CultureInfo.CurrentCulture,
                PluginsResourceStrings.Op_Invalid_PluginActivation,
                nameof(FaultingPlugin)),
            ex.Message);
        Assert.IsInstanceOfType<TargetInvocationException>(ex.InnerException);
        Assert.IsInstanceOfType<InvalidOperationException>(ex.InnerException!.InnerException);
    }

    /// <summary>
    /// Verifies that a plugin type with no <see cref="Type.FullName" /> that activates but is not a plugin is
    /// rejected with <see cref="PluginActivationException" /> naming the type by its simple name, with no inner
    /// exception.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenPluginTypeHasNoFullNameAndIsNotAPlugin_ShouldNameTheTypeBySimpleName()
    {
        NamelessType pluginType = new(typeof(NotAPlugin));
        StubAssembly assembly = new(new AssemblyName("Stub"), "Stub", string.Empty, pluginType);

        PluginActivationException ex = Assert.ThrowsExactly<PluginActivationException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(assembly, new AllowAllPluginTrustPolicy());
        });

        Assert.AreSame(pluginType, ex.PluginType);
        Assert.AreEqual(
            string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginType, nameof(NotAPlugin)),
            ex.Message);
        Assert.IsNull(ex.InnerException);
    }
}
