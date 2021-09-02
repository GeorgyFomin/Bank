using ClassLibrary;
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
        private Dep dep;
        private RelayCommand selCommand;
        private RelayCommand removeClientCommand;
        private RelayCommand addClientCommand;
        private RelayCommand depSelectedCommand;
        private RelayCommand oKCommand;
        private RelayCommand endClientEditCommand;
        private bool depDoSelected;
        #endregion
        #region Properties
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
            new RelayCommand((e) => Client = (e as DataGrid).SelectedItem is Client client ? client : null));
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
        public DataView DataView { get; set; }
        #endregion
        public ClientViewModel(Bank bank, SqlConnection connection) : this()
        {
            DataTable dt = new DataTable("Clients");
            new SqlDataAdapter(new SqlCommand() { CommandText = "select * from [Clients]", Connection = connection }).Fill(dt);
            DataView = dt.DefaultView;
            this.bank = bank;
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
                _ = bank.Deps.First((g) => client.DepID == g.ID).Clients.Remove(client);
                RaisePropertyChanged(nameof(Clients));
                MainViewModel.Log($"Удален клиент {client}");
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