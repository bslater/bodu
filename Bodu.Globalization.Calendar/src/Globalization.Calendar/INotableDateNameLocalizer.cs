using System.Globalization;

namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Translates a canonical notable date name into the active culture's display form.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="NotableDateRule.Name" /> is authored in invariant English. Implementations of <see cref="INotableDateNameLocalizer" />
	/// resolve that key to a culture-specific string — typically by looking it up in a <c>.resx</c> file or external translation catalogue.
	/// </para>
	/// </remarks>
	public interface INotableDateNameLocalizer
	{
		/// <summary>
		/// Returns the display name for the supplied notable date in the requested culture.
		/// </summary>
		/// <param name="notableDate">The notable date being rendered. Must not be <see langword="null" />.</param>
		/// <param name="culture">The target culture. <see langword="null" /> defaults to <see cref="CultureInfo.CurrentCulture" />.</param>
		/// <returns>The localised display name. Implementations should fall back to <see cref="NotableDate.Name" /> when no translation is found.</returns>
		string GetDisplayName(NotableDate notableDate, CultureInfo? culture = null);
	}
}
