// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectPopulation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#elif YAML
namespace Bodu.Text.Yaml.Serialization.Converters;
#endif

/// <summary>
/// Holds the members an object converter has read for one keyed container without constructing an instance, so that,
/// under <see cref="ObjectCreationHandling.Populate" />, they can be bound to the instance a member already holds once
/// the member's owner exists, or to a new instance when the member holds none.
/// </summary>
/// <remarks>
/// An object converter builds its instance only after reading every member, because a parameterized constructor needs
/// the read values; the instance a member holds is therefore not reachable while the member is read. The converter
/// reads such a member into this buffer instead, and <see cref="ObjectBinder" /> binds it once the owner is built.
/// </remarks>
internal abstract class ObjectPopulation
{
    /// <summary>
    /// Constructs a new instance and binds the read members to it, as reading the container normally would.
    /// </summary>
    /// <returns>The new instance, boxed when its type is a value type.</returns>
    internal abstract object Create();

    /// <summary>
    /// Binds the read members to an existing instance: its deserialization callbacks run, each read member is assigned
    /// or populated in turn, and its extension data receives the unmatched entries.
    /// </summary>
    /// <param name="instance">The instance to populate, boxed when its type is a value type.</param>
    internal abstract void Populate(object instance);
}
