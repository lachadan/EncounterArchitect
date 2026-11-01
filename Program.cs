namespace EncounterArchitect;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        if (Environment.GetCommandLineArgs().Contains("--self-test")) { ModelTests.Run(); return; }
        var args=Environment.GetCommandLineArgs();
        if(args.Contains("--check-browser")){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"browser-check.txt"),Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString());return;}
        int checkUrl=Array.IndexOf(args,"--check-url");
        if(checkUrl>=0&&checkUrl+1<args.Length){var uri=BeyondUrlImporter.ValidateUrl(args[checkUrl+1]);var text=BeyondUrlImporter.LoadAsync(uri).GetAwaiter().GetResult();var draft=StatBlockParser.Parse(text);File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"url-check.txt"),$"PASS: {draft.Name}, AC {draft.AC}, HP {draft.HP}, initiative {draft.Initiative}");return;}
        int dataOption=Array.IndexOf(args,"--data-dir");
        Application.Run(new MainWindow(dataOption>=0&&dataOption+1<args.Length?args[dataOption+1]:null));
    }    
}


