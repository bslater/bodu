// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundContainerLayout.PlannedContainer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound.Internal;

internal static partial class CompoundContainerLayout
{
    /// <summary>
    /// A container whose layout has been planned, so that every error its tree can raise has been raised, and whose
    /// bytes are yet to be written.
    /// </summary>
    /// <remarks>
    /// A commit plans the container before it truncates its destination, so a tree that cannot be written leaves the
    /// destination as it was. Writing can still fail part way, when a deferred source is shorter than it declared.
    /// </remarks>
    internal sealed class PlannedContainer
    {
        /// <summary>The emission plan.</summary>
        private readonly EmissionPlan _plan;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlannedContainer" /> class.
        /// </summary>
        /// <param name="plan">The emission plan.</param>
        private PlannedContainer(EmissionPlan plan) =>
            _plan = plan;

        /// <summary>
        /// Writes the container to a destination stream, one sector at a time.
        /// </summary>
        /// <param name="destination">The stream that receives the compound-file content.</param>
        /// <exception cref="CompoundFileSerializationException">
        /// Thrown when a deferred source is shorter than its declared length.
        /// </exception>
        internal void WriteTo(Stream destination) =>
            Emit(destination, _plan);

        /// <summary>
        /// Asynchronously writes the container to a destination stream, one sector at a time.
        /// </summary>
        /// <param name="destination">The stream that receives the compound-file content.</param>
        /// <param name="cancellationToken">A token that cancels the write.</param>
        /// <returns>A task that completes when the container has been written.</returns>
        /// <exception cref="CompoundFileSerializationException">
        /// Thrown when a deferred source is shorter than its declared length.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when <paramref name="cancellationToken" /> is canceled.
        /// </exception>
        internal Task WriteToAsync(Stream destination, CancellationToken cancellationToken) =>
            EmitAsync(destination, _plan, cancellationToken);

        /// <summary>
        /// Plans the container of a tree.
        /// </summary>
        /// <param name="root">The root storage to serialize.</param>
        /// <param name="options">The options controlling the output layout.</param>
        /// <returns>The planned container.</returns>
        /// <exception cref="CompoundFileSerializationException">
        /// Thrown when the model cannot be represented.
        /// </exception>
        internal static PlannedContainer Create(CompoundStorageBuilder root, CompoundBuildOptions options) =>
            new(PreparePlan(root, options));
    }
}
