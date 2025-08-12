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
            ThemeComboBox.SelectedIndex = 0; // Ĭ��ѡ�� Light
            AccentComboBox.SelectedIndex = 0; // Ĭ��ѡ�� Blue
            FontComboBox.SelectedIndex = 0; // Ĭ������
            FontSizeSlider.Value = 14; // Ĭ���ֺ�
            CornerRadiusSlider.Value = 8; // Ĭ��Բ��
            ApplyButton.Click += ApplyButton_Click;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var theme = (ThemeComboBox.SelectedIndex == 1) ? "Dark" : "Light";
            var accent = (AccentComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Blue";
            // �л� MahApps.Metro ����
            var dict = $"pack://application:,,,/MahApps.Metro;component/Styles/Themes/{theme}.{accent}.xaml";
            var mergedDict = new ResourceDictionary { Source = new System.Uri(dict) };
            Application.Current.Resources.MergedDictionaries[2] = mergedDict;

            // ��������
            var fontFamily = (FontComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Segoe UI";
            var fontSize = FontSizeSlider.Value;
            Application.Current.MainWindow.FontFamily = new FontFamily(fontFamily);
            Application.Current.MainWindow.FontSize = fontSize;

            // Բ������
            var border = Application.Current.MainWindow.FindName("MainBorder") as Border;
            if (border != null)
            {
                border.CornerRadius = new CornerRadius(CornerRadiusSlider.Value);
            }
        }
    }
}
