using Bodu.Extensions;

namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Resolves the base date of a notable date definition based on its type and dependencies.
	/// </summary>
	internal sealed class NotableDateResolver
	{
		private readonly IReadOnlyList<NotableDateDefinition> _definitions;

		/// <summary>
		/// Initializes a new instance of the <see cref="NotableDateResolver" /> class.
		/// </summary>
		/// <param name="definitions">The list of notable date definitions available for resolution.</param>
		public NotableDateResolver(IReadOnlyList<NotableDateDefinition> definitions)
		{
			_definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
		}

		/// <summary>
		/// Resolves the base date for a given notable date definition and year.
		/// </summary>
		/// <param name="definition">The notable date definition to resolve.</param>
		/// <param name="year">The year for which to resolve the date.</param>
		/// <returns>The resolved date, or <c>null</c> if it cannot be resolved.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when circular dependencies are detected or referenced definitions are missing.
		/// </exception>
		public DateTime? ResolveBaseDate(NotableDateDefinition definition, int year)
		{
			return ResolveBaseDateInternal(definition, year, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		}

		private DateTime? ResolveBaseDateInternal(NotableDateDefinition definition, int year, HashSet<string> resolving)
		{
			ThrowHelper.ThrowIfNull(definition);

			if (definition.FirstYear.HasValue && year < definition.FirstYear.Value) return null;
			if (definition.LastYear.HasValue && year > definition.LastYear.Value) return null;

			if (!resolving.Add(definition.Name))
				throw new InvalidOperationException($"Circular dependency detected: {string.Join(" → ", resolving.Concat(new[] { definition.Name }))}");

			try
			{
				switch (definition.DefinitionType)
				{
					case NotableDateDefinitionType.Fixed:
						if (definition.Month.HasValue && definition.Day.HasValue)
							return new DateTime(year, definition.Month.Value, definition.Day.Value, 0, 0, 0, DateTimeKind.Unspecified);
						break;

					case NotableDateDefinitionType.Rule:
						if (definition.Month.HasValue && definition.WeekOrdinal.HasValue && definition.DayOfWeek.HasValue)
							return DateTimeExtensions.GetNthDayOfWeekInMonth(year, definition.Month.Value, definition.DayOfWeek.Value, definition.WeekOrdinal.Value);
						break;

					case NotableDateDefinitionType.OffsetFrom:
						if (!string.IsNullOrWhiteSpace(definition.BaseNotableDateName))
						{
							var baseDefinition = _definitions.FirstOrDefault(d => string.Equals(d.Name, definition.BaseNotableDateName, StringComparison.OrdinalIgnoreCase));
							if (baseDefinition == null)
								throw new InvalidOperationException($"Base notable date '{definition.BaseNotableDateName}' not found for '{definition.Name}'.");

							var baseDate = ResolveBaseDateInternal(baseDefinition, year, resolving);

							if (baseDate.HasValue && definition.OffsetDays.HasValue)
								return baseDate.Value.AddDays(definition.OffsetDays.Value);
						}
						break;

					case NotableDateDefinitionType.Dynamic:
						var calculator = Activator.CreateInstance(definition.NotableDateCalculatorType) as INotableDateCalculator;
						return calculator?.GetDate(year);

					default:
						throw new NotSupportedException($"Unsupported definition type '{definition.DefinitionType}' for '{definition.Name}'.");
				}

				return null;
			}
			finally
			{
				resolving.Remove(definition.Name);
			}
		}
	}
}