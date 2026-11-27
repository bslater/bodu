// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextResolver.ContextSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;

namespace Bodu.Financial;

internal sealed partial class MonetaryContextResolver
{
    /// <summary>
    /// Holds one generation of the contexts the resolver serves: the default context and the named ones.
    /// </summary>
    private sealed class ContextSet
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextSet" /> class.
        /// </summary>
        /// <param name="defaultContext">The default context.</param>
        /// <param name="named">The named contexts, keyed case-insensitively.</param>
        public ContextSet(MonetaryContext defaultContext, FrozenDictionary<string, MonetaryContext> named)
        {
            Default = defaultContext;
            Named = named;
        }

        /// <summary>
        /// Gets the default context.
        /// </summary>
        /// <value>The context that <see cref="IMonetaryContextResolver.Default" /> returns.</value>
        public MonetaryContext Default { get; }

        /// <summary>
        /// Gets the named contexts the options configure.
        /// </summary>
        /// <value>The contexts, keyed case-insensitively by name.</value>
        public FrozenDictionary<string, MonetaryContext> Named { get; }
    }
}
