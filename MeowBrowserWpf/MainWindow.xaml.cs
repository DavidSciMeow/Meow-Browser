using MeowBrowserExtern;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Wpf;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MeowBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IAppContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ObservableCollection<PluginStatus> _pluginStatuses = [];
        public ICommand CloseTabCommand { get; }
        public MainWindow()
        {
            InitializeComponent();

            Closed += (s, e) =>
            {
                foreach (Window win in Application.Current.Windows)
                {
                    if (win != this)
                        win.Close();
                }
            };

            string pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            var services = new ServiceCollection();

            if (!Directory.Exists(pluginPath)) Directory.CreateDirectory(pluginPath);

            foreach (var dll in Directory.GetFiles(pluginPath, "*.dll"))
            {
                var asm = Assembly.LoadFrom(dll);
                foreach (var type in asm.GetTypes())
                {
                    if (typeof(IMeowPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        try
                        {
                            var instance = Activator.CreateInstance(type) as IMeowPlugin;
                            _pluginStatuses.Add(new PluginStatus
                            {
                                Name = instance?.Name,
                                Loaded = true,
                                Unloaded = false,
                                Instance = instance
                            });
                            services.AddSingleton(typeof(IMeowPlugin), type);
                        }
                        catch (Exception ex)
                        {
                            _pluginStatuses.Add(new PluginStatus
                            {
                                Name = type.FullName ?? "Unknown",
                                Loaded = false,
                                Unloaded = true,
                                ErrorMessage = ex.Message,
                                StackTrace = ex.ToString()
                            });
                        }
                    }
                }
            }

            _serviceProvider = services.BuildServiceProvider();

            InvokePlugins(plugin => plugin.OnAppLoaded(this));

            // 打开插件管理窗体
            var pluginManager = new PluginManagerWindow(_pluginStatuses);

            PluginManagerButton.Click += (s, e) =>
            {
                var pluginManager = new PluginManagerWindow(_pluginStatuses);
                pluginManager.Show();
            };

            NewTabButton.Click += (s,e) => AddNewTab("https://www.bing.com");
            AddNewTab("https://www.bing.com");

            CloseTabCommand = new RelayCommand<TabItem>(tab =>
            {
                if (tab?.Content is BrowserTabPage page)
                    page.WebViewControl?.Dispose();
                BrowserTabControl.Items.Remove(tab);
            });

            BrowserTabControl.DataContext = this;
        }
        private void InvokePlugins(Action<IMeowPlugin> action)
        {
            foreach (var status in _pluginStatuses.Where(p => p.Loaded && !p.Unloaded))
            {
                try
                {
                    if (status.Instance != null)
                        action(status.Instance);
                }
                catch (Exception ex)
                {
                    status.Unloaded = true;
                    status.ErrorMessage = ex.Message;
                    status.StackTrace = ex.ToString();
                }
            }
        }
        public void AddNewTab(string url)
        {
            var tabPage = new BrowserTabPage(url);
            var tabItem = new TabItem
            {
                Header = "New Tab",
                Content = tabPage
            };

            tabPage.RequestClose += page =>
            {
                if (tabItem.Content == page)
                {
                    page.WebViewControl?.Dispose();
                    BrowserTabControl.Items.Remove(tabItem);
                }
            };

            // 处理新建Tab请求
            tabPage.RequestNewTab += AddNewTab;

            BrowserTabControl.Items.Add(tabItem);
            BrowserTabControl.SelectedItem = tabItem;

            // 可在此处绑定插件事件、网络堆栈等
            tabPage.WebViewControl.CoreWebView2InitializationCompleted += (s, e) =>
            {
                tabPage.WebViewControl.CoreWebView2.WebResourceRequested += (ws, we) =>
                {
                    var entry = new NetworkEntry
                    {
                        Method = we.Request.Method,
                        Url = we.Request.Uri,
                        StatusCode = we.Response?.StatusCode ?? 0,
                        ContentType = we.Response?.Headers.GetHeader("Content-Type") ?? ""
                    };
                    InvokePlugins(plugin => plugin.OnNetworkRequest(entry));
                    // 可扩展：将 entry 加入每个 tabPage 的网络堆栈
                };
            };
        }
        public void CloseActiveTab()
        {
            if (BrowserTabControl.SelectedItem is TabItem selectedTab && selectedTab.Content is BrowserTabPage page)
            {
                page.WebViewControl?.Dispose();
                BrowserTabControl.Items.Remove(selectedTab);
            }
        }
    }
}