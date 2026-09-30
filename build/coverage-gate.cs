using System.Text.Json;
using System.Text.RegularExpressions;

// Pragurile se aplică pe raportul combinat (ReportGenerator, JsonSummary), nu pe fiecare proiect de test.
Gate[] gates =
[
    new("KingdomDuel.Domain", Lines: 93, Branches: 93),
    new("KingdomDuel.Shared", Lines: 95),
    new("KingdomDuel.Application", Lines: 83, Branches: 70),
    new("KingdomDuel.Persistence", Lines: 80),
    new("KingdomDuel.Api", Lines: 75),
    new("KingdomDuel.Client", Lines: 90, Classes: @"^KingdomDuel\.Client\.(Authentication\.|Routes$|Components\.OrderDraft$)"),
];

var path = args.Length > 0 ? args[0] : "coverage/Summary.json";

if (!File.Exists(path))
{
    Console.WriteLine($"::error::Nu există {path}. Rulează întâi testele cu coverage și ReportGenerator cu -reporttypes:JsonSummary.");
    return 1;
}

using var summary = JsonDocument.Parse(File.ReadAllText(path));
var assemblies = summary.RootElement.GetProperty("coverage").GetProperty("assemblies");

var failed = false;

foreach (var gate in gates)
{
    var assembly = assemblies.EnumerateArray().FirstOrDefault(a => a.GetProperty("name").GetString() == gate.Assembly);

    if (assembly.ValueKind == JsonValueKind.Undefined)
    {
        Report(gate.Assembly, "linii", 0, gate.Lines);
        continue;
    }

    if (gate.Classes is { } pattern)
    {
        var classes = assembly.GetProperty("classesinassembly").EnumerateArray()
            .Where(c => Regex.IsMatch(c.GetProperty("name").GetString()!, pattern))
            .ToList();

        var covered = classes.Sum(c => c.GetProperty("coveredlines").GetInt32());
        var coverable = classes.Sum(c => c.GetProperty("coverablelines").GetInt32());

        Report($"{gate.Assembly} (logică)", "linii", coverable == 0 ? 0 : 100.0 * covered / coverable, gate.Lines);
        continue;
    }

    Report(gate.Assembly, "linii", Rate(assembly, "coverage"), gate.Lines);

    if (gate.Branches is { } branches)
    {
        Report(gate.Assembly, "ramuri", Rate(assembly, "branchcoverage"), branches);
    }
}

return failed ? 1 : 0;

static double Rate(JsonElement assembly, string property) =>
    assembly.GetProperty(property) is { ValueKind: JsonValueKind.Number } value ? value.GetDouble() : 0;

void Report(string name, string metric, double actual, double minimum)
{
    var ok = actual >= minimum;
    failed |= !ok;

    Console.WriteLine(ok
        ? $"{name}: {metric} {actual:0.0}% (prag {minimum}%)"
        : $"::error::{name}: {metric} {actual:0.0}% sub pragul de {minimum}%");
}

record Gate(string Assembly, double Lines, double? Branches = null, string? Classes = null);
