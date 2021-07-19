using ClassLibrary;
using System;
using System.Windows;
using WpfBank.ViewModels;

namespace WpfBank
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MWindow : Window
    {
        public MWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
