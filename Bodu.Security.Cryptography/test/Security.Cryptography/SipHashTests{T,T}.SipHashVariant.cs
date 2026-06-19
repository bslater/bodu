// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests{T,T}.SipHashVariant.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the SipHash round configurations exercised by <see cref="SipHashTests{TTest, TAlgorithm}" />:
/// the standard SipHash-2-4 and the higher-security SipHash-4-8.
/// </summary>
public enum SipHashVariant
{
    SipHash_2_4,
    SipHash_4_8
}
