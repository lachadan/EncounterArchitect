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
        int dataOption=Array.IndexOf(args,"--data-dir");
        Application.Run(new MainWindow(dataOption>=0&&dataOption+1<args.Length?args[dataOption+1]:null));
    }    
}


