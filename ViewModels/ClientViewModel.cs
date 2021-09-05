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
                if (MainViewModel.DBMode)
                {
                    if ((DataRowView = (e as DataGrid).SelectedItem is DataRowView rowView ? rowView : null) != null)
                        Client = DataRowView.Row.RowState == DataRowState.Detached ? new Client() :
                        Clients.First((g) => dataRowView.Row.Field<Guid>("Id") == g.ID);
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
            if (MainViewModel.DBMode)
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
                if (MainViewModel.DBMode)
                {
                    new SqlCommand($"delete from {depositsTable} where '{dataRowView.Row.Field<Guid>("Id")}' = ClientId ", App.SqlConnection).ExecuteNonQuery();
                    adapter.Update(depositsTable);
                    new SqlCommand($"delete from {loansTable} where '{dataRowView.Row.Field<Guid>("Id")}' = ClientId ", App.SqlConnection).ExecuteNonQuery();
                    adapter.Update(loansTable);
                    adapter.DeleteCommand = new SqlCommand($"delete from {clientsTable} where '{dataRowView.Row.Field<Guid>("Id")}' = Id ", App.SqlConnection);
                    dataRowView.Delete();
                    adapter.Update(clientsTable);
                }
                else
                    (e as DataGrid).ItemsSource = Clients;
                MainViewModel.Log($"Удален клиент {client}");
                Client = null;
            }
        }
        private void EditClient(object e)
        {
            if (client == null)
                return;
            if (client.DepID != default)
            {
                if (MainViewModel.DBMode)
                {
                    new SqlCommand($"update {clientsTable} set Name = '{client.Name}', DepId = '{client.DepID}' where Id = '{client.ID}'", App.SqlConnection)
                    .ExecuteNonQuery();
                    (e as DataGrid).ItemsSource = DataView;
                    RaisePropertyChanged(nameof(DataRowView));
                }
                MainViewModel.Log($"Имя клиента {Client} отредактировано.");
                return;
            }
            if ((bool)new DepsDialog { DataContext = this }.ShowDialog() && Dep != null)
                AddClientTo(e, Dep);
        }
        private void AddClientTo(object e, Dep dep)
        {
            client.DepID = dep.ID;
            dep.Clients.Add(client);
            if (MainViewModel.DBMode)
            {
                new SqlCommand($"insert into {clientsTable} values ('{client.ID}', '{client.Name}', '{client.DepID}')", App.SqlConnection).ExecuteNonQuery();
                (e as DataGrid).ItemsSource = DataView;
                RaisePropertyChanged(nameof(DataRowView));
            }
            RaisePropertyChanged(nameof(Client));
            MainViewModel.Log($"В отдел {dep} добавлен клиент {client}.");
        }

        //private RelayCommand cellChangedCommand;
        //public ICommand CellChangedCommand => cellChangedCommand ?? (cellChangedCommand = new RelayCommand(CellChanged));
        //private void CellChanged(object e)
        //{
        //    //if (DataRowView == null)
        //    //    return;
        //    //DataRowView.EndEdit();
        //    //adapter.Update(clientsTable);
        //    //(e as DataGrid).ItemsSource = DataView;
        //}
    }
}