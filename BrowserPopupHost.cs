using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EncounterArchitect;

// A login popup must remain a real child browsing context. Navigating the opener
// instead loses window.opener, postMessage callbacks, and about:blank handoffs.
public static class BrowserPopupHost
{
    public const string GoogleMessage="Google sign-in requires your normal browser. Click Open in default browser, sign in there, copy the complete monster stat block, then return to the importer and click Paste clipboard. Sign-in in your normal browser is not shared with this embedded browser.";
    public static bool IsGoogleSignIn(string value)=>Uri.TryCreate(value,UriKind.Absolute,out var uri)&&uri.Host.Equals("accounts.google.com",StringComparison.OrdinalIgnoreCase);
    public static bool CanNavigate(string value)=>Uri.TryCreate(value,UriKind.Absolute,out var uri)&&
        (uri.Scheme=="https"||uri.AbsoluteUri=="about:blank");

    public static void Attach(WebView2 view,CoreWebView2Environment environment,Form owner,Action<string> report)
    {
        view.CoreWebView2.NavigationStarting+=(_,e)=>{if(IsGoogleSignIn(e.Uri)){e.Cancel=true;report(GoogleMessage);}else if(!CanNavigate(e.Uri)){e.Cancel=true;report("The browser could not open this kind of link. Use your default browser and paste the stat block if needed.");}};
        view.CoreWebView2.NewWindowRequested+=async(_,e)=>
        {
            var deferral=e.GetDeferral();e.Handled=true;Form? popup=null;
            try{
                if(IsGoogleSignIn(e.Uri)){report(GoogleMessage);return;}
                if(!CanNavigate(e.Uri)){report("The sign-in popup requested an unsupported address. Use your default browser and paste the stat block.");return;}
                popup=new Form{Text="D&D Beyond sign-in",Size=new Size(900,750),MinimumSize=new Size(650,500),StartPosition=FormStartPosition.CenterParent,Opacity=owner.Opacity,ShowInTaskbar=owner.ShowInTaskbar};
                var child=new WebView2{Dock=DockStyle.Fill};popup.Controls.Add(child);popup.Show(owner);
                await child.EnsureCoreWebView2Async(environment);
                if(popup.IsDisposed||owner.IsDisposed)return;
                Attach(child,environment,popup,report);
                child.CoreWebView2.WindowCloseRequested+=(_,_)=>popup.Close();
                child.CoreWebView2.NavigationCompleted+=(_,args)=>{if(!args.IsSuccess)report($"The sign-in page could not load ({args.WebErrorStatus}). Try Reload or Open in default browser.");};
                child.CoreWebView2.ProcessFailed+=(_,args)=>report($"The sign-in browser stopped ({args.ProcessFailedKind}). Close its window and try again, or use your default browser.");
                e.NewWindow=child.CoreWebView2;
                report("A separate sign-in window is open. Complete sign-in there, then return to the monster entry.");
            }catch(Exception error){popup?.Close();report("Couldn't open the sign-in window: "+error.Message+" Use your default browser and paste the stat block instead.");}
            finally{deferral.Complete();}
        };
    }
}
