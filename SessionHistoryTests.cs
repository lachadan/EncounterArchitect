using System.Text.Json;

namespace EncounterArchitect;

static class SessionHistoryTests
{
    public static void Run()
    {
        var session=new SessionHistory();var target=new Combatant{Name="Goblin 1",HP=10,MaxHP=10,TempHP=3};
        target.Damage(8);session.Record(target,2," Aria ","Damage",8,10,3);
        var e=session.Entries[0];
        if(e.Character!="Aria"||e.Target!="Goblin 1"||e.HPLost!=5||e.TempAbsorbed!=3||e.AfterHP!=5||e.Round!=2)throw new Exception("Damage history attribution/HP/temp");
        target.Name="Renamed";if(e.Target!="Goblin 1")throw new Exception("Historical name changed");
        target.Damage(12);session.Record(target,2,"aria","Damage",12,5,0);
        if(!session.Totals().Contains("20 damage entered · 10 HP lost · 3 temporary HP absorbed"))throw new Exception("Overkill totals/case grouping");
        session.ReverseFrom(1);
        if(!session.Entries[1].Reversed||session.Entries[0].Reversed||!session.Totals().Contains("8 damage entered · 5 HP lost"))throw new Exception("Undo damage totals");
        target.Heal(4);session.Record(target,2,"Thorne","Healing",4,0,0);
        if(session.Totals().Contains("Thorne"))throw new Exception("Healing included in damage totals");
        session.Record(target,2," ","Adjustment",0,4,0);
        if(session.Entries[^1].Character!="Unspecified")throw new Exception("Unspecified source");
        session.ReverseFrom(0);if(session.Totals()!="No damage recorded this session.")throw new Exception("All reversed totals");
        if(new SessionHistory().Entries.Count!=0)throw new Exception("New session not empty");
        string save=JsonSerializer.Serialize(new Encounter{Creatures=new(){target}});
        if(save.Contains("Aria")||save.Contains("SessionEntry")||save.Contains("LogCount"))throw new Exception("Session log leaked to encounter save");
        if(AppVersion.Current!="1.3.0")throw new Exception("Build version mismatch");
    }
}
