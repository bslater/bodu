// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberKind.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.Ordering;

/// <summary>
/// Identifies the broad kind of a type member for ordering purposes.
/// </summary>
public enum MemberKind
{
    /// <summary>A field declared <c>const</c>.</summary>
    Constant = 0,

    /// <summary>A field declared <c>static readonly</c>.</summary>
    StaticReadonlyField = 1,

    /// <summary>A field declared <c>static</c> (non-readonly, non-const).</summary>
    StaticField = 2,

    /// <summary>An instance field.</summary>
    InstanceField = 3,

    /// <summary>An instance constructor.</summary>
    Constructor = 4,

    /// <summary>A static constructor.</summary>
    StaticConstructor = 5,

    /// <summary>A finalizer.</summary>
    Finalizer = 6,

    /// <summary>A delegate type declaration nested inside the type under analysis.</summary>
    Delegate = 7,

    /// <summary>An event member.</summary>
    Event = 8,

    /// <summary>A property member.</summary>
    Property = 9,

    /// <summary>An indexer member.</summary>
    Indexer = 10,

    /// <summary>A method member.</summary>
    Method = 11,

    /// <summary>An operator overload.</summary>
    Operator = 12,

    /// <summary>An implicit or explicit conversion operator.</summary>
    ConversionOperator = 13,

    /// <summary>An explicit interface member implementation.</summary>
    ExplicitInterfaceMember = 14,

    /// <summary>A nested type declaration.</summary>
    NestedType = 15,
}
