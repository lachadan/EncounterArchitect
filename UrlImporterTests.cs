namespace EncounterArchitect;

static class UrlImporterTests
{
    const string Block="<div class='mon-stat-block-2024'><div>Example Sentinel</div><div>Medium Construct, Unaligned</div><div><span>AC</span><span>17</span> <span>Initiative</span><span>+3 (13)</span></div><div>HP <span>40 (5d8 + 15)</span></div><div>CR 2 (XP 450)</div><h3>Actions</h3><p>Slam. <b>Melee Attack Roll:</b> +5. <span>Hit:</span> 8 damage.</p></div>";
    public static void Run()
    {
        if(!BrowserPopupHost.IsGoogleSignIn("https://accounts.google.com/o/oauth2/auth")||BrowserPopupHost.IsGoogleSignIn("https://accounts.google.com.evil.test/login"))throw new Exception("Google browser routing");
        if(!BrowserPopupHost.CanNavigate("about:blank")||!BrowserPopupHost.CanNavigate("https://accounts.example.com/login")||BrowserPopupHost.CanNavigate("file:///C:/test")||BrowserPopupHost.CanNavigate("javascript:alert(1)"))throw new Exception("Login popup navigation policy");
        var monster=BeyondUrlImporter.ValidateUrl("https://www.dndbeyond.com/monsters/123-example");
        var text=BeyondUrlImporter.ExtractHtml("<html><script>HP 99999</script>"+Block+"</html>",monster);
        var draft=StatBlockParser.Parse(text);
        if(draft.Name!="Example Sentinel"||draft.AC!=17||draft.HP!=40||draft.Initiative!=3||!text.Contains("Slam."))throw new Exception("HTML stat block extraction");
        var chapter=BeyondUrlImporter.ValidateUrl("https://www.dndbeyond.com/sources/dnd/example/bestiary#Sentinel");
        string first=Block.Replace("Example Sentinel","Other Monster");
        var selected=BeyondUrlImporter.ExtractHtml("<h2 id='Other'>Other</h2>"+first+"<h2><a id='Sentinel'></a>Sentinel</h2>"+Block+"<h2 id='After'>After</h2>"+first,chapter);
        if(StatBlockParser.Parse(selected).Name!="Example Sentinel")throw new Exception("Wrong anchored monster selected");
        var inside=Block.Replace("<div>Example Sentinel</div>","<h2 id='Sentinel'>Example Sentinel</h2>");
        if(StatBlockParser.Parse(BeyondUrlImporter.ExtractHtml(inside,chapter)).HP!=40)throw new Exception("Anchor inside stat block");
        foreach(string url in new[]{"http://www.dndbeyond.com/monsters/1","https://www.dndbeyond.com.evil.test/monsters/1","https://evil@www.dndbeyond.com/monsters/1","https://www.dndbeyond.com:444/monsters/1","file:///C:/test","https://www.dndbeyond.com/species/1"})
        {bool rejected=false;try{BeyondUrlImporter.ValidateUrl(url);}catch(FormatException){rejected=true;}if(!rejected)throw new Exception("Invalid URL accepted");}
        foreach(var sample in new[]{"<h2>Shop this book</h2>",Block+first,"<h2 id='Sentinel'>Playable race</h2><p>Flight traits, no monster stats.</p>"})
        {bool rejected=false;try{BeyondUrlImporter.ExtractHtml(sample,monster);}catch(FormatException){rejected=true;}if(!rejected)throw new Exception("Non-monster/ambiguous page accepted");}
        bool missing=false;try{BeyondUrlImporter.ExtractHtml(Block,chapter);}catch(FormatException){missing=true;}
        if(!missing)throw new Exception("Missing chapter anchor ignored");
    }
}
