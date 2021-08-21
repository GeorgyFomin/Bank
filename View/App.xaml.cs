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
        protected override void OnStartup(StartupEventArgs e)
        {
            SqlConnection sqlConnection = new SqlConnection() { ConnectionString = ConfigurationManager.ConnectionStrings["bankData"].ConnectionString };
            try
            {
                sqlConnection.Open();
            }
            catch
            {
                throw;
            }
            MainViewModel mainViewModel = new MainViewModel(sqlConnection);
            MWindow mainWindow = new MWindow() { DataContext = mainViewModel };
            mainWindow.Show();
            base.OnStartup(e);
        }
    }
}
