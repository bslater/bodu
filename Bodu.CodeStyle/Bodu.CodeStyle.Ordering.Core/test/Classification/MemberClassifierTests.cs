// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberClassifierTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.CodeStyle.Ordering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.Ordering.Test.Classification;

[TestClass]
public sealed class MemberClassifierTests
{
    /// <summary>
    /// Verifies that the constructor throws when the options instance is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new MemberClassifier(null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MemberClassifier.Classify" /> throws when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        var classifier = new MemberClassifier(DefaultBoduOrderingPolicy.Create());

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = classifier.Classify(null!);
        });

        Assert.AreEqual("input", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <c>const</c> field is classified as <see cref="MemberKind.Constant" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsConstField_ShouldReportConstantKind()
    {
        ClassifiedMember member = ClassifyField(isStatic: false, isReadOnly: false, isConst: true);

        Assert.AreEqual(MemberKind.Constant, member.Kind);
    }

    /// <summary>
    /// Verifies that a <c>static readonly</c> field is classified as
    /// <see cref="MemberKind.StaticReadonlyField" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsStaticReadonlyField_ShouldReportStaticReadonlyFieldKind()
    {
        ClassifiedMember member = ClassifyField(isStatic: true, isReadOnly: true, isConst: false);

        Assert.AreEqual(MemberKind.StaticReadonlyField, member.Kind);
    }

    /// <summary>
    /// Verifies that a <c>static</c> (non-readonly) field is classified as
    /// <see cref="MemberKind.StaticField" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsStaticField_ShouldReportStaticFieldKind()
    {
        ClassifiedMember member = ClassifyField(isStatic: true, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.StaticField, member.Kind);
    }

    /// <summary>
    /// Verifies that an instance field is classified as <see cref="MemberKind.InstanceField" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsInstanceField_ShouldReportInstanceFieldKind()
    {
        ClassifiedMember member = ClassifyField(isStatic: false, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.InstanceField, member.Kind);
    }

    /// <summary>
    /// Verifies that a static constructor is classified as <see cref="MemberKind.StaticConstructor" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsStaticConstructor_ShouldReportStaticConstructorKind()
    {
        ClassifiedMember member = Classify("ConstructorDeclaration", AccessibilityRank.Private, isStatic: true, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.StaticConstructor, member.Kind);
    }

    /// <summary>
    /// Verifies that an instance constructor is classified as <see cref="MemberKind.Constructor" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsConstructor_ShouldReportConstructorKind()
    {
        ClassifiedMember member = Classify("ConstructorDeclaration", AccessibilityRank.Public, isStatic: false, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.Constructor, member.Kind);
    }

    /// <summary>
    /// Verifies that an event declaration is classified as <see cref="MemberKind.Event" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsEvent_ShouldReportEventKind()
    {
        ClassifiedMember member = Classify("EventDeclaration", AccessibilityRank.Public, isStatic: false, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.Event, member.Kind);
    }

    /// <summary>
    /// Verifies that a property declaration is classified as <see cref="MemberKind.Property" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsProperty_ShouldReportPropertyKind()
    {
        ClassifiedMember member = Classify("PropertyDeclaration", AccessibilityRank.Public, isStatic: false, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.Property, member.Kind);
    }

    /// <summary>
    /// Verifies that an indexer declaration is classified as <see cref="MemberKind.Indexer" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsIndexer_ShouldReportIndexerKind()
    {
        ClassifiedMember member = Classify("IndexerDeclaration", AccessibilityRank.Public, isStatic: false, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.Indexer, member.Kind);
    }

    /// <summary>
    /// Verifies that an operator declaration is classified as <see cref="MemberKind.Operator" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsOperator_ShouldReportOperatorKind()
    {
        ClassifiedMember member = Classify("OperatorDeclaration", AccessibilityRank.Public, isStatic: true, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.Operator, member.Kind);
    }

    /// <summary>
    /// Verifies that a conversion operator declaration is classified as
    /// <see cref="MemberKind.ConversionOperator" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsConversionOperator_ShouldReportConversionOperatorKind()
    {
        ClassifiedMember member = Classify("ConversionOperatorDeclaration", AccessibilityRank.Public, isStatic: true, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.ConversionOperator, member.Kind);
    }

    /// <summary>
    /// Verifies that a destructor declaration is classified as <see cref="MemberKind.Finalizer" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsDestructor_ShouldReportFinalizerKind()
    {
        ClassifiedMember member = Classify("DestructorDeclaration", AccessibilityRank.Private, isStatic: false, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.Finalizer, member.Kind);
    }

    /// <summary>
    /// Verifies that a delegate declaration is classified as <see cref="MemberKind.Delegate" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsDelegate_ShouldReportDelegateKind()
    {
        ClassifiedMember member = Classify("DelegateDeclaration", AccessibilityRank.Public, isStatic: false, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.Delegate, member.Kind);
    }

    /// <summary>
    /// Verifies that a nested class is classified as <see cref="MemberKind.NestedType" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenInputIsNestedClass_ShouldReportNestedTypeKind()
    {
        ClassifiedMember member = Classify("ClassDeclaration", AccessibilityRank.Public, isStatic: false, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.NestedType, member.Kind);
    }

    /// <summary>
    /// Verifies that an explicit interface property is classified as
    /// <see cref="MemberKind.ExplicitInterfaceMember" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenExplicitInterfaceProperty_ShouldReportExplicitInterfaceKind()
    {
        ClassifiedMember member = Classify("PropertyDeclaration", AccessibilityRank.NotApplicable, isStatic: false, isReadOnly: false, isConst: false, isExplicitInterface: true);

        Assert.AreEqual(MemberKind.ExplicitInterfaceMember, member.Kind);
    }

    /// <summary>
    /// Verifies that an unrecognised declaration kind falls back to <see cref="MemberKind.Method" />.
    /// </summary>
    [TestMethod]
    public void Classify_WhenDeclarationKindUnknown_ShouldFallBackToMethod()
    {
        ClassifiedMember member = Classify("XyzDeclaration", AccessibilityRank.Private, isStatic: false, isReadOnly: false, isConst: false);

        Assert.AreEqual(MemberKind.Method, member.Kind);
    }

    private static ClassifiedMember ClassifyField(bool isStatic, bool isReadOnly, bool isConst) =>
        Classify("FieldDeclaration", AccessibilityRank.Private, isStatic, isReadOnly, isConst);

    private static ClassifiedMember Classify(string declarationKind, AccessibilityRank accessibility, bool isStatic, bool isReadOnly, bool isConst, bool isExplicitInterface = false)
    {
        var classifier = new MemberClassifier(DefaultBoduOrderingPolicy.Create());
        return classifier.Classify(new FakeMember(declarationKind, accessibility, isStatic, isReadOnly, isConst, isExplicitInterface));
    }

    private sealed class FakeMember : IMemberInput
    {
        public FakeMember(string declarationKind, AccessibilityRank accessibility, bool isStatic, bool isReadOnly, bool isConst, bool isExplicitInterface)
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
