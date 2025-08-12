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
using System.Linq;
using MahApps.Metro.Controls;

namespace MeowBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, IAppContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ObservableCollection<PluginStatus> _pluginStatuses = [];
        public ICommand CloseTabCommand { get; }
        public MainWindow()
        {
            InitializeComponent();

            this.Closed += (s, e) =>
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

            // 打开插件管理窗体（已移除按钮，改为菜单项事件）
            var pluginManager = new PluginManagerWindow(_pluginStatuses);

            // 菜单项事件绑定
            var pluginManagerMenuItem = this.FindName("PluginManagerMenuItem") as MenuItem;
            if (pluginManagerMenuItem != null)
            {
                pluginManagerMenuItem.Click += (s, e) =>
                {
                    var pluginManager = new PluginManagerWindow(_pluginStatuses);
                    pluginManager.Show();
                };
            }
            // 新增主题设置菜单项事件绑定
            var themeSettingsMenuItem = this.FindName("ThemeSettingsMenuItem") as MenuItem;
            if (themeSettingsMenuItem != null)
            {
                themeSettingsMenuItem.Click += (s, e) =>
                {
                    var themeWindow = new ThemeSettingsWindow();
                    themeWindow.Owner = this;
                    themeWindow.ShowDialog();
                };
            }

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

            // 在“＋”Tab前插入新Tab
            int plusIndex = BrowserTabControl.Items.Count - 1;
            BrowserTabControl.Items.Insert(plusIndex, tabItem);
            BrowserTabControl.SelectedItem = tabItem;

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
                };
            };
        }

        // 监听TabControl的SelectionChanged事件
        private void BrowserTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 如果选中的是“＋”Tab，则新建Tab
            if (BrowserTabControl.SelectedItem is TabItem tab && tab.Header?.ToString() == "＋")
            {
                AddNewTab("https://www.bing.com");
            }
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