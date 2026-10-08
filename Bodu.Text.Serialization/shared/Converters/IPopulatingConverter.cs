// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IPopulatingConverter.cs" company="Bodu Pty. Ltd.">
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
/// Implemented by a converter that can read a keyed container into an <see cref="ObjectPopulation" />, so that the
/// members it reads can be set on an instance that already exists.
/// </summary>
/// <remarks>
/// The object converter implements it; <see cref="ObjectBinder.PopulatesObject" /> decides when a member is read this
/// way under <see cref="ObjectCreationHandling.Populate" />.
/// </remarks>
internal interface IPopulatingConverter
{
    /// <summary>
    /// Reads the keyed container at the reader's position into a buffer, without constructing an instance.
    /// </summary>
    /// <param name="reader">The reader, positioned on the container's start token.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The members read, ready to be bound to an instance.</returns>
    ObjectPopulation ReadPopulation(ref FormatReader reader, FormatOptions options);
}
