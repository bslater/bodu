// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HebrewNotableDateAlgorithmPlugin.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Provides the built-in <see cref="INotableDateAlgorithmPlugin" /> that supplies <see cref="HebrewNotableDateAlgorithm" />
/// instances for the seven Jewish observances declared in the embedded <c>global-jewish.xml</c> rule resource.
/// </summary>
/// <remarks>
/// <para>
/// Pass an instance of this plugin to <see cref="NotableDateService" /> through its <c>plugins</c> parameter to make the
/// Hebrew observances declared in <c>global-jewish.xml</c> resolvable without requiring callers to populate an
/// <see cref="INotableDateAlgorithmRegistry" /> by hand.
/// </para>
/// <para>
/// The plugin contributes one algorithm per registered key:
/// </para>
/// <list type="bullet">
///   <item><description><c>rosh-hashanah</c> — 1 Tishrei.</description></item>
///   <item><description><c>yom-kippur</c> — 10 Tishrei.</description></item>
///   <item><description><c>sukkot</c> — 15 Tishrei.</description></item>
///   <item><description><c>hanukkah</c> — 25 Kislev.</description></item>
///   <item><description><c>purim</c> — 14 of the last Adar (Adar I in standard years, Adar II in leap years).</description></item>
///   <item><description><c>passover</c> — 15 Nisan.</description></item>
///   <item><description><c>shavuot</c> — 6 Sivan.</description></item>
/// </list>
/// <para>
/// Multi-day spans (Sukkot, Hanukkah, Passover) are expressed by <c>durationDays</c> on the originating rule, not by this
/// plugin; each algorithm only resolves the anchor (first) day. Caller-supplied registrations on the same key take
/// precedence over the entries contributed here when both are configured.
/// </para>
/// </remarks>
public sealed class HebrewNotableDateAlgorithmPlugin : INotableDateAlgorithmPlugin
{
	/// <summary>
	/// The stable plugin identifier reported through <see cref="Name" />.
	/// </summary>
	public const string PluginName = "Bodu.Globalization.Calendar.Hebrew";

	private static readonly KeyValuePair<string, INotableDateAlgorithm>[] s_algorithms =
	{
		new("rosh-hashanah", new HebrewNotableDateAlgorithm(HebrewMonth.Tishri, 1)),
		new("yom-kippur",    new HebrewNotableDateAlgorithm(HebrewMonth.Tishri, 10)),
		new("sukkot",        new HebrewNotableDateAlgorithm(HebrewMonth.Tishri, 15)),
		new("hanukkah",      new HebrewNotableDateAlgorithm(HebrewMonth.Kislev, 25)),
		new("purim",         new HebrewNotableDateAlgorithm(HebrewMonth.LastAdar, 14)),
		new("passover",      new HebrewNotableDateAlgorithm(HebrewMonth.Nisan, 15)),
		new("shavuot",       new HebrewNotableDateAlgorithm(HebrewMonth.Sivan, 6)),
	};

	/// <inheritdoc />
	public string Name => PluginName;

	/// <inheritdoc />
	public Version Version { get; } = new(1, 0, 0);

	/// <inheritdoc />
	public IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms() => s_algorithms;
}
