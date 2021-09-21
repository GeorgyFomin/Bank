using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfBank.Commands;
using ClassLibrary;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using WpfBank.Dialogs;
using System.Windows;
using System.Linq;
using System.Data.SqlClient;
using System.Data;
using System.Windows.Data;
using System;
using System.Globalization;

namespace WpfBank.ViewModels
{
    public class DepositViewModel : ViewModelBase
    {
        #region Fields
        #region Поля общего назначения
        /// <summary>
        /// Хранит ссылку на текущий банк.
        /// </summary>
        private readonly Bank bank;
        /// <summary>
        /// Хранит ссылку на текущий адаптер связи с базой данных.
        /// </summary>
        private readonly SqlDataAdapter adapter;
        /// <summary>
        /// Хранит ссылку на текущий депозит.
        /// </summary>
        private Account depo;
        /// <summary>
        /// Хранит ссылку на клиента, которому открыт новый депозит.
        /// </summary>
        private Client client;
        /// <summary>
        /// Хранит ссылку на таблицу с депозитами.
        /// </summary>
        private DataTable depositsTable;
        /// <summary>
        /// Хранит ссылку на текущее содержимое таблицы депозитов.
        /// </summary>
        private DataView dataView;
        /// <summary>
        /// Хранит ссылку на содержимое текущей строки таблицы депозитов.
        /// </summary>
        private DataRowView dataRowView;
        /// <summary>
        /// Хранит ссылку на информацию об отредактированной ячейки таблицы депозитов.
        /// </summary>
        private DataGridCellInfo editedCell;
        /// <summary>
        /// Хранит ссылку на отредактированную колонку таблицы депозитов.
        /// </summary>
        private DataGridColumn editedColumn;
        /// <summary>
        /// Хранит ссылку на текущую колонку таблицы депозитов.
        /// </summary>
        private DataGridColumn curColumn;
        /// <summary>
        /// Хранит ссылку на текущий источник данных таблицы депозитов - dataView или Deposits в зависимости от режима MainViewModel.DBMode.
        /// </summary>
        private object dataSource;
        /// <summary>
        /// Хранит флаг, определяющий действия по сохранению данных в базе - добавление новой записи true, редактирование выбранной записи false, отказ от изменения null.
        /// </summary>
        private bool? added = null;
        /// <summary>
        /// Хранит флаг, определяющий состояние редактирования выбранной записи - отредактированная true, нет false.
        /// </summary>
        private bool endEdited;
        /// <summary>
        /// Хранит флаг, определяющий состояние выборки из списка клиента открываемого депозита.
        /// </summary>
        private bool clientDoSelected;
        /// <summary>
        /// Хранит флаг, определяющий состояние выборки депозита, из которого будут переведены средства.
        /// </summary>
        private bool sourceTransferDepoSelected;
        /// <summary>
        /// Хранит флаг, определяющий доступность совершения транзакции.
        /// </summary>
        private bool transferEnabled;
        /// <summary>
        /// Хранит флаг, подтверждающий транзакцию указанной суммы.
        /// </summary>
        private bool transferSumOKEnabled;
        /// <summary>
        /// Хранит ссылку на депозит, на который переводятся средства.
        /// </summary>
        private Account targetTransferDepo;
        /// <summary>
        /// Хранит величину перевода.
        /// </summary>
        private decimal transferAmount;
        /// <summary>
        /// Хранит флаг активации списка депозитов, на который могут быть переведены средства.
        /// </summary>
        private bool targetTransferListEnabled;
        #endregion
        #region Команды управления событиями
        private RelayCommand selectionChangedCommand;
        private RelayCommand removeDepoCommand;
        private RelayCommand depoEditEndingCommand;
        private RelayCommand cellChangedCommand;
        private RelayCommand clientSelectedCommand;
        private RelayCommand oKClientSelectionCommand;
        private RelayCommand transferCommand;
        private RelayCommand targetTransferDepoSelectionChangedCommand;
        private RelayCommand oKTransferCommand;
        private RelayCommand transfAmountChangedCommand;
        #endregion
        #endregion
        #region Properties
        #region Свойства общего назначения
        /// <summary>
        /// Возвращает список всех депозитов банка.
        /// </summary>
        public ObservableCollection<Account> Deposits
        {
            get
            {
                ObservableCollection<Account> deposits = new ObservableCollection<Account>();
                foreach (Dep dep in bank.Deps)
                    foreach (Client client in dep.Clients)
                        foreach (Account deposit in client.Deposits)
                        {
                            // Блокируем появление в списке депозитов, на которые могут быть переведены средства, депозит Depo, с которого средства списываются.
                            if (!targetTransferListEnabled || deposit.ID != Depo?.ID)
                                deposits.Add(deposit);
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
                    foreach (Client client in dep.Clients)
                        clients.Add(client);
                return clients;
            }
        }
        /// <summary>
        /// Устанавливает и возвращает ссылку на содержимое таблицы.
        /// </summary>
        public DataView DataView { get => dataView; set { dataView = value; RaisePropertyChanged(nameof(DataView)); } }
        /// <summary>
        /// Устанавливает и возвращает ссылку на текущий источник данных в таблице. Это либо DataView в режиме DBMode, либо Deposits. 
        /// </summary>
        public object DataSource { get => dataSource; set { dataSource = value; RaisePropertyChanged(nameof(DataSource)); } }
        /// <summary>
        /// Устанавливает и возвращает ссылку на выбранную строку в содержимом таблицы DataView.
        /// </summary>
        public DataRowView DataRowView { get => dataRowView; set { dataRowView = value; RaisePropertyChanged(nameof(DataRowView)); } }
        /// <summary>
        /// Устанавливает и возвращает ссылку на текущий депозит.
        /// </summary>
        public Account Depo { get => depo; set { depo = value; RaisePropertyChanged(nameof(Depo)); } }
        /// <summary>
        /// Возвращает и устанавливает клиента, которому приписываетя вновь открываемый депозит.
        /// </summary>
        public Client Client { get => client; set { client = value; RaisePropertyChanged(nameof(Client)); } }
        /// <summary>
        /// Хранит и возвращает флаг выбора клиента, которому приписываетя вновь открываемый депозит.
        /// </summary>
        public bool ClientDoSelected { get => clientDoSelected; set { clientDoSelected = value; RaisePropertyChanged(nameof(ClientDoSelected)); } }
        #region Transfer properties
        public bool SourceTransferDepoSelected { get => sourceTransferDepoSelected; set { sourceTransferDepoSelected = value; RaisePropertyChanged(nameof(SourceTransferDepoSelected)); } }
        public Account TargetTransferDepo { get => targetTransferDepo; set { targetTransferDepo = value; RaisePropertyChanged(nameof(TargetTransferDepo)); } }
        public bool TransferEnabled { get => transferEnabled; set { transferEnabled = value; RaisePropertyChanged(nameof(TransferEnabled)); } }
        public bool TransferSumOKEnabled { get => transferSumOKEnabled; set { transferSumOKEnabled = value; RaisePropertyChanged(nameof(TransferSumOKEnabled)); } }
        public decimal TransferAmount { get => transferAmount; set { transferAmount = value; RaisePropertyChanged(nameof(TransferAmount)); } }
        #endregion
        #endregion
        #region Команды обработки событий
        public ICommand SelectionChangedCommand => selectionChangedCommand ?? (selectionChangedCommand = new RelayCommand(SelectDepo));
        public ICommand RemoveDepoCommand => removeDepoCommand ?? (removeDepoCommand = new RelayCommand(RemoveDepo));
        public ICommand DepoEditEndingCommand => depoEditEndingCommand ?? (depoEditEndingCommand = new RelayCommand(EditDeposit));
        public ICommand CellChangedCommand => cellChangedCommand ?? (cellChangedCommand = new RelayCommand(CellChanged));
        #region Команды выбора клиента для вновь открываемого депозита.
        public ICommand ClientSelectedCommand => clientSelectedCommand ?? (clientSelectedCommand = new RelayCommand((e) => ClientDoSelected = true));
        public ICommand OKClientSelectionCommand => oKClientSelectionCommand ?? (oKClientSelectionCommand = new RelayCommand(SelectNewDepositClient));
        #endregion
        #region Команды перечисления средств с одного депозита на другой.
        public ICommand TransferCommand => transferCommand ?? (transferCommand = new RelayCommand(Transfer));
        public ICommand TransfAmountChangedCommand => transfAmountChangedCommand ?? (transfAmountChangedCommand = new RelayCommand(TransfAmountChanged));
        public ICommand OKTransferCommand => oKTransferCommand ?? (oKTransferCommand = new RelayCommand((e) => (e as TransferDialog).DialogResult = true));
        public ICommand TargetTransferDepoSelectionChangedCommand => targetTransferDepoSelectionChangedCommand ??
            (targetTransferDepoSelectionChangedCommand = new RelayCommand(SelectTargetTransferDepo));
        #endregion
        #endregion
        #endregion
        #region Methods
        #region Constructors
        public DepositViewModel(Bank bank) : this()
        {
            using (SqlConnection connection = new SqlConnection() { ConnectionString = App.ConnectionString })
                try
                {
                    connection.Open();
                    adapter = new SqlDataAdapter(new SqlCommand() { CommandText = "select * from [Deposits]", Connection = connection });
                    depositsTable = new DataTable("Deposits");
                    adapter.Fill(depositsTable);
                }
                catch
                {
                    throw;
                }
            DataView = depositsTable.DefaultView;
            this.bank = bank;
            if (MainViewModel.DBMode)
                DataSource = DataView;
            else
                DataSource = Deposits;
        }
        public DepositViewModel() { sourceTransferDepoSelected = transferEnabled = clientDoSelected = false; }
        #endregion
        #region Handlers
        private void SelectDepo(object e)
        {
            //MessageBox.Show("Selection Changed\n" +
            //    "Edited Column: " + (editedColumn != null ? (editedColumn.ClipboardContentBinding as Binding).Path.Path : "null") +
            //    "\nCur Column: " + (curColumn != null ? (curColumn.ClipboardContentBinding as Binding).Path.Path : "null") +
            //    "\nadded = " + (added == null ? "null" : $"{added}"));
            // Получаем ссылку строку, выбранную в DataGrid.
            object selItem = (e as DataGrid).SelectedItem;
            // Фильтруем ссылку.
            if (selItem == null || selItem.ToString() == "{NewItemPlaceholder}")
                return;
            // Определяем текущую строку DataRowView в режиме DBMode и текущий клиента Deposit в любом режиме.
            Depo = MainViewModel.DBMode ?
            (DataRowView = (DataRowView)selItem).IsNew ? new Account() : Deposits.First((g) => DataRowView.Row.Field<Guid>("Id") == g.ID) :
            (Account)selItem;
            SourceTransferDepoSelected = true;
        }
        private void RemoveDepo(object obj)
        {
            if (depo == null || MessageBox.Show($"Удалить депозит №{depo.Number}?", $"Удаление депозита №{depo.Number}", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;
            Clients.First((g) => depo.ClientID == g.ID).Deposits.Remove(depo);
            RaisePropertyChanged(nameof(Deposits));
            MainViewModel.Log($"Удален депозит №{depo.Number}");
            if (MainViewModel.DBMode)
                using (SqlConnection connection = new SqlConnection() { ConnectionString = App.ConnectionString })
                    try
                    {
                        connection.Open();
                        adapter.DeleteCommand = new SqlCommand($"delete from {depositsTable} where '{dataRowView.Row.Field<int>("Number")}' = Number", connection);
                        dataRowView.Delete();
                        adapter.Update(depositsTable);
                    }
                    catch
                    {
                        throw;
                    }
            else
                (obj as DataGrid).ItemsSource = Deposits;
            Depo = null;
        }
        private void EditDeposit(object e)
        {
            //MessageBox.Show("CellEditEnding\n" +
            //    "Edited Column: " + (editedColumn != null ? (editedColumn.ClipboardContentBinding as Binding).Path.Path : "null") +
            //    "\nCur Column: " + (curColumn != null ? (curColumn.ClipboardContentBinding as Binding).Path.Path : "null") +
            //    "\nadded = " + (added == null ? "null" : $"{added}"));

            void InsertDepoIntoClient()
            {
                depo.ClientID = client.ID;
                client.Deposits.Add(depo);
                RaisePropertyChanged(nameof(Depo));
                MainViewModel.Log($"Клиенту {client} будет открыт депозит {depo}.");
                added = true;
            }
            if (depo.ClientID != default)
            {
                added = false;
                MainViewModel.Log($"Поля депозита №{depo.Number} будут отредактированы.");
            }
            else
            {
                bool flag;
                do
                {
                    if (flag = (bool)new ClientsDialog { DataContext = this }.ShowDialog() && client != null)
                        InsertDepoIntoClient();
                } while (!flag);
            }
            endEdited = true;
        }
        private void CellChanged(object e)
        {
            string DBChanged()
            {
                void SetNewRowView()
                {
                    int lastRowIndex = depositsTable.Rows.Count - 1;
                    depositsTable.Rows[lastRowIndex][0] = depo.ID;
                    depositsTable.Rows[lastRowIndex][1] = depo.ClientID;
                    depositsTable.Rows[lastRowIndex][2] = depo.Number;
                    depositsTable.Rows[lastRowIndex][3] = depo.Size;
                    depositsTable.Rows[lastRowIndex][4] = depo.Rate;
                    depositsTable.Rows[lastRowIndex][5] = depo.Cap;
                    depositsTable.AcceptChanges();
                    DataSource = DataView = depositsTable.DefaultView;
                }
                // Определяем имя поля, с которым связан столбец с отредактированной ячейкой.
                string editedColumnName = (editedColumn.ClipboardContentBinding as Binding).Path.Path;
                // Свойству columnName объекта класса Deposit присваиваем значение, кторое возвращает
                // расширяющий обобщенный метод dataRowView.Row.Field<T>(editedColumnName).
                // Тип T является типом поля столбца columnName, т.е. dataRowView.Row.Table.Columns[editedColumnName].DataType.
                typeof(Account).GetProperty(editedColumnName).SetValue(Depo,
                    // Вызываем метод dataRowView.Row.Field<T>(columnName), где тип T есть dataRowView.Row.Table.Columns[editedColumnName].DataType
                    // Для этого получаем MethodInfo метода Field,
                    typeof(DataRowExtensions).GetMethod("Field", new Type[] { typeof(DataRow), typeof(string) }).
                    // создаем статический метод Field<T> класса DataRowExtension, где тип T есть dataRowView.Row.Table.Columns[editedColumnName].DataType,
                    // и вызываем его над объектом dataRowView.Row с параметром columnName
                    MakeGenericMethod(dataRowView.Row.Table.Columns[editedColumnName].DataType).Invoke(null, new object[] { dataRowView.Row, editedColumnName }));
                using (SqlConnection connection = new SqlConnection() { ConnectionString = App.ConnectionString })
                    try
                    {
                        connection.Open();
                        if (!added.Value)
                        {
                            new SqlCommand($"update {depositsTable} set Size = " + depo.Size.ToString(CultureInfo.InvariantCulture) +
                                ", Rate = " + depo.Rate.ToString(CultureInfo.InvariantCulture) + ", Cap = " + (depo.Cap ? "1" : "0") +
                                $"where Id = '{depo.ID}'", connection).ExecuteNonQuery();
                        }
                        else
                        {
                            SetNewRowView();
                            new SqlCommand($"insert into {depositsTable} values ('{depo.ID}', '{depo.ClientID}', {depo.Number}, " +
                            depo.Size.ToString(CultureInfo.InvariantCulture) + ", " + depo.Rate.ToString(CultureInfo.InvariantCulture) + (depo.Cap ? ", 1" : ", 0") + ")",
                            connection).ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        throw;
                    }
                return editedColumnName;
            }
            string comment;
            if (MainViewModel.DBMode)
            {
                DataGrid dataGrid = e as DataGrid;
                DataGridCellInfo cell = dataGrid.CurrentCell;
                curColumn = cell.Column;
                editedColumn = editedCell.Column;
                //MessageBox.Show("CurrentCellChanged\n" +
                //    "Edited Column: " + (editedColumn != null ? (editedColumn.ClipboardContentBinding as Binding).Path.Path : "null") +
                //    "\nCur Column: " + (curColumn != null ? (curColumn.ClipboardContentBinding as Binding).Path.Path : "null") +
                //    $"\nEndEdited = {endEdited}" +
                //    "\nadded = " + (added == null ? "null" : $"{added}"));
                if (curColumn == null)
                {
                    endEdited = false; editedCell = cell; added = null;
                    return;
                }
                // Отфильтровываем смену ячейки с переходом в другой столбец.
                if (endEdited && cell != editedCell && cell.Item.ToString() != "{NewItemPlaceholder}" && (DataRowView)cell.Item == DataRowView)
                {
                    editedCell = cell;
                    endEdited = false;
                    if (added == null) return;
                    dataGrid.CommitEdit();
                    //MessageBox.Show("После редактирования ячейки надо нажать Enter или перейти на следующую строку.");
                    //depositsTable.AcceptChanges();
                    //RaisePropertyChanged(nameof(DataSource));
                    //added = null;
                    //return;
                }
                editedCell = cell;
                if (added == null)
                    return;
                comment = DBChanged();
                comment = added.Value ? $"Клиенту {client} открыт депозит {depo}" : "Поле " + comment + $" депозита №{depo.Number} отредактировано.";
            }
            else
            {
                if (added == null)
                    return;
                comment = added.Value ? $"Клиенту {client} открыт депозит {depo}" : $"Поля депозита №{depo.Number} отредактированы.";
            }
            MainViewModel.Log(comment);
            MessageBox.Show(comment);
            added = null;
        }
        private void SelectNewDepositClient(object e)
        {
            ClientsDialog dialog = e as ClientsDialog;
            Client = dialog.clientListBox.SelectedItem is Client client ? client : null;
            dialog.DialogResult = true;
        }
        private void Transfer(object e)
        {
            void TryToTransfer()
            {
                void DoTransfer()
                {
                    if (MessageBox.Show($"Вы действительно хотите перевести со счета №{depo.Number} на счет №{targetTransferDepo.Number} сумму {TransferAmount}?",
                        "Перевод между счетами", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        return;
                    depo.Size -= TransferAmount;
                    TargetTransferDepo.Size += TransferAmount;
                    MainViewModel.Log($"Со счета {depo} будет переведено {TransferAmount} на счет {targetTransferDepo}");
                    if (MainViewModel.DBMode)
                    {
                        using (SqlConnection connection = new SqlConnection() { ConnectionString = App.ConnectionString })
                            try
                            {
                                connection.Open();
                                new SqlCommand($"update {depositsTable} set Size = " + depo.Size.ToString(CultureInfo.InvariantCulture) +
                                    ", Rate = " + depo.Rate.ToString(CultureInfo.InvariantCulture) + ", Cap = " + (depo.Cap ? "1" : "0") +
                                    $"where Id = '{depo.ID}'", connection).ExecuteNonQuery();
                                new SqlCommand($"update {depositsTable} set Size = " + targetTransferDepo.Size.ToString(CultureInfo.InvariantCulture) +
                                    ", Rate = " + targetTransferDepo.Rate.ToString(CultureInfo.InvariantCulture) + ", Cap = " + (targetTransferDepo.Cap ? "1" : "0") +
                                    $"where Id = '{targetTransferDepo.ID}'", connection).ExecuteNonQuery();
                                depositsTable.Dispose();
                                adapter.SelectCommand = new SqlCommand() { CommandText = "select * from [Deposits]", Connection = connection };
                                depositsTable = new DataTable("Deposits");
                                adapter.Fill(depositsTable);
                                DataSource = DataView = depositsTable.DefaultView;
                            }
                            catch
                            {
                                throw;
                            }
                    }
                    string comment = $"Со счета {depo} переведено {TransferAmount} на счет {targetTransferDepo}";
                    MainViewModel.Log(comment);
                    MessageBox.Show(comment);
                }
                TransferDialog dialog = new TransferDialog { DataContext = this };
                RaisePropertyChanged(nameof(Deposits));
                if ((bool)dialog.ShowDialog() && targetTransferDepo != null)
                    DoTransfer();
            }
            if (depo == null || depo.ClientID == default)
                return;
            // Блокируем появление в списке депозитов, на которые могут быть переведены средства, депозит, с которого средства списываются.
            targetTransferListEnabled = true;
            TryToTransfer();
            targetTransferListEnabled = false;
            // Снимаем блокировку со списка доступных депозитов.
            RaisePropertyChanged(nameof(Deposits));
        }
        private void SelectTargetTransferDepo(object e)
        {
            if ((TargetTransferDepo = (e as ListBox).SelectedItem is Account account ? account : null) == null)
                return;
            //if (!(TransferEnabled = targetTransferDepo.ID != Depo.ID))
            //{
            //    MessageBox.Show("Нельзя делать перевод внутри одного и того же счета!!");
            //    return;
            //}
            // Инициализируем флаг подтверждения перевода запрошенной суммы.
            TransferSumOKEnabled = false;
        }
        private void TransfAmountChanged(object e)
        {
            if (!decimal.TryParse((e as TextBox).Text, out decimal amount) || amount <= 0)
                return;
            TransferAmount = amount;
            TransferSumOKEnabled = depo.Size - amount >= Account.minSize;
            if (!TransferSumOKEnabled)
            {
                MessageBox.Show(
                    $"Количество средств {depo.Size} на счету {depo.Number} не допускает сумму {transferAmount} списания.\n" +
                    $"Максимальная сумма списания {depo.Size - Account.minSize}");
                return;
            }
        }
        #endregion
        #endregion
    }
}