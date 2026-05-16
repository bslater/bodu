// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultBoduOrderingPolicyTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.Ordering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.Ordering.Test.Model;

[TestClass]
public sealed class DefaultBoduOrderingPolicyTests
{
    /// <summary>
    /// Verifies that the default policy ranks constants strictly before instance fields.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Create_ShouldOrderConstantsBeforeInstanceFields()
    {
        MemberOrderOptions options = DefaultBoduOrderingPolicy.Create();
        var classifier = new MemberClassifier(options);

        ClassifiedMember constant = classifier.Classify(new FakeMember("FieldDeclaration", AccessibilityRank.Private, isStatic: false, isReadOnly: false, isConst: true));
        ClassifiedMember instance = classifier.Classify(new FakeMember("FieldDeclaration", AccessibilityRank.Private, isStatic: false, isReadOnly: false, isConst: false));

        Assert.IsTrue(constant.GroupRank < instance.GroupRank);
    }

    /// <summary>
    /// Verifies that the default policy ranks public members strictly before private members.
    /// </summary>
    [TestMethod]
    public void Create_ShouldOrderPublicBeforePrivate()
    {
        MemberOrderOptions options = DefaultBoduOrderingPolicy.Create();
        var classifier = new MemberClassifier(options);

        ClassifiedMember pub = classifier.Classify(new FakeMember("MethodDeclaration", AccessibilityRank.Public, isStatic: false, isReadOnly: false, isConst: false));
        ClassifiedMember priv = classifier.Classify(new FakeMember("MethodDeclaration", AccessibilityRank.Private, isStatic: false, isReadOnly: false, isConst: false));

        Assert.IsTrue(pub.AccessibilityRank < priv.AccessibilityRank);
    }

    /// <summary>
    /// Verifies that partial-type boundaries are preserved by default.
    /// </summary>
    [TestMethod]
    public void Create_ShouldPreservePartialTypeBoundariesByDefault()
    {
        MemberOrderOptions options = DefaultBoduOrderingPolicy.Create();
        Assert.IsTrue(options.PreservePartialTypeOrder);
        Assert.IsFalse(options.ReorderAcrossPreprocessorDirectives);
    }

    private sealed class FakeMember : IMemberInput
    {
        public FakeMember(string declarationKind, AccessibilityRank accessibility, bool isStatic, bool isReadOnly, bool isConst, bool isExplicitInterface = false)
        {
            this.DeclarationKind = declarationKind;
            this.Accessibility = accessibility;
            this.IsStatic = isStatic;
            this.IsReadOnly = isReadOnly;
            this.IsConst = isConst;
            this.IsExplicitInterface = isExplicitInterface;
        }

        public string DeclarationKind { get; }

        public AccessibilityRank Accessibility { get; }

        public bool IsStatic { get; }

        public bool IsReadOnly { get; }

        public bool IsConst { get; }

        public bool IsExplicitInterface { get; }
    }
}
