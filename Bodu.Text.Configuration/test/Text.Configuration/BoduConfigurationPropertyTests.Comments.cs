// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationPropertyTests.Comments.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationPropertyTests
{
    /// <summary>
    /// Verifies that a freshly constructed property has no leading or inline comments.
    /// </summary>
    [TestMethod]
    public void Comments_WhenNewlyCreated_ShouldBeEmpty()
    {
        BoduConfigurationProperty property = new("key", "value");

        Assert.AreEqual(0, property.LeadingComments.Count);
        Assert.IsNull(property.InlineComment);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationProperty.AddLeadingComment(BoduConfigurationComment)" />
    /// appends in source order.
    /// </summary>
    [TestMethod]
    public void AddLeadingComment_WhenCalledMultipleTimes_ShouldPreserveOrder()
    {
        BoduConfigurationProperty property = new("key", "value");
        property.AddLeadingComment(new BoduConfigurationComment('#', "first", default));
        property.AddLeadingComment(new BoduConfigurationComment(';', "second", default));

        Assert.AreEqual(2, property.LeadingComments.Count);
        Assert.AreEqual("first", property.LeadingComments[0].Text);
        Assert.AreEqual("second", property.LeadingComments[1].Text);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationProperty.SetLeadingComments(System.Collections.Generic.IEnumerable{BoduConfigurationComment})" />
    /// replaces the existing leading comments.
    /// </summary>
    [TestMethod]
    public void SetLeadingComments_WhenSequenceProvided_ShouldReplaceExistingComments()
    {
        BoduConfigurationProperty property = new("key", "value");
        property.AddLeadingComment(new BoduConfigurationComment('#', "old", default));

        property.SetLeadingComments([new BoduConfigurationComment('#', "new", default)]);

        Assert.AreEqual(1, property.LeadingComments.Count);
        Assert.AreEqual("new", property.LeadingComments[0].Text);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationProperty.SetLeadingComments(System.Collections.Generic.IEnumerable{BoduConfigurationComment})" />
    /// rejects a <see langword="null" /> sequence.
    /// </summary>
    [TestMethod]
    public void SetLeadingComments_WhenSequenceIsNull_ShouldThrowExactly()
    {
        BoduConfigurationProperty property = new("key", "value");

        Assert.ThrowsExactly<ArgumentNullException>(() => property.SetLeadingComments(null!));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationProperty.InlineComment" /> retains an assigned value.
    /// </summary>
    [TestMethod]
    public void InlineComment_WhenAssigned_ShouldRetainValue()
    {
        BoduConfigurationProperty property = new("key", "value");
        BoduConfigurationComment comment = new('#', " note", default);

        property.InlineComment = comment;

        Assert.AreEqual(comment, property.InlineComment);
    }
}
