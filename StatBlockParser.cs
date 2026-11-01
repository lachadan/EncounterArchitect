using System.Text.RegularExpressions;

namespace EncounterArchitect;

public sealed record MonsterDraft(string Name, string Type, int? AC, int? HP,
    int? Initiative, string CR, string Stats, IReadOnlyList<string> Warnings);

// Parses copied visible text, not web pages or authenticated D&D Beyond endpoints.
public static class StatBlockParser
{
    static readonly RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.Multiline;
    static Match Find(string text, string pattern) => Regex.Match(text, pattern, Options, TimeSpan.FromSeconds(1));

    public static MonsterDraft Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) throw new FormatException("Paste one monster's stat block first.");
        if (input.Length > 200_000) throw new FormatException("Paste one monster at a time (up to 200,000 characters).");
        var text = input.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\u00a0', ' ')
            .Replace('−', '-').Replace('–', '-').Trim();
        const string typePattern = @"^\s*(?:Tiny|Small|Medium|Large|Huge|Gargantuan)\b[^\n]*\b(?:Aberration|Beast|Celestial|Construct|Dragon|Elemental|Fey|Fiend|Giant|Humanoid|Monstrosit(?:y|ies)|Ooze|Plant|Undead)s?\b[^\n]*";
        var types = Regex.Matches(text, typePattern, Options, TimeSpan.FromSeconds(1));
        if (types.Count == 0) throw new FormatException("Couldn't find a monster size and type. Copy the monster name, size/type line, and complete stat block, then try again.");
        if (types.Count > 1) throw new FormatException("More than one stat block was detected. Paste one monster at a time.");
        var type = types[0];
        var name = text[..type.Index].Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).LastOrDefault(s => s.Length > 0 && !s.Equals("Legacy", StringComparison.OrdinalIgnoreCase)) ?? "";
        var block = text[(type.Index + type.Length)..];
        // Don't mistake HP, AC, or initiative mentioned in an action for the header.
        var actions = Find(block, @"^\s*(?:Traits|Actions|Bonus Actions|Reactions|Legendary Actions)\s*$");
        var header = actions.Success ? block[..actions.Index] : block;
        int? Number(string pattern) { var m = Find(header, pattern); return m.Success && int.TryParse(m.Groups[1].Value, out int n) ? n : null; }
        int? ac = Number(@"\b(?:Armor\s+Class|AC)\s*:?\s*(\d+)\b");
        int? hp = Number(@"\b(?:Hit\s+Points|HP)\s*:?\s*(\d+)\b");
        int? initiative = Number(@"\bInitiative\s*:?\s*([+-]?\d+)\b");
        var challenge = Find(header, @"\b(?:Challenge|CR)\s*:?\s*(\d+\s*/\s*\d+|\d+)\b");
        var cr = challenge.Success ? Regex.Replace(challenge.Groups[1].Value, @"\s+", "") : "?";
        var warnings = new List<string>();
        if (name.Length == 0) warnings.Add("Name not detected. Enter it below.");
        if (ac is null) warnings.Add("AC not detected. Enter it below.");
        if (hp is null) warnings.Add("HP not detected. Enter it below.");
        if (initiative is null) warnings.Add("Initiative bonus not detected. Enter it below. Older stat blocks often omit this field; don't use passive initiative as the bonus.");
        if (cr == "?") warnings.Add("Challenge rating not detected. You can leave it as ?.");
        warnings.Add("Check the edition and every detected value against the original. The importer does not convert older monsters to the 2024 rules.");
        return new(name, type.Value.Trim(), ac, hp, initiative, cr, text, warnings);
    }
}
