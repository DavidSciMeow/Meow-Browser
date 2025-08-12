using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MeowBrowser
{
    public partial class ThemeSettingsWindow : MetroWindow
    {
        public ThemeSettingsWindow()
        {
            InitializeComponent();
            ThemeComboBox.SelectedIndex = 0; // 默认选中 Light
            AccentComboBox.SelectedIndex = 0; // 默认选中 Blue
            FontComboBox.SelectedIndex = 0; // 默认字体
            FontSizeSlider.Value = 14; // 默认字号
            CornerRadiusSlider.Value = 8; // 默认圆角
            ApplyButton.Click += ApplyButton_Click;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var theme = (ThemeComboBox.SelectedIndex == 1) ? "Dark" : "Light";
            var accent = (AccentComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Blue";
            // 切换 MahApps.Metro 主题
            var dict = $"pack://application:,,,/MahApps.Metro;component/Styles/Themes/{theme}.{accent}.xaml";
            var mergedDict = new ResourceDictionary { Source = new System.Uri(dict) };
            Application.Current.Resources.MergedDictionaries[2] = mergedDict;

            // 字体设置
            var fontFamily = (FontComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Segoe UI";
            var fontSize = FontSizeSlider.Value;
            Application.Current.MainWindow.FontFamily = new FontFamily(fontFamily);
            Application.Current.MainWindow.FontSize = fontSize;

            // 圆角设置
            var border = Application.Current.MainWindow.FindName("MainBorder") as Border;
            if (border != null)
            {
                border.CornerRadius = new CornerRadius(CornerRadiusSlider.Value);
            }
        }
    }
}
