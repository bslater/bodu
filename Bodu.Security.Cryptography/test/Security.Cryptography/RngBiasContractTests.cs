// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RngBiasContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Library-wide contract tests for cryptographically secure key, IV, nonce, and tweak generation.
/// </summary>
[TestClass]
public sealed class RngBiasContractTests
{
    private static readonly string[] s_generateMethodNames =
    [
        "GenerateKey",
        "GenerateIV",
        "GenerateNonce",
        "GenerateTweak",
    ];

    private static readonly string[] s_forbiddenHelperNames =
    [
        "GetRandomNonZeroBytes",
        "FillWithRandomNonZeroBytes",
        "TryFillWithRandomNonZeroBytes",
    ];

    private static readonly int[] s_singleByteOperandSize = BuildOperandSizeTable(twoByte: false);
    private static readonly int[] s_twoByteOperandSize = BuildOperandSizeTable(twoByte: true);

    /// <summary>
    /// Verifies that no <c>GenerateKey</c>, <c>GenerateIV</c>, <c>GenerateNonce</c>, or <c>GenerateTweak</c>
    /// method in <see cref="SymmetricStreamAlgorithm" />'s assembly directly calls a non-zero RNG helper.
    /// Keys, IVs, nonces, and tweaks must be sampled uniformly across all byte values; excluding <c>0x00</c>
    /// introduces statistical bias.
    /// </summary>
    /// <remarks>
    /// This is an IL-level static contract test. It walks the compiled IL of every <c>Generate*</c> method
    /// in the production assembly, resolves every <c>call</c>, <c>callvirt</c>, or <c>newobj</c> token, and
    /// asserts that none resolve to a <c>CryptoHelpers</c> non-zero RNG helper. The forbidden helpers remain
    /// public-to-internal because they have legitimate non-key uses (for example, ISO 10126 random padding);
    /// the contract is specifically about key, IV, nonce, and tweak generation.
    /// </remarks>
    [TestMethod]
    public void GenerateKeyIvNonceTweak_AcrossAllAlgorithms_ShouldNotCallNonZeroRng()
    {
        Assembly assembly = typeof(SymmetricStreamAlgorithm).Assembly;
        Type helpersType = assembly.GetType("Bodu.Security.Cryptography.CryptographyHelper", throwOnError: true, ignoreCase: false)!;

        var forbiddenTokens = new HashSet<int>();
        foreach (MethodInfo m in helpersType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (Array.IndexOf(s_forbiddenHelperNames, m.Name) >= 0)
                forbiddenTokens.Add(m.MetadataToken);
        }

        Assert.IsTrue(forbiddenTokens.Count > 0, "Expected CryptographyHelper to expose at least one non-zero RNG helper.");

        var violations = new List<string>();

        foreach (Type type in assembly.GetTypes())
        {
            const BindingFlags binding =
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly;

            foreach (MethodInfo method in type.GetMethods(binding))
            {
                if (Array.IndexOf(s_generateMethodNames, method.Name) < 0) continue;
                if (method.IsAbstract) continue;

                MethodBody? body = method.GetMethodBody();
                byte[]? il = body?.GetILAsByteArray();
                if (il is null || il.Length == 0) continue;

                foreach (int token in EnumerateCallTokens(il))
                {
                    if (forbiddenTokens.Contains(token))
                    {
                        string helper = ResolveMethodName(type.Module, token);
                        violations.Add($"{type.FullName}.{method.Name} → {helper}");
                    }
                }
            }
        }

        if (violations.Count > 0)
        {
            var sb = new StringBuilder("Generate* methods must use full-range RNG. Found ");
            sb.Append(violations.Count).Append(" violation(s):\n");
            foreach (string v in violations)
                sb.Append("  ").Append(v).Append('\n');
            Assert.Fail(sb.ToString());
        }
    }

    private static string ResolveMethodName(Module module, int token)
    {
        try
        {
            return module.ResolveMethod(token)?.Name ?? "<unresolved>";
        }
        catch
        {
            return "<unresolved>";
        }
    }

    private static IEnumerable<int> EnumerateCallTokens(byte[] il)
    {
        int pos = 0;
        while (pos < il.Length)
        {
            byte first = il[pos];

            // Two-byte opcode prefix (0xFE). None of the call-family opcodes use this prefix.
            if (first == 0xFE)
            {
                if (pos + 1 >= il.Length) yield break;
                pos += 2 + s_twoByteOperandSize[il[pos + 1]];
                continue;
            }

            int operandSize = s_singleByteOperandSize[first];

            // call (0x28), callvirt (0x6F), newobj (0x73): operand is a 4-byte metadata token.
            if (first is 0x28 or 0x6F or 0x73)
            {
                if (pos + 5 > il.Length) yield break;
                yield return BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(pos + 1, 4));
            }
            else if (first == 0x45) // switch: 4-byte count followed by count*4 jump offsets.
            {
                if (pos + 5 > il.Length) yield break;
                int n = BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(pos + 1, 4));
                operandSize = 4 + (n * 4);
            }

            pos += 1 + operandSize;
        }
    }

    private static int[] BuildOperandSizeTable(bool twoByte)
    {
        int[] table = new int[256];
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var op = (OpCode)field.GetValue(null)!;
            ushort value = unchecked((ushort)op.Value);
            bool fieldIsTwoByte = (value >> 8) == 0xFE;
            if (twoByte != fieldIsTwoByte) continue;
            table[value & 0xFF] = OperandTypeSize(op.OperandType);
        }

        return table;
    }

    private static int OperandTypeSize(OperandType type) => type switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineI or OperandType.ShortInlineVar or OperandType.ShortInlineBrTarget => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineBrTarget
            or OperandType.InlineField
            or OperandType.InlineI
            or OperandType.InlineMethod
            or OperandType.InlineSig
            or OperandType.InlineString
            or OperandType.InlineTok
            or OperandType.InlineType
            or OperandType.ShortInlineR => 4,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => 0, // Variable; handled in EnumerateCallTokens.
        _ => 0,
    };
}
