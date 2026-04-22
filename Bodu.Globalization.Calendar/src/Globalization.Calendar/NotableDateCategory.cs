
namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides a coarse-grained classification of a notable date.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="NotableDateCategory" /> assigns a notable date to a single primary category. For finer-grained or overlapping
/// classifications (for example a date that is simultaneously <c>Holiday</c> and <c>Christian</c>), use the
/// <c>NotableDate.Tags</c> collection in addition to the category.
/// </para>
/// <para>The enumeration replaces the earlier <c>NotableDateKind</c> type and uses <c>Category</c> to avoid clashing with <see cref="DateTimeKind" />.</para>
/// </remarks>
public enum NotableDateCategory
{
	/// <summary>
	/// No primary category is assigned.
	/// </summary>
	None = 0,

	/// <summary>
	/// A public holiday, typically recognised with an official day off work or school.
	/// </summary>
	Holiday,

	/// <summary>
	/// A religious, cultural, or secular observance that may or may not involve public closure.
	/// </summary>
	Observance,

	/// <summary>
	/// A date dedicated to remembering significant historical events, individuals, or groups.
	/// </summary>
	Remembrance,

	/// <summary>
	/// A cultural celebration tied to a specific community, ethnicity, or tradition.
	/// </summary>
	Cultural,

	/// <summary>
	/// A seasonal marker such as a solstice, equinox, or daylight-saving transition.
	/// </summary>
	Seasonal,

	/// <summary>
	/// A notable date that does not fit any other primary category.
	/// </summary>
	Other,
}
