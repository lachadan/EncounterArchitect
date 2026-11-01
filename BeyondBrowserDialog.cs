using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EncounterArchitect;

public sealed class BeyondBrowserDialog : Form
{
    readonly WebView2 browser=new(){Dock=DockStyle.Fill};
    readonly Label message=new(){Dock=DockStyle.Fill,AutoSize=false};
    readonly Label address=new(){Dock=DockStyle.Bottom,Height=25,AutoEllipsis=true};
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
        layout.ColumnStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,105));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,44));
        message.Text="Google sign-in needs Open in default browser, followed by copy/paste import. Other sign-in methods may work here.\nThis browser has a separate local profile. If you can open the entry here, click Read this monster.";
        layout.Controls.Add(message,0,0);layout.Controls.Add(browser,0,1);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill};buttons.Controls.Add(read);
        var go=new Button{Text="Return to requested entry",AutoSize=true};go.Click+=(_,_)=>{if(browser.CoreWebView2!=null)browser.CoreWebView2.Navigate(requested.AbsoluteUri);};buttons.Controls.Add(go);
        var external=new Button{Text="Open in default browser",AutoSize=true};external.Click+=(_,_)=>Process.Start(new ProcessStartInfo(requested.AbsoluteUri){UseShellExecute=true});buttons.Controls.Add(external);
        var reload=new Button{Text="Reload",AutoSize=true};reload.Click+=(_,_)=>browser.CoreWebView2?.Reload();buttons.Controls.Add(reload);
        var back=new Button{Text="Back",AutoSize=true};back.Click+=(_,_)=>{if(browser.CanGoBack)browser.GoBack();};buttons.Controls.Add(back);
        layout.Controls.Add(buttons,0,2);Controls.Add(layout);Shown+=async(_,_)=>await Initialize();read.Click+=async(_,_)=>await ReadMonster();
        Controls.Add(address);
        FormClosed+=(_,_)=>browser.Dispose();
    }
    async Task Initialize()
    {
        try{
            var environment=await CoreWebView2Environment.CreateAsync(userDataFolder:profile);
            if(IsDisposed)return;await browser.EnsureCoreWebView2Async(environment);if(IsDisposed)return;
            browser.CoreWebView2.Settings.AreDevToolsEnabled=false;
            BrowserPopupHost.Attach(browser,environment,this,Report);
            browser.CoreWebView2.SourceChanged+=(_,_)=>address.Text=browser.Source?.GetLeftPart(UriPartial.Path)??"";
            browser.CoreWebView2.NavigationStarting+=(_,_)=>{read.Enabled=false;Report("Loading page… If sign-in opens another window, complete it there.");};
            browser.CoreWebView2.NavigationCompleted+=(_,e)=>{read.Enabled=e.IsSuccess;
                Report(e.IsSuccess?"Page loaded. Sign in if needed, return to the requested entry, then click Read this monster. If the page is blank, try Reload or Open in default browser.":$"Page failed to load ({e.WebErrorStatus}). Try Reload or Open in default browser, then paste the stat block.");};
            browser.CoreWebView2.ProcessFailed+=(_,e)=>{read.Enabled=false;Report($"The browser stopped ({e.ProcessFailedKind}). Try Reload or Open in default browser.");};
            browser.CoreWebView2.Navigate(requested.AbsoluteUri);
        }catch(Exception e){if(!IsDisposed)message.Text="The embedded browser couldn't start. It requires Microsoft Edge WebView2 Runtime. You can still use URL loading or paste text from your normal browser.\n"+e.Message;}
    }
    void Report(string text){if(!IsDisposed){if(message.Text==BrowserPopupHost.GoogleMessage&&!text.Contains("Google"))return;message.Text=text;}}
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
