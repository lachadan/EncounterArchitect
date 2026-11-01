namespace EncounterArchitect;

public sealed class PasteMonsterDialog : Form
{
    readonly TextBox source = new() { Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Font = new Font("Consolas", 10) };
    readonly TextBox name = new(), type = new(), ac = new(), hp = new(), initiative = new(), cr = new();
    readonly TextBox warnings = new() { Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    readonly Button save=new();
    readonly TextBox url=new(){Dock=DockStyle.Fill,PlaceholderText="https://www.dndbeyond.com/monsters/… or /sources/…#Monster"};
    readonly CancellationTokenSource lifetime=new();
    readonly string browserProfile;
    string importedUrl="";
    public Monster? Monster { get; private set; }

    public PasteMonsterDialog(string? browserProfileDirectory=null)
    {
        browserProfile=browserProfileDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EncounterArchitect","BeyondBrowser");
        Text = $"Import from D&D Beyond · v{AppVersion.Current}";
        Size = new Size(1100, 820); MinimumSize = new Size(960, 720);
        StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(30, 36, 47); ForeColor = Color.FromArgb(231, 235, 242);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.Absolute, 64)); layout.RowStyles.Add(new(SizeType.Absolute, 44));
        layout.RowStyles.Add(new(SizeType.Absolute, 44));
        layout.RowStyles.Add(new(SizeType.Percent, 100)); layout.RowStyles.Add(new(SizeType.Absolute, 48));
        layout.Controls.Add(new Label { Text = "Load URL is for public monster pages. For Google sign-in or owned books, use Open normal browser, then copy/paste.\nThe embedded browser is optional for other sign-in methods. Review all detected values before saving.", Dock = DockStyle.Fill }, 0, 0);
        var urlRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1};urlRow.ColumnStyles.Add(new(SizeType.Percent,100));urlRow.ColumnStyles.Add(new(SizeType.Absolute,100));urlRow.ColumnStyles.Add(new(SizeType.Absolute,175));urlRow.ColumnStyles.Add(new(SizeType.Absolute,170));
        Button load=null!,browse=null!;load=MakeButton("Load URL",async()=>await LoadUrl());browse=MakeButton("Embedded browser",OpenBrowser);var normal=MakeButton("Open normal browser",()=>{try{var uri=BeyondUrlImporter.ValidateUrl(url.Text);System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri){UseShellExecute=true});warnings.Text="Sign in in your normal browser (including Google), open the monster, and copy its complete stat block. Return here and click Paste clipboard. Browser sign-in does not transfer to Load URL or the embedded browser.";}catch(Exception e){warnings.Text=e.Message;}});urlRow.Controls.Add(url,0,0);urlRow.Controls.Add(load,1,0);urlRow.Controls.Add(normal,2,0);urlRow.Controls.Add(browse,3,0);layout.Controls.Add(urlRow,0,1);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        toolbar.Controls.Add(MakeButton("Paste clipboard", () => {
            try { if (!Clipboard.ContainsText()) { warnings.Text = "The clipboard contains no text."; return; } source.Text = Clipboard.GetText(); Preview(); }
            catch (Exception e) { warnings.Text = "Couldn't read the clipboard. Use Ctrl+V in the text box instead.\r\n" + e.Message; }
        }));
        toolbar.Controls.Add(MakeButton("Preview detected stats", Preview)); layout.Controls.Add(toolbar, 0, 2);
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        content.ColumnStyles.Add(new(SizeType.Percent, 57)); content.ColumnStyles.Add(new(SizeType.Percent, 43));
        content.Controls.Add(source, 0, 0);
        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8, Padding = new Padding(12, 0, 0, 0) };
        fields.ColumnStyles.Add(new(SizeType.Absolute, 115)); fields.ColumnStyles.Add(new(SizeType.Percent, 100));
        TextBox[] inputs = [name, type, ac, hp, initiative, cr];
        string[] labels = ["Name", "Size / type", "AC", "Max HP", "Initiative bonus", "CR"];
        for (int i = 0; i < inputs.Length; i++) { fields.RowStyles.Add(new(SizeType.Absolute, 42)); inputs[i].Dock = DockStyle.Fill; fields.Controls.Add(new Label { Text = labels[i], AutoSize = true, Padding = new Padding(0, 4, 0, 0) }, 0, i); fields.Controls.Add(inputs[i], 1, i); }
        fields.RowStyles.Add(new(SizeType.Absolute, 35)); fields.RowStyles.Add(new(SizeType.Percent, 100));
        fields.Controls.Add(new Label { Text = "Review before saving", AutoSize = true, ForeColor = Color.FromArgb(220, 179, 112) }, 0, 6); fields.SetColumnSpan(fields.GetControlFromPosition(0, 6)!, 2);
        fields.Controls.Add(warnings, 0, 7); fields.SetColumnSpan(warnings, 2);
        content.Controls.Add(fields, 1, 0); layout.Controls.Add(content, 0, 3);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(MakeButton("Cancel", () => { DialogResult = DialogResult.Cancel; Close(); }));
        save = MakeButton("Save to library", SaveMonster); save.Enabled = false; buttons.Controls.Add(save); layout.Controls.Add(buttons, 0, 4);
        Controls.Add(layout); Theme(this);
        warnings.Text = "Load a URL or paste one complete stat block. URL loading contacts D&D Beyond; imported monster data stays on your computer. The optional browser lets you sign in yourself.";
        url.TextChanged+=(_,_)=>importedUrl="";
        FormClosing+=(_,_)=>lifetime.Cancel();FormClosed+=(_,_)=>lifetime.Dispose();
        async Task LoadUrl()
        {
            try{var uri=BeyondUrlImporter.ValidateUrl(url.Text);load.Enabled=false;browse.Enabled=false;url.Enabled=false;save.Enabled=false;warnings.Text="Loading page…";
                var text=await BeyondUrlImporter.LoadAsync(uri,lifetime.Token);if(IsDisposed)return;source.Text=text;importedUrl=uri.AbsoluteUri;Preview();
            }catch(OperationCanceledException){if(!IsDisposed)warnings.Text="The page request timed out or was cancelled. Try Sign in / open page, or paste the stat block.";}
            catch(Exception e){if(!IsDisposed)warnings.Text=e.Message;}
            finally{if(!IsDisposed){load.Enabled=true;browse.Enabled=true;url.Enabled=true;}}
        }
        void OpenBrowser()
        {
            try{var uri=BeyondUrlImporter.ValidateUrl(url.Text);using var browser=new BeyondBrowserDialog(uri,browserProfile);
                if(browser.ShowDialog(this)==DialogResult.OK&&browser.StatText is {} text){source.Text=text;url.Text=browser.SourceUrl??uri.AbsoluteUri;importedUrl=url.Text;Preview();}
            }catch(Exception e){warnings.Text=e.Message;}
        }
        source.TextChanged += (_, _) => { save.Enabled = false; warnings.Text = "Text changed. Click Preview detected stats to refresh the fields before saving."; };
    }

    Button MakeButton(string label, Action action) { var b = new Button { Text = label, AutoSize = true, FlatStyle = FlatStyle.Flat, Padding = new Padding(6, 3, 6, 3) }; b.Click += (_, _) => action(); return b; }
    void Theme(Control parent) { foreach (Control c in parent.Controls) { if (c is TextBox) { c.BackColor = Color.FromArgb(20, 24, 32); c.ForeColor = ForeColor; } else if (c is Button) { c.BackColor = Color.FromArgb(49, 59, 75); c.ForeColor = ForeColor; } Theme(c); } }
    void Preview()
    {
        try {
            var draft = StatBlockParser.Parse(source.Text);
            name.Text = draft.Name; type.Text = draft.Type; ac.Text = draft.AC?.ToString() ?? ""; hp.Text = draft.HP?.ToString() ?? "";
            initiative.Text = draft.Initiative?.ToString() ?? ""; cr.Text = draft.CR;
            warnings.Text = string.Join("\r\n\r\n", draft.Warnings); save.Enabled = true;
        } catch (Exception e) when (e is FormatException or System.Text.RegularExpressions.RegexMatchTimeoutException) { save.Enabled = false; warnings.Text = e.Message; }
    }
    void SaveMonster()
    {
        if (string.IsNullOrWhiteSpace(name.Text) || string.IsNullOrWhiteSpace(type.Text)) { warnings.Text = "Enter a name and creature type."; return; }
        if (!int.TryParse(ac.Text, out var armor) || armor < 0 || armor > 1000 ||
            !int.TryParse(hp.Text, out var health) || health < 1 || health > 1_000_000 ||
            !int.TryParse(initiative.Text, out var bonus) || bonus < -100 || bonus > 100) {
            warnings.Text = "Enter whole numbers: AC 0-1000, HP 1-1,000,000, initiative bonus -100 to +100. Use the bonus, not the passive initiative score."; return;
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(cr.Text.Trim(), @"^(?:\?|0|1/8|1/4|1/2|[1-9]|[12][0-9]|30)$")) { warnings.Text = "CR must be ?, 0, 1/8, 1/4, 1/2, or a whole number from 1 to 30."; return; }
        Monster = new() { Name = name.Text.Trim(), Type = type.Text.Trim(), AC = armor, HP = health, Initiative = bonus, CR = cr.Text.Trim(), Stats = source.Text.Trim(), SourceUrl=importedUrl };
        DialogResult = DialogResult.OK; Close();
    }
}
