using MeowBrowserExtern;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MeowBrowser
{
    /// <summary>
    /// Interaction logic for NetworkStackWindow.xaml
    /// </summary>
    public partial class NetworkStackWindow : Window
    {
        public NetworkStackWindow(ObservableCollection<NetworkEntry> networkStack)
        {
            InitializeComponent();
            NetworkList.ItemsSource = networkStack;
        }
    }
}
