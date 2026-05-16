// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationProperty.Comments.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationProperty
{
    private readonly List<BoduConfigurationComment> _leadingComments = [];

    /// <summary>
    /// Gets the comments that precede this property on their own lines, in source order.
    /// </summary>
    /// <returns>A read-only view of the leading comments.</returns>
    public IReadOnlyList<BoduConfigurationComment> LeadingComments => this._leadingComments;

    /// <summary>
    /// Gets or sets the inline comment captured on the same line as this property, or
    /// <see langword="null" /> when no inline comment was present.
    /// </summary>
    /// <returns>The inline comment, or <see langword="null" />.</returns>
    public BoduConfigurationComment? InlineComment { get; set; }

    /// <summary>
    /// Appends a leading comment to this property.
    /// </summary>
    /// <param name="comment">The comment to append.</param>
    public void AddLeadingComment(BoduConfigurationComment comment) =>
        this._leadingComments.Add(comment);

    /// <summary>
    /// Replaces the leading comments of this property with the supplied sequence.
    /// </summary>
    /// <param name="comments">The new sequence of leading comments.</param>
    /// <exception cref="ArgumentNullException"><paramref name="comments" /> is <see langword="null" />.</exception>
    public void SetLeadingComments(IEnumerable<BoduConfigurationComment> comments)
    {
        ThrowHelper.ThrowIfNull(comments);
        this._leadingComments.Clear();
        this._leadingComments.AddRange(comments);
    }
}
