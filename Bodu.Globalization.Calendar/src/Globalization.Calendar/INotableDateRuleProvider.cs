
namespace Bodu.Globalization.Calendar;

/// <summary>
/// Supplies <see cref="NotableDateRule" /> instances to a <see cref="NotableDateService" /> from an underlying authoring source such as
/// an embedded XML resource, JSON document, database, or external configuration system.
/// </summary>
/// <remarks>
/// Rule providers are loaded once per <see cref="NotableDateService" /> instance. To express runtime-only modifications such as
/// disabling a holiday or adding a one-off observance, use an <see cref="INotableDateRuleOverrideProvider" /> alongside the base
/// providers.
/// </remarks>
public interface INotableDateRuleProvider
{
	/// <summary>
	/// Loads every <see cref="NotableDateRule" /> exposed by this provider.
	/// </summary>
	/// <returns>The notable date rules.</returns>
	/// <exception cref="System.Exception">Thrown if the underlying source cannot be loaded or is invalid.</exception>
	IEnumerable<NotableDateRule> LoadRules();
}
