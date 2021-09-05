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
        private bool clientDoSelected;
        private bool transferEnabled;
        private Account depoTo;
        private decimal transferAmount;
        private bool depoFromSelected;
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
        public Account Depo { get => depo; set { depo = value; RaisePropertyChanged(nameof(Depo)); } }
        public Client Client { get => client; set { client = value; RaisePropertyChanged(nameof(Client)); } }
        public ICommand SelCommand => selCommand ?? (selCommand =
            new RelayCommand((e) => DepoFromSelected = (Depo = (e as DataGrid).SelectedItem is Account depo ? depo : null) != null));
        public ICommand RemoveDepoCommand => removeDepoCommand ?? (removeDepoCommand = new RelayCommand(RemoveDepo));
        public ICommand TransferCommand => transferCommand ?? (transferCommand = new RelayCommand(Transfer));
        public ICommand ClientSelectedCommand => clientSelectedCommand ?? (clientSelectedCommand = new RelayCommand((e) => ClientDoSelected = true));
        public bool ClientDoSelected { get => clientDoSelected; set { clientDoSelected = value; RaisePropertyChanged(nameof(ClientDoSelected)); } }
        public bool TransferEnabled { get => transferEnabled; set { transferEnabled = value; RaisePropertyChanged(nameof(TransferEnabled)); } }
        public ICommand OKCommand => oKCommand ?? (oKCommand = new RelayCommand(OkClientOfAddedDeposit));
        public Account DepoTo { get => depoTo; set { depoTo = value; RaisePropertyChanged(nameof(DepoTo)); } }
        public decimal TransferAmount { get => transferAmount; set { transferAmount = value; RaisePropertyChanged(nameof(TransferAmount)); } }
        public ICommand OKDepoToCommand => oKDepoToCommand ?? (oKDepoToCommand = new RelayCommand((e) => (e as TransferDialog).DialogResult = true));
        public ICommand DepoToSelectedCommand => depoToSelectedCommand ?? (depoToSelectedCommand = new RelayCommand(DepoToTransfer));
        public ICommand EndDepoEditCommand => endDepoEditCommand ?? (endDepoEditCommand = new RelayCommand(EditDeposit));
        public bool DepoFromSelected { get => depoFromSelected; set { depoFromSelected = value; RaisePropertyChanged(nameof(DepoFromSelected)); } }
        public DataView DataView { get; set; }
        #endregion
        public DepositViewModel(Bank bank) : this()
        {
            DataTable dt = new DataTable("Deposits");
            new SqlDataAdapter(new SqlCommand() { CommandText = "select * from [Deposits]", Connection = App.SqlConnection }).Fill(dt);
            DataView = dt.DefaultView;
            this.bank = bank;
        }
        public DepositViewModel() { depoFromSelected = transferEnabled = clientDoSelected = false; }
        private void RemoveDepo(object obj)
        {
            if (depo != null &&
                MessageBox.Show($"Удалить депозит №{depo.Number}?", $"Удаление депозита №{depo.Number}", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _ = Clients.First((g) => depo.ClientID == g.ID).Accounts.Remove(depo);
                RaisePropertyChanged(nameof(Deposits));
                MainViewModel.Log($"Удален депозит №{depo.Number}");
            }
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
            MainViewModel.Log($"Со счета {depo} переведено {TransferAmount} на счет {depoTo}");
        }
        private void DepoToTransfer(object e)
        {
            if ((DepoTo = (e as ListBox).SelectedItem is Account depo ? depo : null) == null)
                return;
            if (depoTo.ID == Depo.ID)
            {
                MessageBox.Show("Нельзя делать перевод внутри одного и того же счета!!");
                return;
            }
            TransferEnabled = true;
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
                MainViewModel.Log($"Поля депозита №{depo.Number} отредактированы.");
            else
                if ((bool)new ClientsDialog { DataContext = this }.ShowDialog() && client != null)
                AddDepoTo();
        }
        private void AddDepoTo()
        {
            depo.ClientID = client.ID;
            client.Accounts.Add(depo);
            RaisePropertyChanged(nameof(Depo));
            MainViewModel.Log($"Клиенту {client} открыт депозит {depo}.");
        }
    }
}