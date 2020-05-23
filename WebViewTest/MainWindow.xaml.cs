using System;
using System.Collections.Generic;
using System.IO;
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

        // Supported protocols.
        string[] protocols = {
            "http:",
            "https:",
            "ftp:",  
            "sftp:",
            "scp:",
            "edge:",
            "javascript:",
            "chrome-search:",
            "data:",
            "about:"
        };
        // The URL of the home page.
        const string home = "https://ntp.msn.com/edge/ntp?locale=en&dsp=0&sp=Google";
        // Prefix for searching on Google.
        const string googlePrefix = "https://www.google.com/search?q=";
        // The default home page prompt.
        const string prompt = "Where do you want to go today?";
        // JavaScript for going back and forward in the history.
        const string goBack = "window.history.back();";
        const string goForward = "window.history.forward();";
        // A list of all loaded userscripts.
        List<string> userscripts = new List<string>();

        public MainWindow() {
            InitializeComponent();
            // Add listener for NavigationStarting
            webView.NavigationStarting += UrlCheck;
            // Add listener for NavigationComplete to load userscripts
            webView.NavigationCompleted += LoadUserscripts;
            // Initialize
            InitializeAsync();
        }

        async private void LoadUserscripts(object sender, CoreWebView2NavigationCompletedEventArgs e) {
            // For each userscript:
            foreach (string script in userscripts) {
                // Execute it
                await webView.ExecuteScriptAsync(script);
            }
        }

        bool hasProtocol (string url) {
            foreach (string p in protocols) {
                if (url.StartsWith(p)) return true;
            }
            return false;
        }
        bool isUrl(string url) {
            Regex reg = new Regex(@"^(^|\s)((https?:\/\/)?[\w-]+(\.[\w-]+)+\.?(:\d+)?(\/\S*)?)$");
            return reg.IsMatch(url);
        }

        async void InitializeAsync() {
            // Initialize the WebView
            await webView.EnsureCoreWebView2Async(null);
            // Add a listener to the WebMessageReceived event
            webView.CoreWebView2.WebMessageReceived += UpdateAddressBar;
            // Add a script to run when a page is loading
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                // This script just posts a message with the window's URL
                "window.chrome.webview.postMessage(window.document.URL);"
            );
            // For each userscript:
            foreach (string userscript in Directory.GetFiles("userscripts", "*.js")) {
                // Read its content
                var usContent = File.ReadAllText(userscript);
                // Add it to the userscript list
                userscripts.Add(usContent);
            }
        }

        void UpdateAddressBar(object sender, CoreWebView2WebMessageReceivedEventArgs args) {
            string uri = args.TryGetWebMessageAsString();
            if (uri != home) addressBar.Text = uri;
            else uri = prompt;
            webView.CoreWebView2.PostWebMessageAsString(uri);
        }

        void UrlCheck(object sender, CoreWebView2NavigationStartingEventArgs args) {
            string uri = args.Uri;
            if (!uri.StartsWith("https://")) {
                
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

        private void back_Click(object sender, RoutedEventArgs e) {
            webView.CoreWebView2.ExecuteScriptAsync(goBack);
        }

        private void next_Click(object sender, RoutedEventArgs e) {
            webView.CoreWebView2.ExecuteScriptAsync(goForward);
        }
    }
}
