// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoaderTests.InMemoryAssemblies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Builds the assembly shapes the loader's
/// <see cref="NotableDatePluginLoader.LoadFrom(Assembly, IPluginTrustPolicy, Microsoft.Extensions.Logging.ILogger)" />
/// overload meets beyond the on-disk, strong-named fixtures: an image loaded from bytes, a dynamic assembly emitted
/// in the test, and an <see cref="Assembly" /> whose identity is constructed outright. None of them writes a file.
/// </summary>
public sealed partial class NotableDatePluginLoaderTests
{
    /// <summary>
    /// Loads the valid fixture plugin from its bytes rather than its path, so the resulting assembly has no
    /// <see cref="Assembly.Location" />.
    /// </summary>
    /// <returns>The fixture plugin assembly loaded from an in-memory image.</returns>
    private static Assembly LoadFixturePluginFromBytes() =>
        Assembly.Load(File.ReadAllBytes(FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin")));

    /// <summary>
    /// Emits a run-only dynamic assembly declaring, through <see cref="NotableDatePluginAttribute" />, an entry-point
    /// type defined in that same assembly.
    /// </summary>
    /// <param name="name">The simple name of the dynamic assembly.</param>
    /// <param name="defineEntryPoint">Defines and creates the entry-point type in the assembly's module.</param>
    /// <returns>The dynamic assembly and the entry-point type it declares.</returns>
    /// <remarks>
    /// The attribute is written as a raw ECMA-335 II.23.3 blob naming the entry point by its unqualified full name.
    /// <see cref="CustomAttributeBuilder" /> would write the assembly-qualified name instead, and a dynamic
    /// assembly cannot be bound by name - neither through probing nor through
    /// <see cref="System.Runtime.Loader.AssemblyLoadContext.Resolving" /> - so reading that attribute back fails
    /// with <see cref="FileNotFoundException" />. An unqualified name is resolved by the custom-attribute search
    /// rules in the declaring assembly first, which is where the type lives.
    /// </remarks>
    private static (AssemblyBuilder Assembly, Type EntryPoint) DefineDynamicPluginAssembly(
        string name,
        Func<ModuleBuilder, Type> defineEntryPoint)
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule(name);
        Type entryPoint = defineEntryPoint(module);

        ConstructorInfo constructor = typeof(NotableDatePluginAttribute).GetConstructor([typeof(Type)])!;
        assembly.SetCustomAttribute(constructor, EncodeTypeArgument(entryPoint.FullName!));

        return (assembly, entryPoint);
    }

    /// <summary>
    /// Encodes the custom-attribute blob of a single <see cref="Type" /> argument, with no named arguments.
    /// </summary>
    /// <param name="typeName">The type name to serialize, without an assembly qualification.</param>
    /// <returns>The blob: the prolog, the name as a compressed-length UTF-8 string, and a zero named-argument count.</returns>
    private static byte[] EncodeTypeArgument(string typeName)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(typeName);
        Assert.IsTrue(utf8.Length < 0x80, "The single-byte compressed length form covers every type name emitted here.");

        var blob = new byte[2 + 1 + utf8.Length + 2];
        blob[0] = 0x01;
        blob[1] = 0x00;
        blob[2] = (byte)utf8.Length;
        utf8.CopyTo(blob, 3);

        return blob;
    }

    /// <summary>
    /// Defines a public class whose parameterless constructor throws <see cref="InvalidOperationException" />.
    /// </summary>
    /// <param name="module">The module to define the type in.</param>
    /// <returns>The created type.</returns>
    private static Type DefineFaultingEntryPoint(ModuleBuilder module)
    {
        TypeBuilder type = module.DefineType("Dynamic.FaultingPlugin", TypeAttributes.Public | TypeAttributes.Class);
        ConstructorBuilder constructor = type.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            Type.EmptyTypes);

