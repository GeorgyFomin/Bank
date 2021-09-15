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
        private readonly Bank bank;
        private Account loan;
        private Client client;
        private RelayCommand selCommand;
        private RelayCommand removeLoanCommand;
        private RelayCommand clientSelectedCommand;
        private RelayCommand oKCommand;
        private RelayCommand endLoanEditCommand;
        private RelayCommand cellChangedCommand;
        private bool clientDoSelected;
        private DataTable loansTable;
        private SqlDataAdapter adapter;
        private DataView dataView;
        private DataRowView dataRowView;
        private bool? added = null;
        private bool endEdited;
        private DataGridCellInfo cellView;
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
        public Account Loan { get => loan; set { loan = value; RaisePropertyChanged(nameof(Loan)); } }
        public Client Client { get => client; set { client = value; RaisePropertyChanged(nameof(Client)); } }
        public ICommand SelCommand => selCommand ?? (selCommand =
            new RelayCommand((e) =>
            {
                // Получаем ссылку строку, выбранную в DataGrid.
                object selItem = (e as DataGrid).SelectedItem;
                // Фильтруем ссылку.
                if (selItem == null || selItem.ToString() == "{NewItemPlaceholder}")
                    return;
                // Определяем текущую строку DataRowView в режиме DBMode и текущий клиента Deposit в любом режиме.
                Loan = MainViewModel.DBMode ?
                (DataRowView = (DataRowView)selItem).IsNew ? new Account() : Loans.First((g) => DataRowView.Row.Field<Guid>("Id") == g.ID) :
                (Account)selItem;
            }));
        public ICommand RemoveLoanCommand => removeLoanCommand ?? (removeLoanCommand = new RelayCommand(RemoveLoan));
        public ICommand ClientSelectedCommand => clientSelectedCommand ?? (clientSelectedCommand = new RelayCommand((e) => ClientDoSelected = true));
        public bool ClientDoSelected { get => clientDoSelected; set { clientDoSelected = value; RaisePropertyChanged(nameof(ClientDoSelected)); } }
        public ICommand OKCommand => oKCommand ?? (oKCommand = new RelayCommand((e) =>
        {
            ClientsDialog dialog = e as ClientsDialog;
            Client = dialog.clientListBox.SelectedItem is Client client ? client : null;
            dialog.DialogResult = true;
        }));
        public ICommand EndLoanEditCommand => endLoanEditCommand ?? (endLoanEditCommand = new RelayCommand(EditLoan));
        public ICommand CellChangedCommand => cellChangedCommand ?? (cellChangedCommand = new RelayCommand(CellChanged));
        public DataView DataView { get => dataView; set { dataView = value; RaisePropertyChanged(nameof(DataView)); } }
        public object DataSource { get; set; }
        public DataRowView DataRowView { get => dataRowView; set { dataRowView = value; RaisePropertyChanged(nameof(DataRowView)); } }
        #endregion
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
                        AddLoanToClient();
                } while (!flag);
            }
            endEdited = true;
        }
        private void AddLoanToClient()
        {
            loan.ClientID = client.ID;
            client.Loans.Add(loan);
            added = true;
            RaisePropertyChanged(nameof(Loan));
            MainViewModel.Log($"Клиенту {client} будет открыт кредит {loan}.");
        }
        private void DBChanged(object e)
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
            DataGridColumn column = (e as DataGrid).CurrentColumn;
            // Определяем имя поля, с которым связан текущий столбец, ячейка которого изменена.
            string columnName = (column.ClipboardContentBinding as Binding).Path.Path;
            // Свойству columnName объекта класса Client присваиваем значение, кторое возвращает
            // расширяющий обобщенный метод dataRowView.Row.Field<T>(columnName).
            // Тип T является типом поля столбца columnName, т.е. dataRowView.Row.Table.Columns[columnName].DataType.
            typeof(Account).GetProperty(columnName).SetValue(Loan,
                // Вызываем метод dataRowView.Row.Field<T>(columnName), где тип T есть dataRowView.Row.Table.Columns[columnName].DataType
                // Для этого получаем MethodInfo метода Field,
                typeof(DataRowExtensions).GetMethod("Field", new Type[] { typeof(DataRow), typeof(string) }).
                // создаем статический метод Field<T> класса DataRowExtension, где тип T есть dataRowView.Row.Table.Columns[columnName].DataType,
                // и вызываем его над объектом dataRowView.Row с параметром columnName
                MakeGenericMethod(dataRowView.Row.Table.Columns[columnName].DataType).Invoke(null, new object[] { dataRowView.Row, columnName }));
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
            return;
        }
        private void CellChanged(object e)
        {
            if (MainViewModel.DBMode)
            {
                DataGrid dataGrid = (DataGrid)e;
                if (dataGrid.CurrentColumn == null)
                    return;
                // Отфильтровываем смену ячейки в одной и той же строке.
                DataGridCellInfo cell = dataGrid.CurrentCell;
                if (endEdited && cell != cellView && (DataRowView)cell.Item == DataRowView)
                {
                    int rowIndex = DataView.Table.Rows.IndexOf(DataRowView.Row),
                        columnIndex = cellView.Column.DisplayIndex;
                    loansTable.Rows[rowIndex][columnIndex] = DataRowView[columnIndex];
                    loansTable.AcceptChanges();
                    DataSource = DataView = loansTable.DefaultView;
                    MessageBox.Show("После редактирования ячейки надо нажать Enter или перейти на следующую строку.");
                    endEdited = false;
                    added = null;
                    return;
                }
                cellView = cell;
            }
            if (added == null)
                return;
            string comment;
            if (MainViewModel.DBMode)
                DBChanged(e);
            comment = added.Value ? $"Клиенту {client} открыт кредит {loan}" : $"Поля кредита №{loan.Number} отредактированы.";
            MainViewModel.Log(comment);
            MessageBox.Show(comment);
            added = null;
        }
    }
}