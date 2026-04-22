
namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the parsing and containment semantics of <see cref="TerritoryCode" />.
/// </summary>
[TestClass]
public sealed class TerritoryCodeTests
{
	/// <summary>
	/// Verifies that parsing a country-only code returns a value with no subdivision.
	/// </summary>
	[TestMethod]
	public void TryParse_WhenCountryOnly_ShouldReturnValueWithoutSubdivision()
	{
		Assert.IsTrue(TerritoryCode.TryParse("AU", out var result));
		Assert.AreEqual("AU", result.Country);
		Assert.IsNull(result.Subdivision);
		Assert.IsFalse(result.HasSubdivision);
	}

	/// <summary>
	/// Verifies that parsing a country and subdivision pair populates both components.
	/// </summary>
	[TestMethod]
	public void TryParse_WhenCountryAndSubdivision_ShouldPopulateBoth()
	{
		Assert.IsTrue(TerritoryCode.TryParse("AU-NSW", out var result));
		Assert.AreEqual("AU", result.Country);
		Assert.AreEqual("NSW", result.Subdivision);
		Assert.IsTrue(result.HasSubdivision);
	}

	/// <summary>
	/// Verifies that parsing trims surrounding whitespace and uppercases the components.
	/// </summary>
	[TestMethod]
	public void TryParse_WhenInputHasWhitespaceAndLowercase_ShouldNormaliseToUpper()
	{
		Assert.IsTrue(TerritoryCode.TryParse("  au-nsw  ", out var result));
		Assert.AreEqual("AU", result.Country);
		Assert.AreEqual("NSW", result.Subdivision);
	}

	/// <summary>
	/// Verifies that parsing a three-letter country code fails (only ISO 3166-1 alpha-2 is valid).
	/// </summary>
	[TestMethod]
	public void TryParse_WhenCountryHasThreeLetters_ShouldFail()
	{
		Assert.IsFalse(TerritoryCode.TryParse("AUS", out _));
	}

	/// <summary>
	/// Verifies that parsing an empty string returns false.
	/// </summary>
	[TestMethod]
	public void TryParse_WhenInputIsEmpty_ShouldReturnFalse()
	{
		Assert.IsFalse(TerritoryCode.TryParse(string.Empty, out _));
	}

	/// <summary>
	/// Verifies that <see cref="TerritoryCode.Parse" /> throws on invalid input.
	/// </summary>
	[TestMethod]
	public void Parse_WhenInputIsInvalid_ShouldThrowFormatException()
	{
		Assert.ThrowsExactly<FormatException>(() => TerritoryCode.Parse("BAD!"));
	}

	/// <summary>
	/// Verifies that a country-only territory contains itself.
	/// </summary>
	[TestMethod]
	public void Contains_WhenSameCountry_ShouldReturnTrue()
	{
		var au = TerritoryCode.Parse("AU");
		Assert.IsTrue(au.Contains(au));
	}

	/// <summary>
	/// Verifies that a country contains any of its subdivisions.
	/// </summary>
	[TestMethod]
	public void Contains_WhenCountryAndSubdivisionShareCountry_ShouldReturnTrue()
	{
		var au = TerritoryCode.Parse("AU");
		var auNsw = TerritoryCode.Parse("AU-NSW");

		Assert.IsTrue(au.Contains(auNsw));
	}

	/// <summary>
	/// Verifies that a subdivision does not contain its parent country (containment is downward only).
	/// </summary>
	[TestMethod]
	public void Contains_WhenSubdivisionAgainstParentCountry_ShouldReturnFalse()
	{
		var au = TerritoryCode.Parse("AU");
		var auNsw = TerritoryCode.Parse("AU-NSW");

		Assert.IsFalse(auNsw.Contains(au));
	}

	/// <summary>
	/// Verifies that two unrelated countries are never considered to contain each other.
	/// </summary>
	[TestMethod]
	public void Contains_WhenDifferentCountries_ShouldReturnFalse()
	{
		var au = TerritoryCode.Parse("AU");
		var nz = TerritoryCode.Parse("NZ");

		Assert.IsFalse(au.Contains(nz));
	}

	/// <summary>
	/// Verifies that <see cref="TerritoryCode.ParseList" /> splits a comma-separated list and skips invalid entries.
	/// </summary>
	[TestMethod]
	public void ParseList_WhenMixedValidAndInvalid_ShouldReturnValidEntriesOnly()
	{
		var list = TerritoryCode.ParseList("AU, NZ, BAD!, US-CA");

		Assert.AreEqual(3, list.Count);
		Assert.AreEqual("AU", list[0].ToString());
		Assert.AreEqual("NZ", list[1].ToString());
		Assert.AreEqual("US-CA", list[2].ToString());
	}

	/// <summary>
	/// Verifies that <see cref="TerritoryCode.ToString" /> round-trips canonical forms.
	/// </summary>
	[TestMethod]
	public void ToString_WhenSubdivisionPresent_ShouldReturnCountryDashSubdivision()
	{
		var auNsw = TerritoryCode.Parse("au-nsw");
		Assert.AreEqual("AU-NSW", auNsw.ToString());
	}
}
