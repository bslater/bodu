// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AttributeBoundJsonContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Serialization;

/// <summary>
/// Verifies the frozen serialization contract of the type-level <c>[JsonConverter]</c> attributes: serializing without
/// any registered options always produces the canonical <see cref="FinancialJsonPolicy.Strict" /> object shape, never
/// the compact or lenient variants.
/// </summary>
[TestClass]
public partial class AttributeBoundJsonContractTests
{
}
