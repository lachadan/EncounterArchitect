using System.ComponentModel;
using System.Text.Json;
using System.Reflection;

namespace EncounterArchitect;

public class Monster
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Custom";
    public string CR { get; set; } = "0";
    public int AC { get; set; } = 10;
    public int HP { get; set; } = 1;
    public int Initiative { get; set; }
    public string Stats { get; set; } = "";
    public override string ToString() => $"{Name}  ·  CR {CR}";
}
public class Combatant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Monster Monster { get; set; } = new();
    public string Name { get; set; } = "";
    public int Initiative { get; set; }
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int TempHP { get; set; }
    public int AC { get; set; }
    public string Notes { get; set; } = "";
    public void Damage(int n) { int absorbed = Math.Min(TempHP,n); TempHP-=absorbed; HP=Math.Max(0,HP-(n-absorbed)); }
    public void Heal(int n) => HP=(int)Math.Min(MaxHP,(long)HP+n);
}
public class Encounter
{
    public List<Combatant> Creatures { get; set; } = new();
    public int Round { get; set; } = 1;
    public Guid? Active { get; set; }
    public void SortByInitiative() => Creatures=Creatures.OrderByDescending(c=>c.Initiative).ToList();
    public void AdvanceTurn(){if(Creatures.Count==0)return;int i=Creatures.FindIndex(c=>c.Id==Active);if(i==Creatures.Count-1){Round++;i=-1;}Active=Creatures[i+1].Id;}
}
public class MainWindow : Form
{
    static readonly Color Background=Color.FromArgb(20,24,32), Surface=Color.FromArgb(30,36,47), Ink=Color.FromArgb(231,235,242), Accent=Color.FromArgb(199,151,78);
    readonly string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EncounterArchitect");
    readonly JsonSerializerOptions json=new(){WriteIndented=true,PropertyNameCaseInsensitive=true};
    List<Monster> monsters=new();
    Encounter encounter=new();
    readonly Stack<string> history=new();
    readonly ListBox library=new(){Dock=DockStyle.Fill,IntegralHeight=false};
    readonly TextBox search=new(){PlaceholderText="Search name, type, or CR…",Dock=DockStyle.Fill};
    readonly ComboBox cr=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    readonly RichTextBox stats=new(){Dock=DockStyle.Fill,ReadOnly=true,BorderStyle=BorderStyle.None,DetectUrls=true};
    readonly Label statTitle=new(){Text="Choose a monster",AutoSize=true,Font=new Font("Segoe UI",19,FontStyle.Bold)};
    readonly Label round=new(){AutoSize=true,Font=new Font("Segoe UI",13,FontStyle.Bold)};
    readonly Label status=new(){Dock=DockStyle.Bottom,Height=28,TextAlign=ContentAlignment.MiddleLeft};
    readonly DataGridView grid=new(){Dock=DockStyle.Fill,AutoGenerateColumns=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,BackgroundColor=Surface,BorderStyle=BorderStyle.None,AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells};
    readonly NumericUpDown amount=new(){Minimum=1,Maximum=1000000,Value=5,Width=75};
    readonly NumericUpDown quantity=new(){Minimum=1,Maximum=99,Value=1,Width=55};
    BindingList<Combatant> rows=new();
    bool refreshing;
    string? beforeEdit;
    public MainWindow(string? dataDirectory=null)
    {
        if(dataDirectory!=null)folder=Path.GetFullPath(dataDirectory);
        Text="Encounter Architect • 2024 Monster & Combat Tracker"; Width=1600; Height=950; MinimumSize=new Size(1180,700); StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Segoe UI",10); BackColor=Background; ForeColor=Ink;
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("EncounterArchitect.monsters.json")!;
        monsters=JsonSerializer.Deserialize<List<Monster>>(stream,json)!;
        Directory.CreateDirectory(folder);
        try { if(File.Exists(CustomPath)) foreach(var m in ReadMonsters(CustomPath)){monsters.RemoveAll(x=>x.Name.Equals(m.Name,StringComparison.OrdinalIgnoreCase));monsters.Add(m);} } catch(Exception e){MessageBox.Show("Custom library could not be loaded: "+e.Message);}
        try {if(File.Exists(AutoPath)) encounter=ReadEncounter(AutoPath);} catch(Exception e){MessageBox.Show("The saved encounter could not be loaded. The file has been preserved.\n"+e.Message);}
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Padding=new Padding(16)};
        root.ColumnStyles.Add(new(SizeType.Percent,23));root.ColumnStyles.Add(new(SizeType.Percent,35));root.ColumnStyles.Add(new(SizeType.Percent,42));
        Controls.Add(root);Controls.Add(status);
        var left=Pane("BESTIARY",out var leftBody);root.Controls.Add(left,0,0);
        leftBody.RowCount=6; leftBody.RowStyles.Add(new(SizeType.Absolute,38));leftBody.RowStyles.Add(new(SizeType.Absolute,38));leftBody.RowStyles.Add(new(SizeType.Percent,100));leftBody.RowStyles.Add(new(SizeType.Absolute,45));leftBody.RowStyles.Add(new(SizeType.Absolute,45));leftBody.RowStyles.Add(new(SizeType.Absolute,45));
        leftBody.Controls.Add(search,0,0);leftBody.Controls.Add(cr,0,1);leftBody.Controls.Add(library,0,2);
        var add=Flow();add.Controls.Add(quantity);add.Controls.Add(Button("+ Add to encounter",()=>AddSelected()));leftBody.Controls.Add(add,0,3);
        var custom=Flow();custom.Controls.Add(Button("Custom creature",Custom));custom.Controls.Add(Button("Import JSON",Import));leftBody.Controls.Add(custom,0,4);
        var paste=Button("Paste from D&D Beyond",PasteFromBeyond);paste.Dock=DockStyle.Fill;leftBody.Controls.Add(paste,0,5);
        cr.Items.Add("All challenge ratings");foreach(var v in monsters.Select(m=>m.CR).Distinct().OrderBy(CrValue))cr.Items.Add(v);cr.SelectedIndex=0;
        search.TextChanged+=(_,_)=>Filter();cr.SelectedIndexChanged+=(_,_)=>Filter();library.SelectedIndexChanged+=(_,_)=>ShowMonster(library.SelectedItem as Monster);library.DoubleClick+=(_,_)=>AddSelected();
        var middle=Pane("STAT BLOCK",out var midBody);root.Controls.Add(middle,1,0);midBody.RowCount=2;midBody.RowStyles.Add(new(SizeType.Absolute,65));midBody.RowStyles.Add(new(SizeType.Percent,100));midBody.Controls.Add(statTitle,0,0);midBody.Controls.Add(stats,0,1);
        var right=Pane("ENCOUNTER",out var body);root.Controls.Add(right,2,0);body.RowCount=6;
        body.RowStyles.Add(new(SizeType.Absolute,48));body.RowStyles.Add(new(SizeType.Absolute,48));body.RowStyles.Add(new(SizeType.Percent,100));body.RowStyles.Add(new(SizeType.Absolute,48));body.RowStyles.Add(new(SizeType.Absolute,48));body.RowStyles.Add(new(SizeType.Absolute,45));
        var file=Flow();file.Controls.Add(Button("New",NewEncounter));file.Controls.Add(Button("Open",Open));file.Controls.Add(Button("Save as…",Export));file.Controls.Add(Button("Undo",Undo));file.Controls.Add(Button("About",()=>MessageBox.Show("Encounter Architect 1.1 • Offline Windows x64\n\n330 SRD monsters for the 2024 rules. Not the complete Monster Manual.\n\nThis work includes material from the System Reference Document 5.2.1 (“SRD 5.2.1”) by Wizards of the Coast LLC, available at https://www.dndbeyond.com/srd. The SRD 5.2.1 is licensed under the Creative Commons Attribution 4.0 International License, available at https://creativecommons.org/licenses/by/4.0/legalcode.\n\nChanges: stat blocks extracted and reformatted for search and encounter tracking. Independent app; not affiliated with or endorsed by Wizards of the Coast.\n\nSaves: "+folder,"About Encounter Architect")));body.Controls.Add(file,0,0);
        var turns=Flow();turns.Controls.Add(round);turns.Controls.Add(Button("Roll initiative",Roll));turns.Controls.Add(Button("Sort",Sort));turns.Controls.Add(Button("Next turn →",Next));body.Controls.Add(turns,0,1);
        grid.ColumnHeadersHeight=44;grid.ColumnHeadersDefaultCellStyle.Padding=new Padding(3);grid.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        AddColumn("Name","Creature",130);AddColumn("Initiative","Init",46);AddColumn("HP","HP",48);AddColumn("MaxHP","Max",48);AddColumn("TempHP","Temp",48);AddColumn("AC","AC",42);AddColumn("Notes","Conditions / notes",150);
        body.Controls.Add(grid,0,2);grid.DataSource=rows;grid.DefaultCellStyle.BackColor=Surface;grid.DefaultCellStyle.ForeColor=Ink;grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(62,77,98);grid.DefaultCellStyle.SelectionForeColor=Color.White;grid.DefaultCellStyle.Padding=new Padding(3,8,3,8);grid.ColumnHeadersDefaultCellStyle.BackColor=Background;grid.ColumnHeadersDefaultCellStyle.ForeColor=Ink;grid.EnableHeadersVisualStyles=false;grid.GridColor=Color.FromArgb(53,62,76);grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionChanged+=(_,_)=>{if(!refreshing && Selected is {} c)ShowMonster(c.Monster);};
        grid.CellBeginEdit+=(_,_)=>beforeEdit=Snapshot();grid.CellValidating+=ValidateCell;grid.CellEndEdit+=(_,_)=>{if(beforeEdit!=null)history.Push(beforeEdit);beforeEdit=null;Save();PaintRows();};grid.DataError+=(_,e)=>{e.ThrowException=false;status.Text="Enter a valid whole number.";};
        var damage=Flow();damage.Controls.Add(new Label{Text="Amount",AutoSize=true,Padding=new Padding(0,7,0,0)});damage.Controls.Add(amount);damage.Controls.Add(Button("Damage",()=>Change(c=>c.Damage((int)amount.Value))));damage.Controls.Add(Button("Heal",()=>Change(c=>c.Heal((int)amount.Value))));damage.Controls.Add(Button("Set temp HP",()=>Change(c=>c.TempHP=(int)amount.Value)));body.Controls.Add(damage,0,3);
        var manage=Flow();manage.Controls.Add(Button("Duplicate",Duplicate));manage.Controls.Add(Button("Remove",Remove));manage.Controls.Add(Button("Restore HP",()=>Change(c=>{c.HP=c.MaxHP;c.TempHP=0;})));body.Controls.Add(manage,0,4);
        body.Controls.Add(new Label{Text="Edit cells for initiative, HP and notes. Gold = current turn.\n0 HP stays in the roster. Damage uses temporary HP first.",Dock=DockStyle.Fill,ForeColor=Color.Silver,Font=new Font("Segoe UI",9)},0,5);
        ApplyTheme(this);stats.BackColor=Surface;stats.Font=new Font("Consolas",11);stats.ForeColor=Ink;library.Font=new Font("Segoe UI",10);Filter();RefreshRows();
        FormClosing+=(_,_)=>{grid.EndEdit();Save();};
        var help=new ToolTip();help.SetToolTip(quantity,"Number of separate creatures to add");help.SetToolTip(grid,"Initiative ties keep the roster's existing order; you can edit initiative to break a tie.");
    }
    string AutoPath=>Path.Combine(folder,"encounter.json");string CustomPath=>Path.Combine(folder,"custom-monsters.json");
    Combatant? Selected=>grid.CurrentRow?.DataBoundItem as Combatant;
    static double CrValue(string s){var parts=s.Split('/');return parts.Length==2&&double.TryParse(parts[0],out var a)&&double.TryParse(parts[1],out var b)&&b!=0?a/b:double.TryParse(s,out var n)?n:999;}
    static FlowLayoutPanel Flow()=>new(){Dock=DockStyle.Fill,WrapContents=false,AutoScroll=true,Margin=new Padding(0)};
    static Button Button(string title,Action action){var b=new Button{Text=title,AutoSize=true,Height=33,FlatStyle=FlatStyle.Flat,Margin=new Padding(0,2,6,2),Padding=new Padding(5,1,5,1),Cursor=Cursors.Hand};b.Click+=(_,_)=>action();return b;}
    static Control Pane(string title,out TableLayoutPanel body){var p=new Panel{Dock=DockStyle.Fill,Padding=new Padding(12),Margin=new Padding(0,0,10,0),BackColor=Surface};body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1};body.ColumnStyles.Add(new(SizeType.Percent,100));p.Controls.Add(body);p.Controls.Add(new Label{Text=title,Dock=DockStyle.Top,Height=38,ForeColor=Accent,Font=new Font("Segoe UI",11,FontStyle.Bold)});return p;}
    static void ApplyTheme(Control c){foreach(Control x in c.Controls){if(x is Button){x.BackColor=Color.FromArgb(49,59,75);x.ForeColor=Ink;}else if(x is TextBox or ListBox or ComboBox or NumericUpDown){x.BackColor=Background;x.ForeColor=Ink;}ApplyTheme(x);}}
    void AddColumn(string property,string title,int width)=>grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=property,HeaderText=title,FillWeight=width,MinimumWidth=property=="Name"?100:property=="Notes"?100:42,SortMode=DataGridViewColumnSortMode.NotSortable});
    void Filter(){var q=search.Text.Trim();library.BeginUpdate();library.Items.Clear();foreach(var m in monsters.Where(m=>(cr.SelectedIndex<=0||m.CR==(string)cr.SelectedItem!)&&($"{m.Name} {m.Type} CR {m.CR}").Contains(q,StringComparison.OrdinalIgnoreCase)).OrderBy(m=>m.Name))library.Items.Add(m);library.EndUpdate();if(library.Items.Count>0)library.SelectedIndex=0;else{statTitle.Text="No matches";stats.Text="Try a different name, creature type, or challenge rating.";}}
    void ShowMonster(Monster? m){if(m==null)return;statTitle.Text=m.Name;stats.Text=m.Stats;foreach(var heading in new[]{"Traits","Actions","Bonus Actions","Reactions","Legendary Actions"}){int start=0;while((start=stats.Text.IndexOf(heading,start,StringComparison.Ordinal))>=0){stats.Select(start,heading.Length);stats.SelectionColor=Accent;stats.SelectionFont=new Font(stats.Font,FontStyle.Bold);start+=heading.Length;}}stats.Select(0,0);stats.ScrollToCaret();}
    string Snapshot()=>JsonSerializer.Serialize(encounter,json);
    void Remember(){grid.EndEdit();history.Push(Snapshot());if(history.Count>100){var keep=history.Take(100).Reverse().ToArray();history.Clear();foreach(var s in keep)history.Push(s);}}
    void Save(){try{var temp=AutoPath+".tmp";File.WriteAllText(temp,Snapshot());File.Move(temp,AutoPath,true);status.Text=$"  {monsters.Count} monsters · {encounter.Creatures.Count} combatants · Autosaved {DateTime.Now:t}";}catch(Exception e){status.Text="  Save failed: "+e.Message;}}
    void RefreshRows(Guid? select=null){refreshing=true;rows=new BindingList<Combatant>(encounter.Creatures);grid.DataSource=rows;round.Text=$"Round {encounter.Round}";if(select is {} id){foreach(DataGridViewRow r in grid.Rows)if(r.DataBoundItem is Combatant bound&&bound.Id==id){grid.CurrentCell=r.Cells[0];break;}}refreshing=false;PaintRows();if(Selected is {} c)ShowMonster(c.Monster);}
    void PaintRows(){foreach(DataGridViewRow r in grid.Rows){if(r.DataBoundItem is not Combatant c)continue;r.DefaultCellStyle.ForeColor=c.HP==0?Color.FromArgb(244,132,132):Ink;r.DefaultCellStyle.BackColor=c.Id==encounter.Active?Color.FromArgb(89,67,36):Surface;}round.Text=$"Round {encounter.Round}";}
    void AddSelected(){if(library.SelectedItem is not Monster m)return;Remember();Guid? last=null;for(int i=0;i<quantity.Value;i++){var c=Make(m);encounter.Creatures.Add(c);last=c.Id;}RefreshRows(last);Save();}
    Combatant Make(Monster m){int number=1;while(encounter.Creatures.Any(c=>c.Name==$"{m.Name} {number}"))number++;return new(){Monster=m,Name=$"{m.Name} {number}",HP=m.HP,MaxHP=m.HP,AC=m.AC,Initiative=m.Initiative};}
    void Change(Action<Combatant> action){if(Selected is not {} c)return;Remember();action(c);RefreshRows(c.Id);Save();}
    void Duplicate(){if(Selected is not {} c)return;Remember();var copy=Make(c.Monster);encounter.Creatures.Add(copy);RefreshRows(copy.Id);Save();}
    void Remove(){if(Selected is not {} c)return;Remember();encounter.Creatures.Remove(c);if(encounter.Active==c.Id)encounter.Active=null;RefreshRows();Save();}
    void Roll(){if(rows.Count==0)return;Remember();foreach(var c in encounter.Creatures)c.Initiative=Random.Shared.Next(1,21)+c.Monster.Initiative;encounter.Active=null;encounter.Round=1;SortCore();RefreshRows();Save();}
    void SortCore()=>encounter.SortByInitiative();
    void Sort(){Remember();var id=Selected?.Id;SortCore();RefreshRows(id);Save();}
    void Next(){if(rows.Count==0)return;Remember();encounter.AdvanceTurn();RefreshRows(encounter.Active);Save();}
    void Undo(){if(history.Count==0)return;encounter=JsonSerializer.Deserialize<Encounter>(history.Pop(),json)!;RefreshRows();Save();}
    void ValidateCell(object? sender,DataGridViewCellValidatingEventArgs e){var p=grid.Columns[e.ColumnIndex].DataPropertyName;string text=e.FormattedValue?.ToString()??"";if(p=="Name"&&string.IsNullOrWhiteSpace(text)){e.Cancel=true;status.Text="Creature name cannot be empty.";return;}if(p is "Initiative" or "HP" or "MaxHP" or "TempHP" or "AC"){if(!int.TryParse(text,out var n)||n>1000000||n<(p=="Initiative"?-1000:p=="MaxHP"?1:0)){e.Cancel=true;status.Text="Enter a whole number in range (HP ≥ 0, max HP ≥ 1).";}else if(Selected is {} c&&((p=="HP"&&n>c.MaxHP)||(p=="MaxHP"&&n<c.HP))){e.Cancel=true;status.Text="HP cannot exceed max HP. Adjust HP or max HP first.";}}}
    void NewEncounter(){if(rows.Count>0&&MessageBox.Show("Clear this encounter? Undo can restore it.","New encounter",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;Remember();encounter=new();RefreshRows();Save();}
    void Export(){using var d=new SaveFileDialog{Filter="Encounter JSON|*.json",FileName="encounter.json"};if(d.ShowDialog()!=DialogResult.OK)return;try{grid.EndEdit();File.WriteAllText(d.FileName,Snapshot());status.Text="Encounter saved.";}catch(Exception e){MessageBox.Show(e.Message,"Could not save");}}
    void Open(){using var d=new OpenFileDialog{Filter="Encounter JSON|*.json"};if(d.ShowDialog()!=DialogResult.OK)return;try{var loaded=ReadEncounter(d.FileName);Remember();encounter=loaded;RefreshRows();Save();}catch(Exception e){MessageBox.Show(e.Message,"Could not open encounter");}}
    Encounter ReadEncounter(string path){var e=JsonSerializer.Deserialize<Encounter>(File.ReadAllText(path),json)??throw new Exception("Empty encounter.");if(e.Creatures==null||e.Round<1||e.Creatures.Any(c=>c==null||c.Monster==null||c.MaxHP<1||c.HP<0||c.HP>c.MaxHP||c.TempHP<0)||e.Creatures.Select(c=>c.Id).Distinct().Count()!=e.Creatures.Count)throw new Exception("Invalid encounter data.");return e;}
    List<Monster> ReadMonsters(string path){var m=JsonSerializer.Deserialize<List<Monster>>(File.ReadAllText(path),json)??throw new Exception("Expected an array of monsters.");if(m.Any(x=>x==null||string.IsNullOrWhiteSpace(x.Name)||x.HP<1||x.HP>1000000||x.AC<0||x.Stats==null))throw new Exception("Each monster needs a name, positive HP, nonnegative AC, and Stats text.");return m;}
    void PasteFromBeyond()
    {
        using var dialog=new PasteMonsterDialog();
        if(dialog.ShowDialog(this)!=DialogResult.OK||dialog.Monster is not {} m)return;
        if(monsters.Any(x=>x.Name.Equals(m.Name,StringComparison.OrdinalIgnoreCase))&&
            MessageBox.Show($"Replace '{m.Name}' in your library? Existing encounter creatures keep their current stats.","Monster already exists",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return;
        try {
            var custom=File.Exists(CustomPath)?ReadMonsters(CustomPath):new();
            custom.RemoveAll(x=>x.Name.Equals(m.Name,StringComparison.OrdinalIgnoreCase));custom.Add(m);
            var temp=CustomPath+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(custom,json));File.Move(temp,CustomPath,true);
            monsters.RemoveAll(x=>x.Name.Equals(m.Name,StringComparison.OrdinalIgnoreCase));monsters.Add(m);
            var values=monsters.Select(x=>x.CR).Distinct().OrderBy(CrValue).ToArray();
            cr.Items.Clear();cr.Items.Add("All challenge ratings");cr.Items.AddRange(values);cr.SelectedIndex=0;
            search.Text=m.Name;Filter();library.SelectedItem=m;Save();
            status.Text=$"Saved {m.Name} to your local monster library.";
        } catch(Exception e){MessageBox.Show(e.Message,"Could not save monster");}
    }
    void Import(){using var d=new OpenFileDialog{Filter="Monster JSON|*.json"};if(d.ShowDialog()!=DialogResult.OK)return;try{var incoming=ReadMonsters(d.FileName);var custom=File.Exists(CustomPath)?ReadMonsters(CustomPath):new();foreach(var m in incoming){custom.RemoveAll(x=>x.Name.Equals(m.Name,StringComparison.OrdinalIgnoreCase));custom.Add(m);}File.WriteAllText(CustomPath,JsonSerializer.Serialize(custom,json));foreach(var m in incoming){monsters.RemoveAll(x=>x.Name.Equals(m.Name,StringComparison.OrdinalIgnoreCase));monsters.Add(m);}Filter();Save();MessageBox.Show($"Imported {incoming.Count} monsters.");}catch(Exception e){MessageBox.Show(e.Message,"Import failed");}}
    void Custom(){using var f=new Form{Text="Custom creature / player character",Width=600,Height=650,StartPosition=FormStartPosition.CenterParent,BackColor=Surface,ForeColor=Ink};var p=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=2,RowCount=7};p.ColumnStyles.Add(new(SizeType.Absolute,110));p.ColumnStyles.Add(new(SizeType.Percent,100));var name=new TextBox{Dock=DockStyle.Fill};var hp=new NumericUpDown{Minimum=1,Maximum=1000000,Value=10};var ac=new NumericUpDown{Maximum=1000,Value=10};var init=new NumericUpDown{Minimum=-100,Maximum=100,Value=0};var text=new TextBox{Multiline=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical};Control[] inputs={name,hp,ac,init,text};string[] labels={"Name","Max HP","AC","Initiative bonus","Stat block / notes"};for(int i=0;i<inputs.Length;i++){p.RowStyles.Add(new(i==4?SizeType.Percent:SizeType.Absolute,i==4?100:42));p.Controls.Add(new Label{Text=labels[i],AutoSize=true},0,i);p.Controls.Add(inputs[i],1,i);}p.RowStyles.Add(new(SizeType.Absolute,44));var save=Button("Save to library",()=>{if(string.IsNullOrWhiteSpace(name.Text))return;try{var m=new Monster{Name=name.Text.Trim(),HP=(int)hp.Value,AC=(int)ac.Value,Initiative=(int)init.Value,Stats=text.Text,Type="Custom"};if(monsters.Any(x=>x.Name.Equals(m.Name,StringComparison.OrdinalIgnoreCase))){MessageBox.Show("Choose a unique creature name.");return;}var custom=File.Exists(CustomPath)?ReadMonsters(CustomPath):new();custom.Add(m);File.WriteAllText(CustomPath,JsonSerializer.Serialize(custom,json));monsters.Add(m);Filter();library.SelectedItem=m;f.Close();Save();}catch(Exception e){MessageBox.Show(e.Message);}});p.Controls.Add(save,1,5);f.Controls.Add(p);ApplyTheme(f);f.ShowDialog(this);}
}
static class ModelTests
{
    public static void Run()
    {
        var c=new Combatant{HP=20,MaxHP=20,TempHP=5};c.Damage(8);
        if(c.HP!=17||c.TempHP!=0)throw new Exception("Temporary HP damage");
        c.Damage(100);if(c.HP!=0)throw new Exception("HP floor");
        c.Heal(100);if(c.HP!=20)throw new Exception("Healing cap");
        var e=new Encounter{Creatures=new(){c},Active=c.Id,Round=3};
        var back=JsonSerializer.Deserialize<Encounter>(JsonSerializer.Serialize(e))!;
        if(back.Round!=3||back.Active!=back.Creatures[0].Id||back.Creatures[0].HP!=20)throw new Exception("Persistence");
        var a=new Combatant{Initiative=15};var b=new Combatant{Initiative=15};
        c.Initiative=2;e=new Encounter{Creatures=new(){c,a,b}};e.SortByInitiative();
        if(e.Creatures[0]!=a||e.Creatures[1]!=b)throw new Exception("Stable initiative ties");
        e.AdvanceTurn();if(e.Active!=a.Id||e.Round!=1)throw new Exception("First turn");
        e.AdvanceTurn();e.AdvanceTurn();e.AdvanceTurn();
        if(e.Active!=a.Id||e.Round!=2)throw new Exception("Round wrap");
        ParserTests.Run();
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"self-test.txt"),"PASS: temporary HP, damage floor, healing cap, persistence, initiative ties, first turn, round wrap; parser: 330 SRD blocks, multiline labels, legacy fields, missing values, multiple blocks, invalid input");
    }
}
