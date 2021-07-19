using ClassLibrary;
using FontAwesome.Sharp;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WpfBank.Commands;

namespace WpfBank.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public static void Log(string report)
        {
            using (TextWriter tw = File.AppendText("log.txt"))
                tw.WriteLine(DateTime.Now.ToString() + ":" + report);
        }
        #region Fields
        private RelayCommand dragCommand;
        private RelayCommand minimizeCommand;
        private RelayCommand maximizeCommand;
        private RelayCommand closeCommand;
        private RelayCommand clientsCommand;
        private RelayCommand depositsCommand;
        private RelayCommand loansCommand;
        private RelayCommand resetBankCommand;
        private Bank bank;
        private ViewModelBase viewModel;
        #endregion
        #region Properties
        public ViewModelBase ViewModel { get => viewModel; set { viewModel = value; RaisePropertyChanged(nameof(ViewModel)); } }
        public Bank Bank { get => bank; set { bank = value; RaisePropertyChanged(nameof(Bank)); } }
        public ICommand DragCommand => dragCommand ?? (dragCommand = new RelayCommand((e) => (e as MWindow).DragMove()));
        public ICommand MinimizeCommand => minimizeCommand ?? (minimizeCommand =
            new RelayCommand((e) => (e as MWindow).WindowState = WindowState.Minimized));
        public ICommand MaximizeCommand => maximizeCommand ?? (maximizeCommand = new RelayCommand((e) =>
        {
            MWindow window = e as MWindow;
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            window.MaxIconBlock.Icon = window.WindowState == WindowState.Maximized ? IconChar.WindowRestore : IconChar.WindowMaximize;
        }));
        public ICommand CloseCommand => closeCommand ?? (closeCommand = new RelayCommand((e) => (e as MWindow).Close()));
        public ICommand ClientsCommand => clientsCommand ?? (clientsCommand = new RelayCommand((e) => ViewModel = new ClientViewModel(Bank)));
        public ICommand DepositsCommand => depositsCommand ?? (depositsCommand = new RelayCommand((e) => ViewModel = new DepositViewModel(Bank)));
        public ICommand LoansCommand => loansCommand ?? (loansCommand = new RelayCommand((e) => ViewModel = new LoanViewModel(Bank)));
        public ICommand ResetBankCommand => resetBankCommand ?? (resetBankCommand = new RelayCommand((e) => ResetBank()));
        #endregion
        public MainViewModel() => ResetBank();
        private void ResetBank()
        {
            Bank = RandomBank.GetBank();
            Log($"Создан банк {bank.Name}.");
            ViewModel = new BankNameViewModel(Bank);
        }
    }
}
