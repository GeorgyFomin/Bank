using ClassLibrary;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WpfBank.Commands;
using WpfBank.Dialogs;

namespace WpfBank.ViewModels
{
    public class LoanViewModel : ViewModelBase
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
        /// Хранит ссылку на таблицу с кредитами.
        /// </summary>
        private readonly DataTable loansTable;
        /// <summary>
        /// Хранит ссылку на текущий кредит.
        /// </summary>
        private Account loan;
        /// <summary>
        /// Хранит ссылку на клиента, которому открыт новый кредит.
        /// </summary>
        private Client client;
        /// <summary>
        /// Хранит ссылку на текущее содержимое таблицы кредитов.
        /// </summary>
        private DataView dataView;
        /// <summary>
        /// Хранит ссылку на содержимое текущей строки таблицы кредитов.
        /// </summary>
        private DataRowView dataRowView;
        /// <summary>
        /// Хранит ссылку на информацию об отредактированной ячейки таблицы кредитов.
        /// </summary>
        private DataGridCellInfo editedCell;
        /// <summary>
        /// Хранит ссылку на отредактированную колонку таблицы кредитов.
        /// </summary>
        private DataGridColumn editedColumn;
        /// <summary>
        /// Хранит ссылку на текущую колонку таблицы кредитов.
        /// </summary>
        private DataGridColumn curColumn;
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
        #endregion
        #region Команды управления событиями
        private RelayCommand selectionChangedCommand;
        private RelayCommand removeLoanCommand;
        private RelayCommand loanEditEndingCommand;
        private RelayCommand cellChangedCommand;
        private RelayCommand clientSelectedCommand;
        private RelayCommand oKClientSelectionCommand;
        #endregion
        #endregion
        #region Properties
        #region Свойства общего назначения
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
        /// Устанавливает и возвращает ссылку на содержимое таблицы.
        /// </summary>
        public DataView DataView { get => dataView; set { dataView = value; RaisePropertyChanged(nameof(DataView)); } }
        /// <summary>
        /// Устанавливает и возвращает ссылку на текущий источник данных в таблице. Это либо DataView в режиме DBMode, либо Loans. 
        /// </summary>
        public object DataSource { get; set; }
        /// <summary>
        /// Устанавливает и возвращает ссылку на выбранную строку в содержимом таблицы DataView.
        /// </summary>
        public DataRowView DataRowView { get => dataRowView; set { dataRowView = value; RaisePropertyChanged(nameof(DataRowView)); } }
        /// <summary>
        /// Устанавливает и возвращает ссылку на текущий кредит.
        /// </summary>
        public Account Loan { get => loan; set { loan = value; RaisePropertyChanged(nameof(Loan)); } }
        /// <summary>
        /// Возвращает и устанавливает клиента, которому приписываетя вновь открываемый кредит.
        /// </summary>
        public Client Client { get => client; set { client = value; RaisePropertyChanged(nameof(Client)); } }
        /// <summary>
        /// Хранит и возвращает флаг выбора клиента, которому приписываетя вновь открываемый депозит.
        /// </summary>
        public bool ClientDoSelected { get => clientDoSelected; set { clientDoSelected = value; RaisePropertyChanged(nameof(ClientDoSelected)); } }
        #endregion
        #region Команды - обработчики событий.
        public ICommand SelectionChangedCommand => selectionChangedCommand ?? (selectionChangedCommand = new RelayCommand(SelectLoan));
        public ICommand RemoveLoanCommand => removeLoanCommand ?? (removeLoanCommand = new RelayCommand(RemoveLoan));
        public ICommand LoanEditEndingCommand => loanEditEndingCommand ?? (loanEditEndingCommand = new RelayCommand(EditLoan));
        public ICommand CellChangedCommand => cellChangedCommand ?? (cellChangedCommand = new RelayCommand(CellChanged));
        #region Команды выбора клиента вновь созданного кредита
        public ICommand ClientSelectedCommand => clientSelectedCommand ?? (clientSelectedCommand = new RelayCommand((e) => ClientDoSelected = true));
        public ICommand OKClientSelectionCommand => oKClientSelectionCommand ?? (oKClientSelectionCommand = new RelayCommand((e) =>
        {
            ClientsDialog dialog = e as ClientsDialog;
            Client = dialog.clientListBox.SelectedItem is Client client ? client : null;
            dialog.DialogResult = true;
        }));
        #endregion
        #endregion
        #endregion
        #region Methods
        #region Constructors
        public LoanViewModel(Bank bank) : this()
        {
            using (SqlConnection connection = new SqlConnection() { ConnectionString = App.ConnectionString })
                try
                {
                    connection.Open();
                    adapter = new SqlDataAdapter(new SqlCommand() { CommandText = "select * from [Loans]", Connection = connection });
                    loansTable = new DataTable("Loans");
                    adapter.Fill(loansTable);
                }
                catch
                {
                    throw;
                }
            DataView = loansTable.DefaultView;
            this.bank = bank;
            if (MainViewModel.DBMode)
                DataSource = DataView;
            else
                DataSource = Loans;
        }
        public LoanViewModel() { }
        #endregion
        #region Handlers
        private void SelectLoan(object e)
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
            Loan = MainViewModel.DBMode ?
            (DataRowView = (DataRowView)selItem).IsNew ? new Account() : Loans.First((g) => DataRowView.Row.Field<Guid>("Id") == g.ID) :
            (Account)selItem;
        }
        private void RemoveLoan(object obj)
        {
            if (loan == null || MessageBox.Show($"Удалить кредит №{loan.Number}?", "Удаление кредита " + loan.Number, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }
            _ = Clients.First((g) => loan.ClientID == g.ID).Loans.Remove(loan);
            RaisePropertyChanged(nameof(Loans));
            MainViewModel.Log($"Удален кредит {loan}");
            if (MainViewModel.DBMode)
                using (SqlConnection connection = new SqlConnection() { ConnectionString = App.ConnectionString })
                    try
                    {
                        connection.Open();
                        adapter.DeleteCommand = new SqlCommand($"delete from {loansTable} where '{dataRowView.Row.Field<int>("Number")}' = Number", connection);
                        dataRowView.Delete();
                        adapter.Update(loansTable);
                    }
                    catch
                    {
                        throw;
                    }
            else
                (obj as DataGrid).ItemsSource = Loans;
            Loan = null;
        }
        private void EditLoan(object e)
        {
            //MessageBox.Show("CellEditEnding\n" +
            //    "Edited Column: " + (editedColumn != null ? (editedColumn.ClipboardContentBinding as Binding).Path.Path : "null") +
            //    "\nCur Column: " + (curColumn != null ? (curColumn.ClipboardContentBinding as Binding).Path.Path : "null") +
            //    "\nadded = " + (added == null ? "null" : $"{added}"));
            void InsertLoanIntoClient()
            {
                loan.ClientID = client.ID;
                client.Loans.Add(loan);
                added = true;
                RaisePropertyChanged(nameof(Loan));
                MainViewModel.Log($"Клиенту {client} будет открыт кредит {loan}.");
            }
            if (loan.ClientID != default)
            {
                added = false;
                MainViewModel.Log($"Поля кредита {loan} будут отредактированы.");
            }
            else
            {
                bool flag;
                do
                {
                    if (flag = (bool)new ClientsDialog { DataContext = this }.ShowDialog() && client != null)
                        InsertLoanIntoClient();
                } while (!flag);
            }
            endEdited = true;
        }
        private void CellChanged(object e)
        {
            string DBChanged()
            {
                void SetNewRowView(Account loan)
                {
                    int lastRowIndex = loansTable.Rows.Count - 1;
                    loansTable.Rows[lastRowIndex][0] = loan.ID;
                    loansTable.Rows[lastRowIndex][1] = loan.ClientID;
                    loansTable.Rows[lastRowIndex][2] = loan.Number;
                    loansTable.Rows[lastRowIndex][3] = loan.Size;
                    loansTable.Rows[lastRowIndex][4] = loan.Rate;
                    loansTable.Rows[lastRowIndex][5] = loan.Cap;
                    loansTable.AcceptChanges();
                    DataSource = DataView = loansTable.DefaultView;
                }
                // Определяем имя поля, с которым связан столбец с отредактированной ячейкой.
                string editedColumnName = added.Value ? (curColumn.ClipboardContentBinding as Binding).Path.Path : (editedColumn.ClipboardContentBinding as Binding).Path.Path;
                // Свойству columnName объекта класса Loan присваиваем значение, кторое возвращает
                // расширяющий обобщенный метод dataRowView.Row.Field<T>(editedColumnName).
                // Тип T является типом поля столбца columnName, т.е. dataRowView.Row.Table.Columns[editedColumnName].DataType.
                typeof(Account).GetProperty(editedColumnName).SetValue(Loan,
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
                            new SqlCommand($"update {loansTable} set Size = " + loan.Size.ToString(CultureInfo.InvariantCulture) +
                                ", Rate = " + loan.Rate.ToString(CultureInfo.InvariantCulture) + ", Cap = " + (loan.Cap ? "1" : "0") +
                                $"where Id = '{loan.ID}'", connection).ExecuteNonQuery();
                        }
                        else
                        {
                            SetNewRowView(loan);
                            new SqlCommand($"insert into {loansTable} values ('{loan.ID}', '{loan.ClientID}', {loan.Number}, " +
                            loan.Size.ToString(CultureInfo.InvariantCulture) + ", " + loan.Rate.ToString(CultureInfo.InvariantCulture) + (loan.Cap ? ", 1" : ", 0") + ")",
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
                if (endEdited && cell != editedCell && cell.Item.ToString() != "{NewItemPlaceholder}" && (DataRowView)cell.Item == DataRowView)
                {
                    editedCell = cell;
                    endEdited = false;
                    if (added == null) return;
                    dataGrid.CommitEdit();
                    //MessageBox.Show("Для завершения редактирования ячейки надо нажать Enter или перейти на следующую строку.");
                    //loansTable.AcceptChanges();
                    //RaisePropertyChanged(nameof(DataSource));
                    //added = null;
                    //return;
                }
                editedCell = cell;
                if (added == null) return;
                comment = DBChanged();
                comment = added.Value ? $"Клиенту {client} открыт кредит {loan}" : "Поле " + comment + $" кредита №{loan.Number} отредактировано.";
            }
            else
            {
                if (added == null)
                    return;
                comment = added.Value ? $"Клиенту {client} открыт кредит {loan}" : $"Поля кредита №{loan.Number} отредактированы.";
            }
            MainViewModel.Log(comment);
            MessageBox.Show(comment);
            added = null;
        }
        #endregion
        #endregion
    }
}