using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis.Text;

namespace Bodu.Rosyln.Generators
{
	[Generator]
	public sealed class CrcSpecGenerator
		: IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			// 1️⃣ Pick up crc-specs.json
			var specsProvider =
				context.AdditionalTextsProvider
				   .Where(f => Path.GetFileName(f.Path).Equals("crc-specs.json",
							StringComparison.OrdinalIgnoreCase))
				   .Select(static (file, _) =>
				   {
					   var text = file.GetText()!.ToString();
					   return JsonSerializer.Deserialize<List<CrcSpec>>(text,
							   new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
				   });

			// 2️⃣ Read namespace from MSBuild properties
			var nsProvider =
				context.AnalyzerConfigOptionsProvider
				   .Select(static (opts, _) =>
				   {
					   // Try <CrcGenNamespace> first
					   if (opts.GlobalOptions.TryGetValue("build_property.CrcGenNamespace", out var ns) && !string.IsNullOrWhiteSpace(ns))
						   return ns.Trim();

					   // Fall back to project RootNamespace (SDK-style projects)
					   if (opts.GlobalOptions.TryGetValue("build_property.RootNamespace", out ns) && !string.IsNullOrWhiteSpace(ns))
						   return ns.Trim();

					   // Final fallback
					   return "GeneratedCrc";
				   });

			// 3️⃣ Combine specs + namespace
			var combo = specsProvider.Combine(nsProvider);

			// 4️⃣ Emit the source
			context.RegisterSourceOutput(combo, static (spc, pair) =>
			{
				var (specs, ns) = pair;
				var src = CrcCodeEmitter.Emit(specs, ns);
				spc.AddSource("CrcStandards.g.cs", SourceText.From(src, Encoding.UTF8));
			});
		}
	}
}