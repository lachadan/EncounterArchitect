using System.ComponentModel;
using System.Reflection;

namespace EncounterArchitect;

public static class AppVersion
{
    public static string Current { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";
}

public sealed record UndoState(string EncounterJson, int LogCount);

public sealed class SessionEntry
{
    public DateTime Time { get; init; } = DateTime.Now;
    public string TimeDisplay => Time.ToString("HH:mm:ss");
    public int Round { get; init; }
    public string Character { get; init; } = "Unspecified";
    public string Target { get; init; } = "";
    public string Kind { get; init; } = "";
    public int Amount { get; init; }
    public int BeforeHP { get; init; }
    public int AfterHP { get; init; }
    public int BeforeTemp { get; init; }
    public int AfterTemp { get; init; }
    public int HPLost => Math.Max(0, BeforeHP - AfterHP);
    public int TempAbsorbed => Kind == "Damage" ? Math.Max(0, BeforeTemp - AfterTemp) : 0;
    public string HPChange => $"{BeforeHP} → {AfterHP}";
    public string TempChange => $"{BeforeTemp} → {AfterTemp}";
    public bool Reversed { get; set; }
    public string State => Reversed ? "Reversed" : "Applied";
}

// Deliberately separate from Encounter: this log never enters autosaves or exports.
public sealed class SessionHistory
{
    public BindingList<SessionEntry> Entries { get; } = new();
    public event Action? Changed;
    public void Record(Combatant target, int round, string character, string kind, int amount, int beforeHP, int beforeTemp)
    {
        Entries.Add(new() { Round=round, Character=string.IsNullOrWhiteSpace(character)?"Unspecified":character.Trim(),
            Target=target.Name, Kind=kind, Amount=amount, BeforeHP=beforeHP, AfterHP=target.HP, BeforeTemp=beforeTemp, AfterTemp=target.TempHP });
        Changed?.Invoke();
    }
    public void ReverseFrom(int index)
    {
        for(int i=Math.Max(0,index);i<Entries.Count;i++)Entries[i].Reversed=true;
        Entries.ResetBindings();Changed?.Invoke();
    }
    public string Totals()
    {
        var totals=Entries.Where(e=>!e.Reversed&&e.Kind=="Damage")
            .GroupBy(e=>e.Character,StringComparer.OrdinalIgnoreCase)
            .Select(g=>new { Name=g.Key, Rolled=g.Sum(e=>(long)e.Amount), Lost=g.Sum(e=>(long)e.HPLost), Temp=g.Sum(e=>(long)e.TempAbsorbed) })
            .OrderByDescending(g=>g.Rolled).ToList();
        return totals.Count==0?"No damage recorded this session.":string.Join(Environment.NewLine,
            totals.Select(g=>$"{g.Name}: {g.Rolled:N0} damage entered · {g.Lost:N0} HP lost · {g.Temp:N0} temporary HP absorbed"));
    }
}

public sealed class SessionHistoryWindow : Form
{
    readonly SessionHistory session;
    readonly TextBox totals=new(){Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None};
    public SessionHistoryWindow(SessionHistory history)
    {
        session=history;Text=$"Session History · Encounter Architect v{AppVersion.Current}";
        Size=new Size(1150,650);MinimumSize=new Size(850,480);StartPosition=FormStartPosition.CenterParent;
        Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(30,36,47);ForeColor=Color.FromArgb(231,235,242);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=1,RowCount=4};
        layout.ColumnStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,55));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,30));layout.RowStyles.Add(new(SizeType.Absolute,110));
        layout.Controls.Add(new Label{Dock=DockStyle.Fill,Text="Current application session only — clears when you exit.\nDamage entered can exceed HP actually lost. Reversed entries are excluded from totals."},0,0);
        var grid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,BackgroundColor=BackColor,BorderStyle=BorderStyle.None,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,DataSource=session.Entries};
        void Column(string property,string title,int width){grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=property,HeaderText=title,FillWeight=width,MinimumWidth=55,SortMode=DataGridViewColumnSortMode.NotSortable});}
        Column("TimeDisplay","Time",70);Column("Round","Round",55);Column("Character","Character",120);Column("Target","Target",160);Column("Kind","Event",100);Column("Amount","Entered",65);Column("HPChange","HP",85);Column("TempChange","Temp HP",85);Column("State","Status",80);
        grid.DefaultCellStyle.BackColor=BackColor;grid.DefaultCellStyle.ForeColor=ForeColor;grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(62,77,98);grid.DefaultCellStyle.SelectionForeColor=Color.White;grid.DefaultCellStyle.Padding=new Padding(3,7,3,7);grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells;
        grid.EnableHeadersVisualStyles=false;grid.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(20,24,32);grid.ColumnHeadersDefaultCellStyle.ForeColor=ForeColor;grid.ColumnHeadersDefaultCellStyle.Padding=new Padding(3);grid.ColumnHeadersHeight=42;
        layout.Controls.Add(grid,0,1);layout.Controls.Add(new Label{Text="DAMAGE TOTALS BY CHARACTER",ForeColor=Color.FromArgb(220,179,112),Dock=DockStyle.Fill},0,2);
        totals.BackColor=BackColor;totals.ForeColor=ForeColor;layout.Controls.Add(totals,0,3);Controls.Add(layout);
        session.Changed+=UpdateTotals;FormClosed+=(_,_)=>session.Changed-=UpdateTotals;UpdateTotals();
    }
    void UpdateTotals()=>totals.Text=session.Totals();
}
