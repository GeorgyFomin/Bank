using System;
using System.Collections.ObjectModel;
using System.IO;

namespace ClassLibrary
{
    public class Client : Named
    {
        /// <summary>
        /// Хранит список депозитов.
        /// </summary>
        private ObservableCollection<Account> deposits = new ObservableCollection<Account>();
        /// <summary>
        /// Хранит список кредитов.
        /// </summary>
        private ObservableCollection<Account> loans = new ObservableCollection<Account>();
        #region Properties
        /// <summary>
        /// Устанавливает и возвращает ссылки на депозиты клиента.
        /// </summary>
        public ObservableCollection<Account> Deposits
        {
            get => deposits;
            set
            {
                deposits = value ?? new ObservableCollection<Account>();
                foreach (Account account in deposits)
                {
                    account.ClientID = ID;
                }
            }
        }
        /// <summary>
        /// Устанавливает и возвращает ссылки на кредиты клиента.
        /// </summary>
        public ObservableCollection<Account> Loans
        {
            get => loans;
            set
            {
                loans = value ?? new ObservableCollection<Account>();
                foreach (Account account in loans)
                {
                    account.ClientID = ID;
                }
            }
        }
        /// <summary>
        /// Устанавливает и возвращает ID отдела банка, к которому прикреплен клиент.
        /// </summary>
        public Guid DepID { get; set; }
        #endregion
        public override string ToString() => "Client " + base.ToString();
        /// <summary>
        /// Печатает сведения о клиенте.
        /// </summary>
        /// <param name="tw">Райтер.</param>
        public void Print(TextWriter tw)
        {
            // Печатаем информацию о клиенте.
            tw.WriteLine("Клиент\n" + Info() + "\nДепозиты");
            tw.WriteLine(Account.header);
            // Печатаем сведения о депозитах.
            foreach (Account deposit in Deposits)
            {
                deposit.PrintFields(tw);
            }
            tw.WriteLine("\nКредиты");
            tw.WriteLine(Account.header);
            // Печатаем сведения о кредитах.
            foreach (Account loan in Loans)
            {
                loan.PrintFields(tw);
            }
        }
    }
}
