using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace EncounterArchitect;

public static class BeyondUrlImporter
{
    public static Uri ValidateUrl(string value)
    {
        if(!Uri.TryCreate(value.Trim(),UriKind.Absolute,out var uri)||uri.Scheme!="https"||!uri.IsDefaultPort||uri.UserInfo.Length>0||
            !(uri.Host.Equals("www.dndbeyond.com",StringComparison.OrdinalIgnoreCase)||uri.Host.Equals("dndbeyond.com",StringComparison.OrdinalIgnoreCase))||
            !(uri.AbsolutePath.StartsWith("/monsters/",StringComparison.OrdinalIgnoreCase)||uri.AbsolutePath.StartsWith("/sources/",StringComparison.OrdinalIgnoreCase)))
            throw new FormatException("Enter an HTTPS D&D Beyond monster or sourcebook URL (www.dndbeyond.com/monsters/… or /sources/…).");
        return uri;
    }
    public static async Task<string> LoadAsync(Uri uri,CancellationToken cancellation=default)
    {
        ValidateUrl(uri.AbsoluteUri);
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellation);deadline.CancelAfter(TimeSpan.FromSeconds(30));cancellation=deadline.Token;
        using var handler=new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false,AutomaticDecompression=DecompressionMethods.All};
        using var client=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(25)};
        client.DefaultRequestHeaders.UserAgent.ParseAdd("EncounterArchitect/"+AppVersion.Current);
        var current=uri;
        for(int redirects=0;redirects<6;redirects++)
        {
            using var response=await client.GetAsync(current,HttpCompletionOption.ResponseHeadersRead,cancellation);
            if((int)response.StatusCode is >=300 and <400&&response.Headers.Location is {} location)
            {
                var next=new Uri(current,location);
                try{ValidateUrl(next.AbsoluteUri);}catch(FormatException){throw new FormatException("This is not a public monster page: D&D Beyond redirected to its store or sign-in. For owned sourcebooks or Google sign-in, use Open normal browser, sign in there, copy the complete monster stat block, and return to Paste clipboard. Other sign-in methods can try Embedded browser.");}
                current=next;continue;
            }
            if(!response.IsSuccessStatusCode)throw new FormatException($"D&D Beyond returned HTTP {(int)response.StatusCode}. Use Open normal browser, sign in if needed, and copy the complete stat block into Paste clipboard.");
            if(response.Content.Headers.ContentLength>8_000_000)throw new FormatException("This page is too large. Open it in the browser and read a single monster.");
            using var stream=await response.Content.ReadAsStreamAsync(cancellation);using var memory=new MemoryStream();var buffer=new byte[16384];int read;
            while((read=await stream.ReadAsync(buffer,cancellation))>0){if(memory.Length+read>8_000_000)throw new FormatException("This page is too large. Read a single monster in the browser instead.");memory.Write(buffer,0,read);}
            return ExtractHtml(Encoding.UTF8.GetString(memory.ToArray()),uri);
        }
        throw new FormatException("Too many redirects. Open the page in the browser and try Read this monster.");
    }
    static bool IsBlock(HtmlNode node)=>node.GetAttributeValue("class","").Split(' ',StringSplitOptions.RemoveEmptyEntries)
        .Any(c=>c is "mon-stat-block" or "mon-stat-block-2024" or "stat-block" or "statblock");
    public static string ExtractHtml(string html,Uri uri)
    {
        var doc=new HtmlAgilityPack.HtmlDocument();doc.LoadHtml(html);
        var blocks=doc.DocumentNode.Descendants().Where(IsBlock).Where(n=>!n.Ancestors().Any(IsBlock)).ToList();
        string fragment=Uri.UnescapeDataString(uri.Fragment.TrimStart('#'));
        if(fragment.Length>0)
        {
            var anchor=doc.DocumentNode.Descendants().FirstOrDefault(n=>n.GetAttributeValue("id","").Equals(fragment,StringComparison.OrdinalIgnoreCase)||n.GetAttributeValue("name","").Equals(fragment,StringComparison.OrdinalIgnoreCase));
            if(anchor==null)throw new FormatException($"The page did not contain the #{fragment} entry. It may require sign-in. Open the page, wait for it to load, then click Read this monster.");
            var containing=anchor.AncestorsAndSelf().FirstOrDefault(IsBlock);
            if(containing!=null)blocks=new(){containing};
            else
            {
                var heading=anchor.AncestorsAndSelf().FirstOrDefault(n=>Regex.IsMatch(n.Name,@"^h[1-6]$"));
                int level=heading==null?2:heading.Name[1]-'0';int start=heading?.StreamPosition??anchor.StreamPosition;
                var next=doc.DocumentNode.Descendants().FirstOrDefault(n=>n.StreamPosition>start&&Regex.IsMatch(n.Name,@"^h[1-6]$")&&n.Name[1]-'0'<=level);
                blocks=blocks.Where(n=>n.StreamPosition>=start&&(next==null||n.StreamPosition<next.StreamPosition)).ToList();
            }
        }
        if(blocks.Count==0)throw new FormatException("No monster stat block was found. This may be a species/race page, a sign-in page, or a page format the importer does not recognize. Open it in the browser or paste the complete stat block.");
        if(blocks.Count>1)throw new FormatException("This page has several monsters. Use a URL ending in the monster's #anchor, or open an individual /monsters/ page.");
        var result=new StringBuilder();WriteText(blocks[0],result);
        var text=Regex.Replace(WebUtility.HtmlDecode(result.ToString()),@"[\t \u00a0]+"," ");
        text=string.Join("\n",text.Split('\n').Select(l=>l.Trim()).Where(l=>l.Length>0));
        var parsed=StatBlockParser.Parse(text);
        if(parsed.HP==null||parsed.AC==null)throw new FormatException("The selected entry doesn't contain monster HP and AC. Use a full monster stat block, not playable-race traits.");
        return text;
    }
    static void WriteText(HtmlNode node,StringBuilder output)
    {
        if(node.Name is "script" or "style" or "noscript" or "input" or "button")return;
        if(node.GetAttributeValue("aria-hidden","")=="true"||node.Attributes["hidden"]!=null)return;
        if(node.NodeType==HtmlNodeType.Text){output.Append(Regex.Replace(node.InnerText,@"\s+"," "));return;}
        bool line=node.Name is "div" or "p" or "br" or "li" or "tr" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6";
        if(line)output.Append('\n');foreach(var child in node.ChildNodes)WriteText(child,output);
        if(line)output.Append('\n');else output.Append(' ');
    }
}
