// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositePluginTrustPolicy.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Composes two or more <see cref="IPluginTrustPolicy" /> instances under AND semantics: every child policy must return trusted
/// for the candidate to be admitted. The first child to reject short-circuits the evaluation and surfaces its reason.
/// </summary>
/// <remarks>
/// Typical usage pairs strong-name validation with byte-hash pinning, so a tampered-but-correctly-tokened assembly fails the
/// hash check while an assembly with the right bytes but the wrong token fails the strong-name check.
/// </remarks>
public sealed class CompositePluginTrustPolicy : IPluginTrustPolicy
{
	/// <summary>The ordered list of child policies evaluated in sequence; the first rejection short-circuits the rest.</summary>
	private readonly ImmutableArray<IPluginTrustPolicy> _policies;

	/// <summary>
	/// Initialises a new instance of the <see cref="CompositePluginTrustPolicy" /> class.
	/// </summary>
	/// <param name="policies">The child policies, evaluated in the order supplied. Must not be <see langword="null" /> and must contain at least one non-<see langword="null" /> policy.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="policies" /> is <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="policies" /> is empty or contains a <see langword="null" /> element.</exception>
	public CompositePluginTrustPolicy(params IPluginTrustPolicy[] policies)
	{
		if (policies is null) throw new ArgumentNullException(nameof(policies));
		if (policies.Length == 0)
			throw new ArgumentException(CalendarResourceStrings.ArgumentException_PoliciesEmpty, nameof(policies));
		foreach (var policy in policies)
		{
			if (policy is null)
				throw new ArgumentException(CalendarResourceStrings.ArgumentException_PoliciesContainNull, nameof(policies));
		}

		_policies = policies.ToImmutableArray();
	}

	/// <inheritdoc />
	public PluginTrustResult Evaluate(PluginTrustContext context)
	{
		foreach (var policy in _policies)
		{
			var result = policy.Evaluate(context);
			if (!result.Trusted)
				return result;
		}

		return new PluginTrustResult(Trusted: true, Reason: null);
	}
}
