using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;

namespace WebViewTest {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        string[] protocols = {
            "http:",
            "https:",
            "ftp:",  
            "sftp:",
            "scp:",
            "edge:",
            "javascript:"
        };
        const string ntp = "https://ntp.msn.com/edge/ntp?locale=en&dsp=0&sp=Google";
        const string googlePrefix = "https://www.google.com/search?q=";

        const string goBack = "window.history.back()";
        const string goForward = "window.history.forward()";

        public MainWindow() {
            InitializeComponent();
            webView.NavigationStarting += UrlCheck;
            InitializeAsync();
        }

        bool hasProtocol (string url) {
            foreach (string p in protocols) {
                if (url.StartsWith(p)) return true;
            }
            return false;
        }
        bool isUrl(string url) {
            Regex reg = new Regex(@"^\w+\.\w+\/?$");
            return reg.IsMatch(url);
        }

        async void InitializeAsync() {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.WebMessageReceived += UpdateAddressBar;

            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.chrome.webview.postMessage(window.document.URL);");
            // await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.chrome.webview.addEventListener(\'message\', event => alert(event.data));");
        }

        void UpdateAddressBar(object sender, CoreWebView2WebMessageReceivedEventArgs args) {
            string uri = args.TryGetWebMessageAsString();
            addressBar.Text = uri;
            webView.CoreWebView2.PostWebMessageAsString(uri);
        }

        void UrlCheck(object sender, CoreWebView2NavigationStartingEventArgs args) {
            string uri = args.Uri;
            if (!uri.StartsWith("https://")) {
                webView.CoreWebView2.ExecuteScriptAsync($"alert('Warning: {uri} is not safe. Try an https link for better security.')");
            }
        }

        private void addressBar_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {

                if (webView != null && webView.CoreWebView2 != null) {
                    if (hasProtocol(addressBar.Text)) {
                        webView.CoreWebView2.Navigate(addressBar.Text);
                    } else if (isUrl(addressBar.Text)) {
                        webView.CoreWebView2.Navigate("http://" + addressBar.Text);
                    } else {
                        webView.CoreWebView2.Navigate(googlePrefix + addressBar.Text.Replace(' ', '+'));
                    }
                }
            }
        }
    }
}
