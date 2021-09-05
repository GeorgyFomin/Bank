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
        public static SqlConnection SqlConnection { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            SqlConnection = new SqlConnection() { ConnectionString = ConfigurationManager.ConnectionStrings["bankData"].ConnectionString };
            try
            {
                SqlConnection.Open();
            }
            catch
            {
                throw;
            }
            MainViewModel mainViewModel = new MainViewModel();
            MWindow mainWindow = new MWindow() { DataContext = mainViewModel };
            mainWindow.Show();
            base.OnStartup(e);
        }
    }
}
