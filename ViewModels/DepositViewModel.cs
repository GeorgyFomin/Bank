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
        private readonly Bank bank;
        private Account depo;
        private Client client;
        private RelayCommand selCommand;
        private RelayCommand removeDepoCommand;
        private RelayCommand transferCommand;
        private RelayCommand clientSelectedCommand;
        private RelayCommand oKCommand;
        private RelayCommand depoToSelectedCommand;
        private RelayCommand oKDepoToCommand;
        private RelayCommand endDepoEditCommand;
        private RelayCommand cellChangedCommand;
        private bool clientDoSelected;
        private bool transferEnabled;
        private bool transferOKEnabled;
        private Account depoTo;
        private decimal transferAmount;
        private bool depoFromSelected;
        private DataTable depositsTable;
        private SqlDataAdapter adapter;
        private DataView dataView;
        private object dataSource;
        private DataRowView dataRowView;
        private bool? added = null;
        #endregion
        #region Properties
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
        public Account Depo { get => depo; set { depo = value; RaisePropertyChanged(nameof(Depo)); } }
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
                Depo = MainViewModel.DBMode ?
                (DataRowView = (DataRowView)selItem).IsNew ? new Account() : Deposits.First((g) => DataRowView.Row.Field<Guid>("Id") == g.ID) :
                (Account)selItem;
                DepoFromSelected = true;
            }));
        public ICommand RemoveDepoCommand => removeDepoCommand ?? (removeDepoCommand = new RelayCommand(RemoveDepo));
        public ICommand TransferCommand => transferCommand ?? (transferCommand = new RelayCommand(Transfer));
        public ICommand ClientSelectedCommand => clientSelectedCommand ?? (clientSelectedCommand = new RelayCommand((e) => ClientDoSelected = true));
        public bool ClientDoSelected { get => clientDoSelected; set { clientDoSelected = value; RaisePropertyChanged(nameof(ClientDoSelected)); } }
        public bool TransferEnabled { get => transferEnabled; set { transferEnabled = value; RaisePropertyChanged(nameof(TransferEnabled)); } }
        public bool TransferOKEnabled { get => transferOKEnabled; set { transferOKEnabled = value; RaisePropertyChanged(nameof(TransferOKEnabled)); } }
        public ICommand OKCommand => oKCommand ?? (oKCommand = new RelayCommand(OkClientOfAddedDeposit));
        public Account DepoTo { get => depoTo; set { depoTo = value; RaisePropertyChanged(nameof(DepoTo)); } }
        public decimal TransferAmount { get => transferAmount; set { transferAmount = value; RaisePropertyChanged(nameof(TransferAmount)); } }
        public ICommand OKDepoToCommand => oKDepoToCommand ?? (oKDepoToCommand = new RelayCommand((e) => (e as TransferDialog).DialogResult = true));
        public ICommand DepoToSelectedCommand => depoToSelectedCommand ?? (depoToSelectedCommand = new RelayCommand(DepoToTransfer));
        public ICommand EndDepoEditCommand => endDepoEditCommand ?? (endDepoEditCommand = new RelayCommand(EditDeposit));
        public ICommand CellChangedCommand => cellChangedCommand ?? (cellChangedCommand = new RelayCommand(CellChanged));
        public bool DepoFromSelected { get => depoFromSelected; set { depoFromSelected = value; RaisePropertyChanged(nameof(DepoFromSelected)); } }
        public DataView DataView { get => dataView; set { dataView = value; RaisePropertyChanged(nameof(DataView)); } }
        public object DataSource { get => dataSource; set { dataSource = value; RaisePropertyChanged(nameof(DataSource)); } }
        public DataRowView DataRowView { get => dataRowView; set { dataRowView = value; RaisePropertyChanged(nameof(DataRowView)); } }
        #endregion
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
        public DepositViewModel() { depoFromSelected = transferEnabled = clientDoSelected = false; }
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
        private void Transfer(object obj)
        {
            if (depo == null || depo.ClientID == default)
                return;
            TryToTransfer();
            RaisePropertyChanged(nameof(Deposits));
        }
        private void TryToTransfer()
        {
            TransferDialog dialog = new TransferDialog { DataContext = this };
            if ((bool)dialog.ShowDialog() && depoTo != null)
                DoTransfer();
        }
        private void DoTransfer()
        {
            if (MessageBox.Show($"Вы действительно хотите перевести со счета №{depo.Number} на счет №{depoTo.Number} сумму {TransferAmount}?",
                "Перевод между счетами", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;
            depo.Size -= TransferAmount;
            DepoTo.Size += TransferAmount;
            MainViewModel.Log($"Со счета {depo} будет переведено {TransferAmount} на счет {depoTo}");
            if (MainViewModel.DBMode)
            {
                using (SqlConnection connection = new SqlConnection() { ConnectionString = App.ConnectionString })
                    try
                    {
                        connection.Open();
                        new SqlCommand($"update {depositsTable} set Size = " + depo.Size.ToString(CultureInfo.InvariantCulture) +
                            ", Rate = " + depo.Rate.ToString(CultureInfo.InvariantCulture) + ", Cap = " + (depo.Cap ? "1" : "0") +
                            $"where Id = '{depo.ID}'", connection).ExecuteNonQuery();
                        new SqlCommand($"update {depositsTable} set Size = " + depoTo.Size.ToString(CultureInfo.InvariantCulture) +
                            ", Rate = " + depoTo.Rate.ToString(CultureInfo.InvariantCulture) + ", Cap = " + (depoTo.Cap ? "1" : "0") +
                            $"where Id = '{depoTo.ID}'", connection).ExecuteNonQuery();
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

            string comment = $"Со счета {depo} переведено {TransferAmount} на счет {depoTo}";
            MainViewModel.Log(comment);
            MessageBox.Show(comment);
        }
        private void DepoToTransfer(object e)
        {
            if ((DepoTo = (e as ListBox).SelectedItem is Account account ? account : null) == null)
                return;
            if (depoTo.ID == Depo.ID)
            {
                MessageBox.Show("Нельзя делать перевод внутри одного и того же счета!!");
                return;
            }
            TransferEnabled = TransferOKEnabled = true;
        }
        private void OkClientOfAddedDeposit(object e)
        {
            ClientsDialog dialog = e as ClientsDialog;
            Client = dialog.clientListBox.SelectedItem is Client client ? client : null;
            dialog.DialogResult = true;
        }
        private void EditDeposit(object e)
        {
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
                        AddDepoTo();
                } while (!flag);
            }
        }
        private void AddDepoTo()
        {
            depo.ClientID = client.ID;
            client.Deposits.Add(depo);
            RaisePropertyChanged(nameof(Depo));
            MainViewModel.Log($"Клиенту {client} будет открыт депозит {depo}.");
            added = true;
        }
        private void DBChanged(object e)
        {
            void SetNewRowView(Account depo)
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
            // Определяем имя поля, с которым связан текущий столбец, ячейка которого изменена.
            string columnName = ((e as DataGrid).CurrentColumn.ClipboardContentBinding as Binding).Path.Path;
            // Свойству columnName объекта класса Client присваиваем значение, кторое возвращает
            // расширяющий обобщенный метод dataRowView.Row.Field<T>(columnName).
            // Тип T является типом поля столбца columnName, т.е. dataRowView.Row.Table.Columns[columnName].DataType.
            typeof(Account).GetProperty(columnName).SetValue(Depo,
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
                        new SqlCommand($"update {depositsTable} set Size = " + depo.Size.ToString(CultureInfo.InvariantCulture) +
                            ", Rate = " + depo.Rate.ToString(CultureInfo.InvariantCulture) + ", Cap = " + (depo.Cap ? "1" : "0") +
                            $"where Id = '{depo.ID}'", connection).ExecuteNonQuery();
                    }
                    else
                    {
                        SetNewRowView(depo);
                        new SqlCommand($"insert into {depositsTable} values ('{depo.ID}', '{depo.ClientID}', {depo.Number}, " +
                        depo.Size.ToString(CultureInfo.InvariantCulture) + ", " + depo.Rate.ToString(CultureInfo.InvariantCulture) + (depo.Cap ? ", 1" : ", 0") + ")",
                        connection).ExecuteNonQuery();
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
            string comment = added.Value ? $"Клиенту {client} открыт депозит {depo}" : $"Поля депозита №{depo.Number} отредактированы.";
            MainViewModel.Log(comment);
            MessageBox.Show(comment);
            added = null;
        }

        private RelayCommand transfAmountChangedCommand;

        public ICommand TransfAmountChangedCommand => transfAmountChangedCommand ?? (transfAmountChangedCommand = new RelayCommand(TransfAmountChanged));
        private void TransfAmountChanged(object e)
        {
            if (!decimal.TryParse((e as TextBox).Text, out decimal amount))
                return;
            TransferAmount = amount;
            TransferOKEnabled = depo.Size - amount >= Account.minSize;
            if (!TransferOKEnabled)
            {
                MessageBox.Show(
                    $"Количество средств {depo.Size} на счету {depo.Number} не допускает сумму {transferAmount} списания.\n" +
                    $"Максимальная сумма списания {depo.Size - Account.minSize}");
                return;
            }
        }
    }
}