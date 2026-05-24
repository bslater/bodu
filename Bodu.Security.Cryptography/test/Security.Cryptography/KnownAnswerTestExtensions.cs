// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KnownAnswerTestExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Extension methods for <see cref="KnownAnswerTest" /> providing typed access to the loosely-typed
/// <see cref="KnownAnswerTest.Parameters" /> dictionary.
/// </summary>
public static class KnownAnswerTestExtensions
{
    public static bool TryGet<T>(this KnownAnswerTest kat, string key, out T? value)
    {
        if (kat.Parameters.TryGetValue(key, out var obj) && obj is T t)
        {
            value = t;
            return true;
        }
        value = default;
        return false;
    }
}
