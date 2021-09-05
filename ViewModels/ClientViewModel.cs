using ClassLibrary;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfBank.Commands;

namespace WpfBank.ViewModels
{
    public class ClientViewModel : ViewModelBase
    {
        /// <summary>
        /// Хранит режим источника данных - база данных (true) или локальная коллекция Clients (false).
        /// </summary>
        private const bool dbMode = true;
        #region Fields
        private readonly Bank bank;
        private Client client;
        private DataRowView dataRowView;
        private Dep dep;
        private RelayCommand selCommand;
        private RelayCommand removeClientCommand;
        private RelayCommand addClientCommand;
        private RelayCommand depSelectedCommand;
        private RelayCommand oKCommand;
        private RelayCommand endClientEditCommand;
        private bool depDoSelected;
        private SqlDataAdapter adapter;
        private DataTable clientsTable, depositsTable, loansTable;
        private DataView dataView;
        #endregion
        #region Properties
        /// <summary>
        /// Возвращает список всех кредитов банка.
        /// </summary>
        public ObservableCollection<Account> Loans
        {
            get
            {
                ObservableCollection<Account> loans = new ObservableCollection<Account>();
                foreach (Dep dep in bank.Deps)
                {
                    foreach (Client client in dep.Clients)
                    {
                        foreach (Account account in client.Accounts)
                        {
                            if (account.Size <= 0)
                                loans.Add(account);
                        }
                    }
                }
                return loans;
            }
        }
        /// <summary>
        /// Возвращает список всех депозитов банка.
        /// </summary>
        public ObservableCollection<Account> Deposits
        {
            get
            {
                ObservableCollection<Account> deposits = new ObservableCollection<Account>();
                foreach (Dep dep in bank.Deps)
                {
                    foreach (Client client in dep.Clients)
                    {
                        foreach (Account account in client.Accounts)
                        {
                            if (account.Size >= 0)
                                deposits.Add(account);
                        }
                    }
                }
                return deposits;
            }
        }
        /// <summary>
        /// Возвращает список всех клиентов банка.
        /// </summary>
        public ObservableCollection<Client> Clients
        {
            get
            {
                ObservableCollection<Client> clients = new ObservableCollection<Client>();
                foreach (Dep dep in bank.Deps)
                {
                    foreach (Client client in dep.Clients)
                    {
                        clients.Add(client);
                    }
                }
                return clients;
            }
        }
        public ObservableCollection<Dep> Deps { get => bank.Deps; }
        public Client Client { get => client; set { client = value; RaisePropertyChanged(nameof(Client)); } }
        public Dep Dep { get => dep; set { dep = value; RaisePropertyChanged(nameof(Dep)); } }
        public ICommand SelCommand => selCommand ?? (selCommand =
            new RelayCommand((e) =>
            {
                if (dbMode)
                {
                    if ((DataRowView = (e as DataGrid).SelectedItem is DataRowView rowView ? rowView : null) != null)
                        Client = Clients.First((g) => dataRowView.Row.Field<Guid>("Id") == g.ID);
                    return;
                }
                Client = (e as DataGrid).SelectedItem is Client client ? client : null;
            }));
        public ICommand RemoveClientCommand => removeClientCommand ?? (removeClientCommand = new RelayCommand(RemoveClient));
        public ICommand AddClientCommand => addClientCommand ?? (addClientCommand = new RelayCommand(EditClient));
        public ICommand DepSelectedCommand => depSelectedCommand ?? (depSelectedCommand = new RelayCommand((e) => DepDoSelected = true));
        public bool DepDoSelected { get => depDoSelected; set { depDoSelected = value; RaisePropertyChanged(nameof(DepDoSelected)); } }
        public ICommand OKCommand => oKCommand ?? (oKCommand = new RelayCommand((e) =>
        {
            DepsDialog window = e as DepsDialog;
            Dep = window.depListBox.SelectedItem is Dep dep ? dep : null;
            window.DialogResult = true;
        }));
        public ICommand EndClientEditCommand => endClientEditCommand ?? (endClientEditCommand = new RelayCommand(EditClient));

        public DataView DataView { get => dataView; set { dataView = value; RaisePropertyChanged(nameof(DataView)); } }
        public object DataSource { get; set; }
        public DataRowView DataRowView { get => dataRowView; set { dataRowView = value; RaisePropertyChanged(nameof(DataRowView)); } }
        #endregion
        public ClientViewModel(Bank bank) : this()
        {
            clientsTable = new DataTable("Clients");
            adapter = new SqlDataAdapter(new SqlCommand() { CommandText = "select * from [Clients]", Connection = App.SqlConnection });
            adapter.Fill(clientsTable);
            depositsTable = new DataTable("Deposits");
            adapter.SelectCommand = new SqlCommand() { CommandText = "select * from [Deposits]", Connection = App.SqlConnection };
            adapter.Fill(depositsTable);
            loansTable = new DataTable("Loans");
            adapter.SelectCommand = new SqlCommand() { CommandText = "select * from [Loans]", Connection = App.SqlConnection };
            adapter.Fill(loansTable);

            DataView = clientsTable.DefaultView;
            this.bank = bank;
            if (dbMode)
                DataSource = DataView;
            else
                DataSource = Clients;
        }
        public ClientViewModel()
        {
            depDoSelected = false;
        }
        private void RemoveClient(object e)
        {
            if (client != null &&
                MessageBox.Show("Удалить клиента?", "Удаление клиента " + client.Name, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                foreach (Account deposit in Deposits.Where((g) => g.ClientID == client.ID))
                    Deposits.Remove(deposit);
                RaisePropertyChanged(nameof(Deposits));
                foreach (Account loan in Loans.Where((g) => g.ClientID == client.ID))
                    Loans.Remove(loan);
                RaisePropertyChanged(nameof(Loans));
                _ = bank.Deps.First((g) => client.DepID == g.ID).Clients.Remove(client);
                RaisePropertyChanged(nameof(Clients));
                if (dbMode)
                {
                    //adapter.DeleteCommand =
                    new SqlCommand($"delete from {depositsTable} where '{dataRowView.Row.Field<Guid>("Id")}' = ClientId ", App.SqlConnection).ExecuteNonQuery();
                    adapter.Update(depositsTable);
                    //adapter.DeleteCommand = 
                    new SqlCommand($"delete from {loansTable} where '{dataRowView.Row.Field<Guid>("Id")}' = ClientId ", App.SqlConnection).ExecuteNonQuery();
                    adapter.Update(loansTable);
                    adapter.DeleteCommand = new SqlCommand($"delete from {clientsTable} where '{dataRowView.Row.Field<Guid>("Id")}' = Id ", App.SqlConnection);
                    adapter.Update(clientsTable);
                    dataRowView.Delete();
                    //DataView = clientsTable.DefaultView;
                }
                else
                    (e as DataGrid).ItemsSource = Clients;
                MainViewModel.Log($"Удален клиент {client}");
                Client = null;
            }
        }
        private void EditClient(object commandParameter)
        {
            if (client == null)
                return;
            if (client.DepID != default)
            {
                MainViewModel.Log($"Имя клиента {Client} отредактировано.");
                return;
            }
            if ((bool)new DepsDialog { DataContext = this }.ShowDialog() && Dep != null)
                AddClientTo(Dep);
        }
        private void AddClientTo(Dep dep)
        {
            client.DepID = dep.ID;
            dep.Clients.Add(client);
            RaisePropertyChanged(nameof(Client));
            MainViewModel.Log($"В отдел {dep} добавлен клиент {client}.");
        }
    }
}