        ILGenerator il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldstr, "The dynamic plugin's constructor faults.");
        il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor([typeof(string)])!);
        il.Emit(OpCodes.Throw);

        return type.CreateType();
    }

    /// <summary>
    /// Defines a public class with a parameterless constructor that implements no plugin contract.
    /// </summary>
    /// <param name="module">The module to define the type in.</param>
    /// <returns>The created type.</returns>
    private static Type DefinePlainEntryPoint(ModuleBuilder module)
    {
        TypeBuilder type = module.DefineType("Dynamic.NotAPlugin", TypeAttributes.Public | TypeAttributes.Class);
        _ = type.DefineDefaultConstructor(MethodAttributes.Public);

        return type.CreateType();
    }

    /// <summary>
    /// Defines a public class implementing <see cref="INotableDatePlugin" /> with the name <c>Dynamic Plugin</c> and
    /// the version <c>2.0.1</c>.
    /// </summary>
    /// <param name="module">The module to define the type in.</param>
    /// <returns>The created type.</returns>
    private static Type DefinePluginEntryPoint(ModuleBuilder module)
    {
        const MethodAttributes getter = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig
            | MethodAttributes.NewSlot | MethodAttributes.Final | MethodAttributes.SpecialName;

        TypeBuilder type = module.DefineType(
            "Dynamic.DynamicPlugin",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(object),
            [typeof(INotableDatePlugin)]);
        _ = type.DefineDefaultConstructor(MethodAttributes.Public);

        MethodBuilder getName = type.DefineMethod("get_Name", getter, typeof(string), Type.EmptyTypes);
        ILGenerator il = getName.GetILGenerator();
        il.Emit(OpCodes.Ldstr, "Dynamic Plugin");
        il.Emit(OpCodes.Ret);
        type.DefineProperty("Name", PropertyAttributes.None, typeof(string), null).SetGetMethod(getName);

        MethodBuilder getVersion = type.DefineMethod("get_Version", getter, typeof(Version), Type.EmptyTypes);
        il = getVersion.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4_2);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Newobj, typeof(Version).GetConstructor([typeof(int), typeof(int), typeof(int)])!);
        il.Emit(OpCodes.Ret);
        type.DefineProperty("Version", PropertyAttributes.None, typeof(Version), null).SetGetMethod(getVersion);

        return type.CreateType();
    }

    /// <summary>
    /// An <see cref="Assembly" /> whose name, full name, location and plugin attribute are supplied outright, so a
    /// test can hand the loader an identity no loaded image produces: a nameless <see cref="AssemblyName" />, a
    /// location that names no file, an absent or empty public-key token, or an entry-point type that is not a
    /// runtime type.
    /// </summary>
    private sealed class StubAssembly
        : Assembly
    {
        /// <summary>The name returned by <see cref="GetName()" />.</summary>
        private readonly AssemblyName _name;

        /// <summary>The value of <see cref="FullName" />.</summary>
        private readonly string? _fullName;

        /// <summary>The value of <see cref="Location" />.</summary>
        private readonly string _location;

        /// <summary>The assembly's only custom attribute.</summary>
        private readonly NotableDatePluginAttribute _attribute;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubAssembly" /> class.
        /// </summary>
        /// <param name="name">The name returned by <see cref="GetName()" />.</param>
        /// <param name="fullName">The value of <see cref="FullName" />.</param>
        /// <param name="location">The value of <see cref="Location" />; empty for an assembly with no file.</param>
        /// <param name="pluginType">The entry-point type the assembly declares.</param>
        public StubAssembly(AssemblyName name, string? fullName, string location, Type pluginType)
        {
            _name = name;
            _fullName = fullName;
            _location = location;
            _attribute = new NotableDatePluginAttribute(pluginType);
        }

        /// <inheritdoc />
        public override string? FullName =>
            _fullName;

        /// <inheritdoc />
        public override string Location =>
            _location;

        /// <inheritdoc />
        public override AssemblyName GetName() =>
            _name;

        /// <inheritdoc />
        public override AssemblyName GetName(bool copiedName) =>
            _name;

        /// <inheritdoc />
        public override object[] GetCustomAttributes(bool inherit) =>
            new Attribute[] { _attribute };

        /// <inheritdoc />
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            attributeType.IsInstanceOfType(_attribute) ? new Attribute[] { _attribute } : Array.Empty<Attribute>();

        /// <inheritdoc />
        public override bool IsDefined(Type attributeType, bool inherit) =>
            attributeType.IsInstanceOfType(_attribute);
    }

    /// <summary>
    /// A <see cref="Type" /> that activates as the runtime type it wraps but reports no <see cref="Type.FullName" />,
    /// the shape a generic type parameter has, which no custom attribute can carry.
    /// </summary>
    private sealed class NamelessType
        : TypeDelegator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NamelessType" /> class.
        /// </summary>
        /// <param name="underlyingType">The runtime type that is activated in this type's place.</param>
        public NamelessType(Type underlyingType)
            : base(underlyingType)
        {
        }

        /// <inheritdoc />
        public override string? FullName =>
            null;
    }

    /// <summary>
    /// An entry point whose constructor throws <see cref="InvalidOperationException" />.
    /// </summary>
    private sealed class FaultingPlugin
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FaultingPlugin" /> class, which always fails.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always.</exception>
        public FaultingPlugin() =>
            throw new InvalidOperationException("The plugin's constructor faults.");
    }

    /// <summary>
    /// An entry point that activates but implements no plugin contract.
    /// </summary>
    private sealed class NotAPlugin
    {
    }
}
