using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using WpfBank.ViewModels;

namespace WpfBank
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["bankData"].ConnectionString;
        protected override void OnStartup(StartupEventArgs e)
        {
            MainViewModel mainViewModel = new MainViewModel();
            MWindow mainWindow = new MWindow() { DataContext = mainViewModel };
            mainWindow.Show();
            base.OnStartup(e);
        }
    }
}
