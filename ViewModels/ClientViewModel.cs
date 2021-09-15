using ClassLibrary;
using System;
using System.CodeDom;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WpfBank.Commands;

namespace WpfBank.ViewModels
{
    public class ClientViewModel : ViewModelBase
    {
        #region Fields
        private readonly Bank bank;
        /// <summary>
        /// Хранит индекс отдела по умолчанию, в который добавляется клиент.
        /// </summary>
        private readonly int DepClientAddToDefault = 0;
        private Client client;
        private Dep dep;
        private RelayCommand selCommand;
        private RelayCommand removeClientCommand;
        private RelayCommand addClientCommand;
        private RelayCommand oKCommand;
        private RelayCommand endClientEditCommand;
        private RelayCommand cellChangedCommand;
        private RelayCommand depSelDefaultCommand;
        private DataTable clientsTable, depositsTable, loansTable;
        private SqlDataAdapter adapter;
        private DataView dataView;
        private DataRowView dataRowView;
        private bool? added = null;
        private object dataSource;
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
                        foreach (Account loan in client.Loans)
                        {
                            loans.Add(loan);
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
                        foreach (Account deposit in client.Deposits)
                        {
                            deposits.Add(deposit);
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
                // Получаем ссылку на строку, выбранную в DataGrid.
                object selItem = (e as DataGrid).SelectedItem;
                // Фильтруем ссылку.
                if (selItem == null || selItem.ToString() == "{NewItemPlaceholder}")
                    return;
                // Определяем текущую строку DataRowView в режиме DBMode и текущего клиента Client в любом режиме.
                Client = MainViewModel.DBMode ? (DataRowView = (DataRowView)selItem).IsNew ? new Client() :
                Clients.First((g) => DataRowView.Row.Field<Guid>("Id") == g.ID) : (Client)selItem;
            }));
        public ICommand RemoveClientCommand => removeClientCommand ?? (removeClientCommand = new RelayCommand(RemoveClient));
        public ICommand AddClientCommand => addClientCommand ?? (addClientCommand = new RelayCommand(EditClient));
        public ICommand OKCommand => oKCommand ?? (oKCommand = new RelayCommand((e) =>
        {
            DepsDialog dialog = e as DepsDialog;
            Dep = dialog.depListBox.SelectedItem as Dep;
            dialog.DialogResult = true;
        }));
        public ICommand DepSelDefaultCommand
            => depSelDefaultCommand ?? (depSelDefaultCommand = new RelayCommand((e) => ((ListBox)e).SelectedItem = ((ListBox)e).Items[DepClientAddToDefault]));
        public ICommand EndClientEditCommand => endClientEditCommand ?? (endClientEditCommand = new RelayCommand(EditClient));
        public ICommand CellChangedCommand => cellChangedCommand ?? (cellChangedCommand = new RelayCommand(CellChanged));
        public DataView DataView { get => dataView; set { dataView = value; RaisePropertyChanged(nameof(DataView)); } }
        public object DataSource { get => dataSource; set { dataSource = value; RaisePropertyChanged(nameof(DataSource)); } }
        public DataRowView DataRowView { get => dataRowView; set { dataRowView = value; RaisePropertyChanged(nameof(DataRowView)); } }
        #endregion
        public ClientViewModel(Bank bank) : this()
        {
            using (SqlConnection connection = new SqlConnection() { ConnectionString = App.ConnectionString })
                try
                {
                    connection.Open();
                    clientsTable = new DataTable("Clients");
                    adapter = new SqlDataAdapter(new SqlCommand() { CommandText = "select * from [Clients]", Connection = connection });
                    adapter.Fill(clientsTable);
                    depositsTable = new DataTable("Deposits");
                    adapter.SelectCommand = new SqlCommand() { CommandText = "select * from [Deposits]", Connection = connection };
                    adapter.Fill(depositsTable);
                    loansTable = new DataTable("Loans");
                    adapter.SelectCommand = new SqlCommand() { CommandText = "select * from [Loans]", Connection = connection };
                    adapter.Fill(loansTable);
                }
                catch
                {
                    throw;
                }
            DataView = clientsTable.DefaultView;
            this.bank = bank;
            if (MainViewModel.DBMode)
            {
                DataSource = DataView;
            }
            else
            {
                DataSource = Clients;
            }
        }
        public ClientViewModel() { }
        private void RemoveClient(object e)
        {
            if (client == null || MessageBox.Show("Удалить клиента?", "Удаление клиента " + client.Name, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (Account deposit in Deposits.Where((g) => g.ClientID == client.ID))
            {
                Deposits.Remove(deposit);
            }

            RaisePropertyChanged(nameof(Deposits));
            foreach (Account loan in Loans.Where((g) => g.ClientID == client.ID))
            {
                _ = Loans.Remove(loan);
            }

            RaisePropertyChanged(nameof(Loans));
            _ = bank.Deps.First((g) => client.DepID == g.ID).Clients.Remove(client);
            RaisePropertyChanged(nameof(Clients));
            if (MainViewModel.DBMode)
            {
                using (SqlConnection connection = new SqlConnection() { ConnectionString = App.ConnectionString })
                {
                    try
                    {
                        connection.Open();
                        new SqlCommand($"delete from {depositsTable} where '{dataRowView.Row.Field<Guid>("Id")}' = ClientId ", connection).ExecuteNonQuery();
                        adapter.Update(depositsTable);
                        new SqlCommand($"delete from {loansTable} where '{dataRowView.Row.Field<Guid>("Id")}' = ClientId ", connection).ExecuteNonQuery();
                        adapter.Update(loansTable);
                        adapter.DeleteCommand = new SqlCommand($"delete from {clientsTable} where '{dataRowView.Row.Field<Guid>("Id")}' = Id ", connection);
                        dataRowView.Delete();
                        adapter.Update(clientsTable);
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            else
            {
                (e as DataGrid).ItemsSource = Clients;
            }

            MainViewModel.Log($"Удален клиент {client}");
            Client = null;
        }
        private void EditClient(object e)
        {
            if (client.DepID != default)
            {
                added = false;
                MainViewModel.Log($"Имя клиента {Client} будет отредактировано.");
                return;
            }
            // По умолчанию клиент добавляется в отдел:
            Dep = Deps[DepClientAddToDefault];
            // Показываем список отделов в диалоговом рeжиме.
            new DepsDialog { DataContext = this }.ShowDialog();
            // Добавляем нового клиента в выбранный отдел или в отдел по умолчанию.
            AddClientTo(e, Dep);
        }
        private void AddClientTo(object e, Dep dep)
        {
            client.DepID = dep.ID;
            dep.Clients.Add(client);
            added = true;
            RaisePropertyChanged(nameof(Client));
            MainViewModel.Log($"В отдел {dep} будет добавлен клиент {client}.");
        }
        void SetNewRowView(Client client)
        {
            int lastRowIndex = clientsTable.Rows.Count - 1;
            clientsTable.Rows[lastRowIndex][0] = client.ID;
            clientsTable.Rows[lastRowIndex][1] = client.Name;
            clientsTable.Rows[lastRowIndex][2] = client.DepID;
            clientsTable.AcceptChanges();
            DataSource = DataView = clientsTable.DefaultView;
        }
        private void DBChanged(object e)
        {
            if ((e as DataGrid).CurrentColumn == null)
                return;
            // Определяем имя поля, с которым связан текущий столбец, ячейка которого изменена.
            string columnName = ((e as DataGrid).CurrentColumn.ClipboardContentBinding as Binding).Path.Path;
            // Свойству columnName объекта класса Client присваиваем значение, кторое возвращает
            // расширяющий обобщенный метод dataRowView.Row.Field<T>(columnName).
            // Тип T является типом поля столбца columnName, т.е. dataRowView.Row.Table.Columns[columnName].DataType.
            typeof(Client).GetProperty(columnName).SetValue(Client,
                // Вызываем метод dataRowView.Row.Field<T>(columnName), где тип T есть dataRowView.Row.Table.Columns[columnName].DataType
                // Определяем MethodInfo метода Field
                typeof(DataRowExtensions).GetMethod("Field", new Type[] { typeof(DataRow), typeof(string) }).
                // Создаем статический метод Field<T> класса DataRowExtension, где тип T есть dataRowView.Row.Table.Columns[columnName].DataType,
                // и вызываем его над объектом dataRowView.Row с параметром columnName
                MakeGenericMethod(dataRowView.Row.Table.Columns[columnName].DataType).Invoke(null, new object[] { dataRowView.Row, columnName }));
            using (SqlConnection connection = new SqlConnection() { ConnectionString = App.ConnectionString })
                try
                {
                    connection.Open();
                    if (!added.Value)
                        new SqlCommand($"update {clientsTable} set Name = '{client.Name}', DepId = '{client.DepID}' where Id = '{client.ID}'", connection).ExecuteNonQuery();
                    else
                    {
                        // Обновляем новую строку в DataView.
                        SetNewRowView(client);
                        // Добавляем строку в таблицу БД. 
                        new SqlCommand($"insert into {clientsTable} values ('{client.ID}', '{client.Name}', '{client.DepID}')", connection).ExecuteNonQuery();
                    }
                }
                catch
                {
                    throw;
                }
        }
        private void CellChanged(object e)
        {
            if (added == null)
                return;
            if (MainViewModel.DBMode)
                DBChanged(e);
            string comment = added.Value ? $"В отдел {dep} добавлен клиент {client}" : $"Клиент {client} отредактирован";
            MainViewModel.Log(comment);
            MessageBox.Show(comment);
            added = null;
        }
    }
}