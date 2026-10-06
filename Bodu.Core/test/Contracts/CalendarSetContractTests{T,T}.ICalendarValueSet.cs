// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.ICalendarValueSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that the type implements every member of <see cref="ICalendarValueSet{TSelf, TValue}" /> with a public
    /// member of its own, so that a caller reaches each one on the type without going through the interface.
    /// </summary>
    [TestMethod]
    public void ICalendarValueSet_WhenMapped_ShouldImplementEveryMemberPublicly()
    {
        InterfaceMapping map = typeof(TSet).GetInterfaceMap(typeof(ICalendarValueSet<TSet, TElement>));
        string[] expected =
        [
            "get_Empty", "get_All", "get_Count", "FromUInt64", "ToUInt64", "Contains", "With", "Without", "ParseExact",
            "TryParseExact", "ToString",
        ];

        CollectionAssert.AreEquivalent(expected, map.InterfaceMethods.Select(method => method.Name).ToArray());
        for (int i = 0; i < map.TargetMethods.Length; i++)
        {
            MethodInfo target = map.TargetMethods[i];

            Assert.IsTrue(target.IsPublic, $"{map.InterfaceMethods[i].Name} is implemented by {target.Name}, which is not public.");
            Assert.AreEqual(typeof(TSet), target.DeclaringType, $"{map.InterfaceMethods[i].Name} is not declared by the type.");
        }
    }
}
