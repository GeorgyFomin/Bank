using ClassLibrary;
using FontAwesome.Sharp;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WpfBank.Commands;

namespace WpfBank.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public static readonly string[] tableNames = { "Deposits", "Loans", "Clients", "Departments" };
        public static void Log(string report)
        {
            using (TextWriter tw = File.AppendText("log.txt"))
                tw.WriteLine(DateTime.Now.ToString() + ":" + report);
        }
        #region Fields
        private RelayCommand dragCommand;
        private RelayCommand minimizeCommand;
        private RelayCommand maximizeCommand;
        private RelayCommand closeCommand;
        private RelayCommand clientsCommand;
        private RelayCommand depositsCommand;
        private RelayCommand loansCommand;
        private RelayCommand resetBankCommand;
        private Bank bank;
        private ViewModelBase viewModel;
        #endregion
        #region Properties
        public ViewModelBase ViewModel { get => viewModel; set { viewModel = value; RaisePropertyChanged(nameof(ViewModel)); } }
        public Bank Bank { get => bank; set { bank = value; RaisePropertyChanged(nameof(Bank)); } }
        public ICommand DragCommand => dragCommand ?? (dragCommand = new RelayCommand((e) => (e as MWindow).DragMove()));
        public ICommand MinimizeCommand => minimizeCommand ?? (minimizeCommand =
            new RelayCommand((e) => (e as MWindow).WindowState = WindowState.Minimized));
        public ICommand MaximizeCommand => maximizeCommand ?? (maximizeCommand = new RelayCommand((e) =>
        {
            MWindow window = e as MWindow;
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            window.MaxIconBlock.Icon = window.WindowState == WindowState.Maximized ? IconChar.WindowRestore : IconChar.WindowMaximize;
        }));
        public ICommand CloseCommand => closeCommand ?? (closeCommand = new RelayCommand((e) =>
        {
            (e as MWindow).Close();
        }));
        public ICommand ClientsCommand => clientsCommand ?? (clientsCommand = new RelayCommand((e) => ViewModel = new ClientViewModel(Bank)));
        public ICommand DepositsCommand => depositsCommand ?? (depositsCommand = new RelayCommand((e) => ViewModel = new DepositViewModel(Bank)));
        public ICommand LoansCommand => loansCommand ?? (loansCommand = new RelayCommand((e) => ViewModel = new LoanViewModel(Bank)));
        public ICommand ResetBankCommand => resetBankCommand ?? (resetBankCommand = new RelayCommand((e) => ResetBank()));
        #endregion
        public MainViewModel()
        {
            ResetBank();
        }
        private void ResetBank()
        {
            // Очищаем все таблицы БД от данных, создаем случайные данные и заполняем ими таблицы.
            using (SqlConnection connection = new SqlConnection() { ConnectionString = App.ConnectionString })
                try
                {
                    connection.Open();
                    for (int i = 0; i < tableNames.Length; i++)
                        new SqlCommand($"delete from {tableNames[i]}", connection).ExecuteNonQuery();
                    // Создаем объекты банка.
                    Bank = RandomBank.GetBank();
                    // Заполняем таблицы БД полученными полями.
                    FillDBTables(connection);
                }
                catch
                {
                    throw;
                }
            Log($"Создан банк {bank.Name}.");
            ViewModel = new BankNameViewModel(Bank);
        }
        private void FillDBTables(SqlConnection connection)
        {
            void PopulateDBTable(string tableName)
            {
                switch (tableName)
                {
                    case "Departments":
                        foreach (Dep dep in bank.Deps)
                        {
                            new SqlCommand($"insert into {tableName} values ('{dep.ID}', '{dep.Name}')", connection).ExecuteNonQuery();
                        }
                        break;
                    case "Clients":
                        foreach (Dep dep in bank.Deps)
                        {
                            foreach (Client client in dep.Clients)
                            {
                                new SqlCommand($"insert into {tableName} values ('{client.ID}', '{client.Name}', '{client.DepID}')", connection).ExecuteNonQuery();
                            }
                        }
                        break;
                    case "Loans":
                        foreach (Dep dep in bank.Deps)
                        {
                            foreach (Client client in dep.Clients)
                            {
                                foreach (Account account in client.Accounts)
                                {
                                    new SqlCommand($"insert into " + (account.Size >= 0 ? "Deposits" : "Loans") +
                                      $" values ('{account.ID}', '{account.ClientID}', {account.Number}, "
                                      + account.Size.ToString(CultureInfo.InvariantCulture) + ", " +
                                      account.Rate.ToString(CultureInfo.InvariantCulture) +
                                      (account.Cap ? ", 1" : ", 0") + ")", connection).ExecuteNonQuery();
                                }
                            }
                        }
                        break;
                }
            }
            for (int i = tableNames.Length - 1; i > 0; i--)
            {
                PopulateDBTable(tableNames[i]);
            }
        }
        private RelayCommand dBModeCommand;
        private string toolTipText = "Режим " + (DBMode ? "базы данных" : "коллекций");
        public string ToolTipText { get => toolTipText; set { toolTipText = value; RaisePropertyChanged(nameof(ToolTipText)); } }
        public ICommand DBModeCommand => dBModeCommand ?? (dBModeCommand = new RelayCommand((e) =>
        {
            DBMode = !DBMode;
            ToolTipText = "Режим " + (DBMode ? "базы данных" : "коллекций");
        }));
        public static bool DBMode { get; private set; } = true;
    }
}
