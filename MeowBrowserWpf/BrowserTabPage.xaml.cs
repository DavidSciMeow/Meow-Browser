using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;
using System.Collections.ObjectModel;
using MeowBrowserExtern;
using System.Diagnostics;

namespace MeowBrowser
{
    public partial class BrowserTabPage : UserControl, IPageContext
    {
        public event Action<BrowserTabPage>? RequestClose;
        public event Action<string>? RequestNewTab; // 新建Tab请求（传递URL）

        public WebView2 WebViewControl => WebView;

        public ObservableCollection<NetworkEntry> NetworkStack { get; } = [];

        public string Url => WebViewControl.Source?.ToString() ?? "";

        public void ExecuteScript(string script)
        {
            WebViewControl.ExecuteScriptAsync(script);
        }

        public void Reload()
        {
            WebViewControl.Reload();
        }

        public void ClickElementBySelector(string cssSelector)
        {
            var script = $"var el = document.querySelector('{cssSelector}'); if(el) el.click();";
            WebViewControl.ExecuteScriptAsync(script);
        }

        public void InputTextBySelector(string cssSelector, string text)
        {
            var escapedText = text.Replace("'", "\\'");
            var script = $"var el = document.querySelector('{cssSelector}'); if(el) {{ el.value = '{escapedText}'; el.dispatchEvent(new Event('input', {{ bubbles: true }})); }}";
            WebViewControl.ExecuteScriptAsync(script);
        }

        public void ScrollBy(int deltaY)
        {
            var script = $"window.scrollBy(0, {deltaY});";
            WebViewControl.ExecuteScriptAsync(script);
        }

        public bool ElementExists(string cssSelector)
        {
            // WebView2的ExecuteScriptAsync是异步的，这里返回值需要异步处理
            var script = $"document.querySelector('{cssSelector}') !== null";
            var task = WebViewControl.ExecuteScriptAsync(script);
            task.Wait();
            var result = task.Result;
            return result == "true";
        }

        public BrowserTabPage(string url)
        {
            InitializeComponent();

            AddressBar.Text = url;

            WebView.CoreWebView2InitializationCompleted += (s, e) =>
            {
                // Register event handlers first
                WebView.CoreWebView2.WebResourceRequested += (ws, we) =>
                {
                    Debug.WriteLine($"Request: {we.Request.Uri} | Method: {we.Request.Method}");
                    var entry = new NetworkEntry
                    {
                        Method = we.Request.Method,
                        Url = we.Request.Uri,
                        StatusCode = we.Response?.StatusCode ?? 0,
                        ContentType = we.Response?.Headers.GetHeader("Content-Type") ?? ""
                    };
                    NetworkStack.Add(entry);
                };

                WebView.CoreWebView2.NewWindowRequested += (sender, args) =>
                {
                    args.Handled = true; // 阻止弹窗
                    var targetUrl = args.Uri;
                    RequestNewTab?.Invoke(targetUrl); // 通知主窗体新建Tab
                };

                WebView.NavigationCompleted += async (ns, ne) =>
                {
                    AddressBar.Text = WebView.Source?.ToString() ?? "";
                    try
                    {
                        var titleJson = await WebView.ExecuteScriptAsync("document.title");
                        var title = System.Text.Json.JsonSerializer.Deserialize<string>(titleJson);
                        if (Parent is TabItem tabItem)
                            tabItem.Header = string.IsNullOrWhiteSpace(title) ? WebView.Source?.Host ?? "Tab" : title;
                    }
                    catch
                    {
                        if (Parent is TabItem tabItem)
                            tabItem.Header = WebView.Source?.Host ?? "Tab";
                    }
                };

                BackButton.Click += (bs, be) => { if (WebView.CanGoBack) WebView.GoBack(); };
                ForwardButton.Click += (fs, fe) => { if (WebView.CanGoForward) WebView.GoForward(); };
                RefreshButton.Click += (rs, re) => WebView.Reload();

                // Add web resource requested filter
                WebView.CoreWebView2.AddWebResourceRequestedFilter("*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);

                // Now start navigation
                WebView.Source = new Uri(url);
            };

            NetworkStackButton.Click += (s, e) =>
            {
                var win = new NetworkStackWindow(NetworkStack);
                win.Show();
            };

            WebView.EnsureCoreWebView2Async();
        }

        private void AddressBar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (!string.IsNullOrWhiteSpace(AddressBar.Text))
                {
                    WebView.Source = new Uri(AddressBar.Text);
                }
            }
        }
    }
}
