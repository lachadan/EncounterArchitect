using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EncounterArchitect;

public sealed class BeyondBrowserDialog : Form
{
    readonly WebView2 browser=new(){Dock=DockStyle.Fill};
    readonly Label message=new(){Dock=DockStyle.Fill,AutoSize=false};
    readonly Button read=new(){Text="Read this monster",AutoSize=true,Enabled=false};
    readonly Uri requested;
    readonly string profile;
    public string? StatText {get;private set;}
    public string? SourceUrl {get;private set;}

    public BeyondBrowserDialog(Uri uri,string profileDirectory)
    {
        requested=uri;profile=profileDirectory;Text=$"D&D Beyond browser · Encounter Architect v{AppVersion.Current}";
        Size=new Size(1150,800);MinimumSize=new Size(850,600);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(10)};
        layout.ColumnStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,70));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,44));
        message.Text="Sign in yourself if needed, then open the monster and click Read this monster.\nThis browser uses a separate local profile and may remember your sign-in. The app reads only the selected stat block.";
        layout.Controls.Add(message,0,0);layout.Controls.Add(browser,0,1);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill};buttons.Controls.Add(read);
        var go=new Button{Text="Return to requested entry",AutoSize=true};go.Click+=(_,_)=>{if(browser.CoreWebView2!=null)browser.CoreWebView2.Navigate(requested.AbsoluteUri);};buttons.Controls.Add(go);
        var external=new Button{Text="Open in default browser",AutoSize=true};external.Click+=(_,_)=>Process.Start(new ProcessStartInfo(requested.AbsoluteUri){UseShellExecute=true});buttons.Controls.Add(external);
        layout.Controls.Add(buttons,0,2);Controls.Add(layout);Shown+=async(_,_)=>await Initialize();read.Click+=async(_,_)=>await ReadMonster();
        FormClosed+=(_,_)=>browser.Dispose();
    }
    async Task Initialize()
    {
        try{
            var environment=await CoreWebView2Environment.CreateAsync(userDataFolder:profile);
            if(IsDisposed)return;await browser.EnsureCoreWebView2Async(environment);if(IsDisposed)return;
            browser.CoreWebView2.Settings.AreDevToolsEnabled=false;
            browser.CoreWebView2.NewWindowRequested+=(_,e)=>{e.Handled=true;if(Uri.TryCreate(e.Uri,UriKind.Absolute,out var next)&&next.Scheme=="https")browser.CoreWebView2.Navigate(next.AbsoluteUri);};
            browser.CoreWebView2.NavigationStarting+=(_,e)=>{if(Uri.TryCreate(e.Uri,UriKind.Absolute,out var next)&&next.Scheme!="https"&&next.Scheme!="about")e.Cancel=true;};
            browser.CoreWebView2.NavigationCompleted+=(_,_)=>read.Enabled=true;
            browser.CoreWebView2.Navigate(requested.AbsoluteUri);
        }catch(Exception e){if(!IsDisposed)message.Text="The embedded browser couldn't start. It requires Microsoft Edge WebView2 Runtime. You can still use URL loading or paste text from your normal browser.\n"+e.Message;}
    }
    async Task ReadMonster()
    {
        try{
            var current=BeyondUrlImporter.ValidateUrl(browser.Source?.AbsoluteUri??"");
            read.Enabled=false;
            // Only stat blocks and heading markers are read. No cookies, input fields,
            // script contents, account details, or browser profile data are extracted.
            const string script="""
            (() => {
              const selector='.mon-stat-block,.mon-stat-block-2024,.stat-block,.statblock';
              const roots=[...document.querySelectorAll(selector)].filter(n=>!n.parentElement?.closest(selector));
              const fragment=decodeURIComponent(location.hash.slice(1));
              let candidates=roots;
              if(fragment){
                const anchor=document.getElementById(fragment)||[...document.querySelectorAll('a[name]')].find(n=>n.name===fragment);
                if(!anchor)return '';
                const containing=anchor.closest(selector);
                if(containing)candidates=[containing];
                else {
                  const heading=anchor.closest('h1,h2,h3,h4,h5,h6')||anchor;
                  const level=/^H[1-6]$/.test(heading.tagName)?Number(heading.tagName[1]):2;
                  const following=n=>Boolean(heading.compareDocumentPosition(n)&Node.DOCUMENT_POSITION_FOLLOWING);
                  const end=[...document.querySelectorAll('h1,h2,h3,h4,h5,h6')].find(n=>following(n)&&Number(n.tagName[1])<=level);
                  candidates=roots.filter(n=>following(n)&&(!end||Boolean(n.compareDocumentPosition(end)&Node.DOCUMENT_POSITION_FOLLOWING)));
                }
              }
              if(candidates.length!==1)return '';
              const clone=candidates[0].cloneNode(true);
              clone.querySelectorAll('script,style,input,button,noscript,[hidden],[aria-hidden="true"]').forEach(n=>n.remove());
              return clone.outerHTML;
            })()
            """;
            var encoded=await browser.CoreWebView2.ExecuteScriptAsync(script);if(IsDisposed)return;
            var html=JsonSerializer.Deserialize<string>(encoded);
            if(string.IsNullOrWhiteSpace(html)){
                var selected=JsonSerializer.Deserialize<string>(await browser.CoreWebView2.ExecuteScriptAsync("window.getSelection().toString()"));
                if(IsDisposed)return;
                if(string.IsNullOrWhiteSpace(selected))throw new FormatException("No single monster was found at this entry. Finish signing in and return to the requested entry. You can also select the complete stat block's text in this browser, then click Read this monster again.");
                var parsed=StatBlockParser.Parse(selected);if(parsed.HP==null||parsed.AC==null)throw new FormatException("Select the complete monster stat block, including its name, size/type, AC, and HP.");StatText=selected;
            }else StatText=BeyondUrlImporter.ExtractHtml(html,new Uri(current.GetLeftPart(UriPartial.Path)));
            SourceUrl=current.AbsoluteUri;DialogResult=DialogResult.OK;Close();
        }catch(Exception e){if(!IsDisposed)message.Text=e.Message;}finally{if(!IsDisposed)read.Enabled=true;}
    }
}
