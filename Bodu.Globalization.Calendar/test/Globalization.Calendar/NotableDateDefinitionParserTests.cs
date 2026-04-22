// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDefinitionParserTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar
{
	[TestClass]
	public partial class NotableDateDefinitionParserTests
	{
		/// <summary>
		/// Verifies that parsing the same data from XML and JSON produces equivalent NotableDateDefinitions.
		/// </summary>
		[TestMethod]
		public void ParseXmlAndParseJson_ShouldProduceEquivalentDefinitions()
		{
			var xmlDefinitions = NotableDateDefinitionParser.ParseXml(XDocument.Parse(NotableDateDefinitionParserTests.ComplexNotableDatesXml)).ToImmutableArray();
			var jsonDefinitions = NotableDateDefinitionParser.ParseJson(NotableDateDefinitionParserTests.ComplexNotableDatesJson).ToImmutableArray();

			Assert.AreEqual(xmlDefinitions, jsonDefinitions);
		}
	}
}