using ClassLibrary;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public ObservableCollection<Client> Clients { get => bank.Clients; }
        public ObservableCollection<Dep> Deps { get => bank.Deps; }
        public Client Client { get => client; set { client = value; RaisePropertyChanged(nameof(Client)); } }
        public Dep Dep { get => dep; set { dep = value; RaisePropertyChanged(nameof(Dep)); } }
        public ICommand SelCommand => selCommand ?? (selCommand =
            new RelayCommand((e) => Client = (e as DataGrid).SelectedItem is Client client ? client : null));
        public ICommand RemoveClientCommand => removeClientCommand ?? (removeClientCommand = new RelayCommand(RemoveClient));
        public ICommand AddClientCommand => addClientCommand ?? (addClientCommand = new RelayCommand(AddClient));
        public ICommand DepSelectedCommand => depSelectedCommand ?? (depSelectedCommand = new RelayCommand((e) => DepDoSelected = true));
        public bool DepDoSelected { get => depDoSelected; set { depDoSelected = value; RaisePropertyChanged(nameof(DepDoSelected)); } }
        public ICommand OKCommand => oKCommand ?? (oKCommand = new RelayCommand((e) =>
        {
            DepsDialog window = e as DepsDialog;
            Dep = window.depListBox.SelectedItem is Dep dep ? dep : null;
            window.DialogResult = true;
        }));
        public ICommand EndClientEditCommand => endClientEditCommand ?? (endClientEditCommand = new RelayCommand(
            (e) => MainViewModel.Log($"Имя клиента {Client} отредактировано.")));
        #endregion
        public ClientViewModel(Bank bank) : this() => this.bank = bank;
        public ClientViewModel() { depDoSelected = false; }
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
        private void AddClientTo(Dep dep)
        {
            client.DepID = dep.ID;
            dep.Clients.Add(client);
            MainViewModel.Log($"В отдел {dep} добавлен клиент {client}.");
        }
        private void AddClient(object commandParameter)
        {
            if (client == null || client.DepID != default)
                return;
            DepsDialog dialog = new DepsDialog();
            dialog.DataContext = this;
            if ((bool)dialog.ShowDialog() && Dep != null)
                AddClientTo(Dep);
            RaisePropertyChanged(nameof(Clients));
        }
    }
}