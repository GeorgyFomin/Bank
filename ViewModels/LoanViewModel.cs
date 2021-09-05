using ClassLibrary;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private bool clientDoSelected;
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
            new RelayCommand((e) => Loan = (e as DataGrid).SelectedItem is Account loan ? loan : null));
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
        public DataView DataView { get; set; }
        #endregion
        public LoanViewModel(Bank bank) : this()
        {
            DataTable dt = new DataTable("Loans");
            new SqlDataAdapter(new SqlCommand() { CommandText = "select * from [Loans]", Connection = App.SqlConnection }).Fill(dt);
            DataView = dt.DefaultView;
            this.bank = bank;
        }

        public LoanViewModel() { }
        private void RemoveLoan(object obj)
        {
            if (loan != null &&
                MessageBox.Show($"Удалить кредит №{loan.Number}?", "Удаление кредита " + loan.Number, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _ = Clients.First((g) => loan.ClientID == g.ID).Accounts.Remove(loan);
                RaisePropertyChanged(nameof(Loans));
                MainViewModel.Log($"Удален кредит {loan}");
            }
        }
        private void EditLoan(object e)
        {
            if (loan.ClientID != default)
                MainViewModel.Log($"Поля кредита {loan} отредактированы.");
            else
                if ((bool)new ClientsDialog { DataContext = this }.ShowDialog() && Client != null)
                AddLoanToClient();
        }
        private void AddLoanToClient()
        {
            loan.ClientID = client.ID;
            client.Accounts.Add(loan);
            RaisePropertyChanged(nameof(Loan));
            MainViewModel.Log($"Клиенту {client} открыт кредит {loan}.");
        }
    }
}