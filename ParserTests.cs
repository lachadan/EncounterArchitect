using System.Reflection;
using System.Text.Json;

namespace EncounterArchitect;

static class ParserTests
{
    public static void Run()
    {
        // Real embedded SRD text protects against regressions across every bundled monster.
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("EncounterArchitect.monsters.json")!;
        var monsters=JsonSerializer.Deserialize<List<Monster>>(stream)!;
        foreach(var m in monsters) {
            var d=StatBlockParser.Parse(m.Stats);
            if(d.Name!=m.Name||d.HP!=m.HP||d.AC!=m.AC||d.Initiative!=m.Initiative||d.CR!=m.CR)
                throw new Exception($"Stat block parser mismatch: {m.Name}");
        }
        const string modern="Test Sentinel\nLarge Construct, Unaligned\nAC\n17\nInitiative\n−2 (8)\nHP\n85 (10d10 + 30)\nCR\n5 (XP 1,800)\nActions\nSlam. Damage.";
        var parsed=StatBlockParser.Parse(modern);
        if(parsed.Name!="Test Sentinel"||parsed.AC!=17||parsed.HP!=85||parsed.Initiative!=-2||parsed.CR!="5")throw new Exception("Multiline/Unicode parser");
        const string legacy="Legacy Test\nMedium humanoid (human), neutral\nArmor Class 12 (leather armor)\nHit Points 11 (2d8 + 2)\nChallenge 1 / 4 (50 XP)\nActions\nAttack. Initiative +99 and HP 999 are not header values.";
        var old=StatBlockParser.Parse(legacy);
        if(old.Initiative!=null||old.AC!=12||old.HP!=11||old.CR!="1/4")throw new Exception("Legacy/missing initiative parser");
        var missing=StatBlockParser.Parse("Unnamed Beast\nSmall Beast, Unaligned\nActions\nBite. HP 99.");
        if(missing.HP!=null||missing.AC!=null||missing.Initiative!=null||missing.Warnings.Count<4)throw new Exception("Missing field warnings");
        foreach(var invalid in new[]{"", "This is not a monster.", modern+"\n"+legacy, new string('x',200001)}) {
            bool rejected=false;try{StatBlockParser.Parse(invalid);}catch(FormatException){rejected=true;}
            if(!rejected)throw new Exception("Invalid or multiple stat blocks accepted");
        }
    }
}
