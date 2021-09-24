using ClassLibrary;
using System;
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
        #region Поля общего назначения
        /// <summary>
        /// Хранит ссылку на текущий банк.
        /// </summary>
        private readonly Bank bank;
        /// <summary>
        /// Хранит индекс отдела по умолчанию, в который добавляется клиент.
        /// </summary>
        private readonly int DepClientAddToDefault = 0;
        /// <summary>
        /// Хранит ссылку на текущего клиента.
        /// </summary>
        private Client client;
        /// <summary>
        /// Хранит ссылку на отдел к которому приписан вновь создаваемый клиент.
        /// </summary>
        private Dep dep;
        /// <summary>
        /// Хранит ссылку на таблицу клиентов.
        /// </summary>
        private readonly DataTable clientsTable;
        /// <summary>
        /// Хранит ссылку на таблицу депозитов.
        /// </summary>
        private readonly DataTable depositsTable;
        /// <summary>
        /// Хранит ссылку на таблицу кредитов.
        /// </summary>
        private readonly DataTable loansTable;
        /// <summary>
        /// Хранит ссылку на адаптер связи с базой данных.
        /// </summary>
        private readonly SqlDataAdapter adapter;
        /// <summary>
        /// Хранит ссылку на текущее содержимое таблицы клиентов.
        /// </summary>
        private DataView dataView;
        /// <summary>
        /// Хранит ссылку на содержимое текущей строки таблицы клиентов.
        /// </summary>
        private DataRowView dataRowView;
        /// <summary>
        /// Хранит ссылку на текущий источник данных таблицы клиентов - dataView или Clients в зависимости от режима MainViewModel.DBMode.
        /// </summary>
        private object dataSource;
        /// <summary>
        /// Хранит флаг, определяющий действия по сохранению данных в базе - добавление новой записи true, редактирование выбранной записи false, отказ от изменения null.
        /// </summary>
        private bool? added = null;
        #endregion
        #region Ссылки на команды, управляющие обработчиками событий.
        private RelayCommand selectionChangedCommand;
        private RelayCommand removeClientCommand;
        private RelayCommand cellEditEndingCommand;
        private RelayCommand cellChangedCommand;
        private RelayCommand oKDepartmentCommand;
        private RelayCommand depSelDefaultCommand;
        #endregion
        #endregion
        #region Properties
        #region Свойства общего назначения.
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
        /// <summary>
        /// Возвращает отдел банка.
        /// </summary>
        public ObservableCollection<Dep> Deps { get => bank.Deps; }
        /// <summary>
        /// Устанавливает и возвращает текущего клиента.
        /// </summary>
        public Client Client { get => client; set { client = value; RaisePropertyChanged(nameof(Client)); } }
        /// <summary>
        /// Возвращает и устанавливает отдел банка, к которому приписываетя вновь создаваемый клиент.
        /// </summary>
        public Dep Dep { get => dep; set { dep = value; RaisePropertyChanged(nameof(Dep)); } }
        /// <summary>
        /// Устанавливает и возвращает ссылку на содержимое таблицы.
        /// </summary>
        public DataView DataView { get => dataView; set { dataView = value; RaisePropertyChanged(nameof(DataView)); } }
        /// <summary>
        /// Устанавливает и возвращает ссылку на текущий источник данных в таблице. Это либо DataView в режиме DBMode, либо Clients. 
        /// </summary>
        public object DataSource { get => dataSource; set { dataSource = value; RaisePropertyChanged(nameof(DataSource)); } }
        /// <summary>
        /// Устанавливает и возвращает ссылку на выбранную строку в содержимом таблицы DataView.
        /// </summary>
        public DataRowView DataRowView { get => dataRowView; set { dataRowView = value; RaisePropertyChanged(nameof(DataRowView)); } }
        #endregion
        #region Команды управления обработчиками событий.
        public ICommand SelectionChangedCommand => selectionChangedCommand ?? (selectionChangedCommand = new RelayCommand(SelectClient));
        public ICommand RemoveClientCommand => removeClientCommand ?? (removeClientCommand = new RelayCommand(RemoveClient));
        public ICommand CellEditEndingCommand => cellEditEndingCommand ?? (cellEditEndingCommand = new RelayCommand(EditClient));
        public ICommand CellChangedCommand => cellChangedCommand ?? (cellChangedCommand = new RelayCommand(CellChanged));
        #region Команды выбора отдела, к которому приписывается вновь создаваемый клиент.
        public ICommand OKDepartmentCommand => oKDepartmentCommand ?? (oKDepartmentCommand = new RelayCommand((e) =>
        {
            DepsDialog dialog = e as DepsDialog;
            Dep = dialog.depListBox.SelectedItem as Dep;
            dialog.DialogResult = true;
        }));
        public ICommand DepSelDefaultCommand
            => depSelDefaultCommand ?? (depSelDefaultCommand = new RelayCommand((e) => ((ListBox)e).SelectedItem = ((ListBox)e).Items[DepClientAddToDefault]));
        #endregion
        #endregion
        #endregion
        #region Methods
        #region Constructors
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
        #endregion
        #region Handlers
        private void SelectClient(object e)
        {
            //MessageBox.Show("Selection Changed\nadded = " + (added == null ? "null" : $"{added}"));
            // Получаем ссылку на строку, выбранную в DataGrid.
            object selItem = (e as DataGrid).SelectedItem;
            // Фильтруем ссылку.
            if (selItem == null || selItem.ToString() == "{NewItemPlaceholder}")
                return;
            // Определяем текущую строку DataRowView в режиме DBMode и текущего клиента Client в любом режиме.
            Client = MainViewModel.DBMode ? (DataRowView = (DataRowView)selItem).IsNew ? new Client() :
                    Clients.First((g) => DataRowView.Row.Field<Guid>("Id") == g.ID) : (Client)selItem;

        }
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
            //MessageBox.Show("CellEditEnding\nadded = " + (added == null ? "null" : $"{added}"));
            void InsertClientIntoDepartment()
            {
                client.DepID = dep.ID;
                dep.Clients.Add(client);
                added = true;
                RaisePropertyChanged(nameof(Client));
                MainViewModel.Log($"В отдел {dep} будет добавлен клиент {client}.");
            }
            if (client.DepID != default)
            {
                added = false;
                MainViewModel.Log($"Имя клиента {Client} будет отредактировано.");
                return;
            }
            // По умолчанию клиент добавляется в отдел:
            Dep = Deps[DepClientAddToDefault];
            // Показываем список отделов в диалоговом рeжиме.
            _ = new DepsDialog { DataContext = this }.ShowDialog();
            // Добавляем нового клиента в выбранный отдел или в отдел по умолчанию.
            InsertClientIntoDepartment();
        }
        private void CellChanged(object e)
        {
            //MessageBox.Show("CurrentCellChanged\nadded = " + (added == null ? "null" : $"{added}"));
            if (added == null)
                return;
            void DBChanged()
            {
                if ((e as DataGrid).CurrentColumn == null)
                    return;
                void SetNewRowView()
                {
                    int lastRowIndex = clientsTable.Rows.Count - 1;
                    clientsTable.Rows[lastRowIndex][0] = client.ID;
                    clientsTable.Rows[lastRowIndex][1] = client.Name;
                    clientsTable.Rows[lastRowIndex][2] = client.DepID;
                    clientsTable.AcceptChanges();
                    DataSource = DataView = clientsTable.DefaultView;
                }
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
                            SetNewRowView();
                            // Добавляем строку в таблицу БД. 
                            new SqlCommand($"insert into {clientsTable} values ('{client.ID}', '{client.Name}', '{client.DepID}')", connection).ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        throw;
                    }
            }
            if (MainViewModel.DBMode)
                DBChanged();
            string comment = added.Value ? $"В отдел {dep} добавлен клиент {client}" : $"Клиент {client} отредактирован";
            MainViewModel.Log(comment);
            //MessageBox.Show(comment);
            added = null;
        }
        #endregion
        #endregion
    }
}