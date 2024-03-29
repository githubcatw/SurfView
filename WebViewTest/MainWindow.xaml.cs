﻿using System;
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
            "about:",
            "file:"
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
        // JavaScript for reloading the page.
        const string refreshScript = "location.reload();";
        // A list of all loaded userscripts.
        List<string> userscripts = new List<string>();

        // Ran when the window is loaded.
        public MainWindow() {
            InitializeComponent();
            // Add listeners for NavigationStarting
            webView.NavigationStarting += UrlCheck;
            // Add listener for NavigationComplete to load userscripts
            webView.NavigationCompleted += RunUserscripts;
            // And to get the title
            webView.NavigationCompleted += NavigationComplete;
            // Initialize
            InitializeAsync();
        }

        private void NavigationComplete(object sender, CoreWebView2NavigationCompletedEventArgs e) {
            // Post the title as a message
            webView.ExecuteScriptAsync("window.chrome.webview.postMessage(\"title=\" + window.document.title);");
        }

        // Run all loaded userscripts.
        async private void RunUserscripts(object sender, CoreWebView2NavigationCompletedEventArgs e) {
            // For each userscript:
            foreach (string script in userscripts) {
                // Execute it
                await webView.ExecuteScriptAsync(script);
            }
        }

        // Does a given URL have a supported protocol?
        bool hasProtocol (string url) {
            // For each protocol:
            foreach (string p in protocols) {
                // If the URL starts with it, return true
                if (url.StartsWith(p)) return true;
            }
            // Return false if the URL doesn't start with any protocol
            return false;
        }
        // Is a given string a URL?
        bool isUrl(string url) {
            // Create a regex that matches a URL
            Regex reg = new Regex(@"^(^|\s)((https?:\/\/)?[\w-]+(\.[\w-]+)+\.?(:\d+)?(\/\S*)?)$");
            // Return if the given string matches it (so the string is a URL)
            return reg.IsMatch(url);
        }

        // Initialize the WebView.
        async void InitializeAsync() {
            // Initialize the WebView
            await webView.EnsureCoreWebView2Async(null);
            // Add listeners to the WebMessageReceived event
            webView.CoreWebView2.WebMessageReceived += UpdateAddressBar;
            webView.CoreWebView2.WebMessageReceived += ReadPayload;
            // Add a script to run when a page is loading
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                // This script just posts a message with the window's URL
                "window.chrome.webview.postMessage(\"uri=\" + window.document.URL);"
            );
            // If there is no userscript folder, create one
            if (!Directory.Exists("userscripts")) Directory.CreateDirectory("userscripts");
            // For each userscript:
            foreach (string userscript in Directory.GetFiles("userscripts", "*.js")) {
                // Read its content
                var usContent = File.ReadAllText(userscript);
                // Add it to the userscript list
                userscripts.Add(usContent);
            }
        }

        // Update the text of the address bar.
        void UpdateAddressBar(object sender, CoreWebView2WebMessageReceivedEventArgs args) {
            // Get the received message (the URI of the current page)
            string msg = args.TryGetWebMessageAsString();
            // If the message is a payload, return
            if (msg.StartsWith(":svpl:")) return;
            // If it's a URI, get the URI
            if (msg.StartsWith("url=")) {
                string uri = msg.Replace("url=", "");
                // If the URI isn't a config page:
                if (!uri.Contains(Directory.GetCurrentDirectory().Replace('\\', '/') + "/config/")) {
                    // If the URI isn't the homepage, set the address bar's text to it
                    if (uri != home) addressBar.Text = uri;
                    // Else, set it to the browsing prompt
                    else addressBar.Text = prompt;
                }
            }
            // If it's a title:
            if (msg.StartsWith("title=")) {
                // If there is a title sent:
                if (msg != "title=")
                    // Update the window title
                    Title = msg.Replace("title=", "") + " - SurfView";
            }
        }

        // Read payloads sent by config pages
        void ReadPayload(object sender, CoreWebView2WebMessageReceivedEventArgs args) {
            // Get the received message
            string uri = args.TryGetWebMessageAsString();
            // If the URI isn't a config page, return
            if (!uri.Contains(Directory.GetCurrentDirectory().Replace('\\', '/') + "/config/")) return;
            // If the message isn't a payload, return
            if (!uri.StartsWith(":svpl:")) return;
            // Remove header
            uri = uri.Replace(":svpl:", "");
            MessageBox.Show(uri);
        }

        // Check a URL.
        void UrlCheck(object sender, CoreWebView2NavigationStartingEventArgs args) {
            // Get the URL
            string uri = args.Uri;
            // If it's a secure URL (starts with https or sftp) or a settings page (edge or sv):
            if (uri.StartsWith("https:") || uri.StartsWith("sftp:") ||
                uri.StartsWith("edge:") || addressBar.Text.StartsWith("sv:")) {
                // Set the padlock icon to a padlock
                padlock.Content = "";
                // Color it green
                padlock.Foreground = Brushes.LightGreen;
                // Set its tooltip to "Secure"
                padlock.ToolTip = "Secure";
            // Else:
            } else {
                // Set the padlock icon to a warning shield
                padlock.Content = "";
                // Color it red
                padlock.Foreground = Brushes.Red;
                // Set its tooltip to "Not secure"
                padlock.ToolTip = "Not secure";
            }
        }

        // Ran when the user presses a key in the address bar.
        private void addressBar_KeyDown(object sender, KeyEventArgs e) {
            // If the user pressed Enter:
            if (e.Key == Key.Enter) {
                // If the WebView is initialized (just for safety):
                if (webView != null && webView.CoreWebView2 != null) {
                    addressBar.Text = addressBar.Text.Replace("‭", "");
                    // If the address bar's text has a supported protocol:
                    if (hasProtocol(addressBar.Text)) {
                        // Navigate to the text
                        webView.CoreWebView2.Navigate(addressBar.Text);
                    // Else, if the address bar's text is a URL:
                    } else if (isUrl(addressBar.Text)) {
                        // Navigate to "http://" + the address bar's text
                        // Secure sites should redirect us to the https version
                        webView.CoreWebView2.Navigate("http://" + addressBar.Text);
                    // Else, if it's a config page (the text contains the sv: protocol)
                    } else if (addressBar.Text.StartsWith("sv:")) {
                        // Navigate to the correct config page
                        webView.CoreWebView2.Navigate(Directory.GetCurrentDirectory() + "/config/" + addressBar.Text.Replace("sv:", "") + ".html");
                    // Else:
                    } else {
                        // Search the address bar's text with Google, replacing the spaces with plus signs and removing the LRO
                        webView.CoreWebView2.Navigate(googlePrefix + addressBar.Text.Replace(' ', '+'));
                    }
                }
            }
        }

        // Runs the go back script when the Back button is pressed.
        private void back_Click(object sender, RoutedEventArgs e) { webView.CoreWebView2.ExecuteScriptAsync(goBack); }
        // Runs the go forward script when the Next button is pressed.
        private void next_Click(object sender, RoutedEventArgs e) { webView.CoreWebView2.ExecuteScriptAsync(goForward); }
        // Runs the reload script when the Reload button is pressed.
        private void refresh_Click(object sender, RoutedEventArgs e) { webView.CoreWebView2.ExecuteScriptAsync(refreshScript); }
        // Fires when the address bar is clicked.
        private void addressBar_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            // Set the address bar's text to a left-right override if we're on the homepage
            if (addressBar.Text == prompt) addressBar.Text = "‭";
        }
    }
}
