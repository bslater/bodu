// ---------------------------------------------------------------------------------------------------------------
// TEMPORARY corpus classifier generator. Deleted after the classification manifest is finalized.
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

[TestClass]
public sealed class _ClassifierGen
{
    private static readonly string CorpusRoot = Path.Combine(AppContext.BaseDirectory, "YamlTestCorpus");

    [TestMethod]
    public void Debug()
    {
        foreach (var id in new[] { "JHB9", "6XDY", "S4JQ" })
        {
            var dir = Path.Combine(CorpusRoot, id);
            var yaml = File.ReadAllBytes(Path.Combine(dir, "in.yaml"));
            var jsonBytes = File.ReadAllBytes(Path.Combine(dir, "in.json"));
            string line;
            try
            {
                var exp = CorpusCompare.SplitJsonValues(jsonBytes);
                var docs = YamlDocument.ParseAllDocuments(yaml);
                line = $"{id}: expected={exp.Count} docs={docs.Count}";
                if (exp.Count == docs.Count)
                {
                    for (var i = 0; i < docs.Count; i++)
                        line += $" [{i}]match={CorpusCompare.Matches(exp[i], docs[i].RootElement)},kind={docs[i].RootElement.ValueKind}";
                }
            }
            catch (Exception ex)
            {
                line = $"{id}: EX {ex.GetType().Name}: {ex.Message}";
            }

            Console.WriteLine("DBG " + line);
        }
    }

    [TestMethod]
    public void Generate()
    {
        var rows = new List<string>();
        var gaps = new List<string>();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var dir in Directory.EnumerateDirectories(CorpusRoot, "*", SearchOption.AllDirectories))
        {
            var inYaml = Path.Combine(dir, "in.yaml");
            if (!File.Exists(inYaml))
                continue;

            var id = Path.GetRelativePath(CorpusRoot, dir).Replace('\\', '/');
            var isError = File.Exists(Path.Combine(dir, "error"));
            var jsonPath = Path.Combine(dir, "in.json");
            var hasJson = File.Exists(jsonPath);
            var yaml = File.ReadAllBytes(inYaml);

            string category;
            if (isError)
                category = Throws(yaml) ? "SupportedFail" : "KnownGap";
            else if (hasJson)
                category = ClassifyValid(yaml, jsonPath);
            else
                category = ParsesCleanly(yaml) ? "SupportedParseOnly" : ClassifyRejected(yaml);

            var kind = isError ? "invalid" : hasJson ? "valid" : "valid-nojson";
            rows.Add($"{id}\t{kind}\t{category}");
            counts[category] = counts.GetValueOrDefault(category) + 1;

            if (category == "KnownGap")
                gaps.Add($"{id}\t{kind}\t{File.ReadAllText(Path.Combine(dir, "name")).Trim()}");
        }

        rows.Sort(StringComparer.Ordinal);
        gaps.Sort(StringComparer.Ordinal);
        File.WriteAllLines(Path.Combine(Path.GetTempPath(), "yaml_classification.tsv"), rows);
        File.WriteAllLines(Path.Combine(Path.GetTempPath(), "yaml_gaps.tsv"), gaps);

        var summary = new StringBuilder("CLASSIFY_BEGIN\n");
        foreach (var kv in counts.OrderBy(static k => k.Key, StringComparer.Ordinal))
            summary.Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
        summary.Append("CLASSIFY_END");
        Console.WriteLine(summary.ToString());
    }

    private static bool Throws(byte[] yaml)
    {
        try
        {
            using var _ = YamlDocument.Parse(yaml);
            return false;
        }
        catch (YamlFormatException)
        {
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ParsesCleanly(byte[] yaml)
    {
        try
        {
            using var _ = YamlDocument.Parse(yaml);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ClassifyRejected(byte[] yaml)
    {
        try
        {
            using var _ = YamlDocument.Parse(yaml);
            return "KnownGap";
        }
        catch (YamlFormatException)
        {
            return "UnsupportedFeatureRejected";
        }
        catch
        {
            return "KnownGap";
        }
    }

    private static string ClassifyValid(byte[] yaml, string jsonPath)
    {
        var jsonBytes = File.ReadAllBytes(jsonPath);
        var jsonText = Encoding.UTF8.GetString(jsonBytes).Trim();

        if (jsonText.Length == 0)
        {
            try
            {
                using var doc = YamlDocument.Parse(yaml);
                return doc.RootElement.ValueKind == YamlValueKind.Null ? "SupportedPass" : "KnownGap";
            }
            catch (YamlFormatException)
            {
                return "UnsupportedFeatureRejected";
            }
            catch
            {
                return "KnownGap";
            }
        }

        List<System.Text.Json.JsonElement> expected;
        try
        {
            expected = CorpusCompare.SplitJsonValues(jsonBytes);
        }
        catch
        {
            return "KnownGap";
        }

        try
        {
            if (expected.Count == 1)
            {
                using var doc = YamlDocument.Parse(yaml);
                return CorpusCompare.Matches(expected[0], doc.RootElement) ? "SupportedPass" : "KnownGap";
            }

            var docs = YamlDocument.ParseAllDocuments(yaml);
            if (docs.Count != expected.Count)
                return "KnownGap";

            for (var i = 0; i < docs.Count; i++)
            {
                if (!CorpusCompare.Matches(expected[i], docs[i].RootElement))
                    return "KnownGap";
            }

            return "SupportedPass";
        }
        catch (YamlFormatException)
        {
            return "UnsupportedFeatureRejected";
        }
        catch
        {
            return "KnownGap";
        }
    }
}
