using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EncounterArchitect;

static class BrowserFlowTest
{
    // Optional integration check: synthetic pages only, isolated profile, no login
    // or user browser data. Verifies an about:blank popup can call its opener.
    public static void Run()
    {
        using var host=new Form{Opacity=0,ShowInTaskbar=false,Width=700,Height=500};
        using var view=new WebView2{Dock=DockStyle.Fill};host.Controls.Add(view);
        host.Shown+=async(_,_)=>{
            string result=Path.Combine(AppContext.BaseDirectory,"browser-flow-check.txt");
            var diagnostics=new List<string>();
            try{
                var env=await CoreWebView2Environment.CreateAsync(userDataFolder:Path.Combine(AppContext.BaseDirectory,"browser-flow-profile"));
                await view.EnsureCoreWebView2Async(env);
                view.CoreWebView2.NavigationStarting+=(_,e)=>diagnostics.Add($"Navigate: {e.Uri}, cancelled: {e.Cancel}");
                var loaded=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var received=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                view.CoreWebView2.NavigationCompleted+=(_,e)=>{diagnostics.Add($"Loaded: {e.IsSuccess}, {e.WebErrorStatus}");if(e.IsSuccess)loaded.TrySetResult();else loaded.TrySetException(new Exception(e.WebErrorStatus.ToString()));};
                view.CoreWebView2.WebMessageReceived+=(_,e)=>{if(e.TryGetWebMessageAsString()=="popup-ok")received.TrySetResult();};
                view.NavigateToString("<html><body><script>window.addEventListener('message',e=>{if(e.data==='popup-ok')chrome.webview.postMessage('popup-ok')});</script></body></html>");
                await loaded.Task.WaitAsync(TimeSpan.FromSeconds(15));
                BrowserPopupHost.Attach(view,env,host,s=>diagnostics.Add(s));
                await view.ExecuteScriptAsync("(()=>{const p=window.open('about:blank');p.document.write(\"<script>window.opener.postMessage('popup-ok','*');window.close();<\\/script>\");})()");
                await received.Task.WaitAsync(TimeSpan.FromSeconds(20));
                File.WriteAllText(result,"PASS: real WebView2 about:blank popup, window.opener callback, and popup close; isolated synthetic page, no account access.");
            }catch(Exception e){Environment.ExitCode=1;File.WriteAllText(result,"FAIL: "+e+Environment.NewLine+string.Join(Environment.NewLine,diagnostics));}
            finally{host.Close();}
        };
        Application.Run(host);
    }
}
