using ClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfBank.ViewModels
{
    public class BankNameViewModel : ViewModelBase
    {
        private readonly Bank bank;
        public string BankName
        {
            get => bank.Name;
            set
            {
                if (string.IsNullOrEmpty(value))
                    return;
                bank.Name = value;
                RaisePropertyChanged(nameof(BankName));
            }
        }
        public BankNameViewModel() { }
        public BankNameViewModel(Bank bank) : this() => this.bank = bank;
    }
}
