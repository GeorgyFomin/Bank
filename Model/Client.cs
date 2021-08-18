using System;
using System.Collections.ObjectModel;
using System.IO;

namespace ClassLibrary
{
    public class Client : Named
    {
        /// <summary>
        /// Хранит список вкладов.
        /// </summary>
        private ObservableCollection<Account> accounts = new ObservableCollection<Account>();
        #region Properties
        /// <summary>
        /// Устанавливает и возвращает ссылки на счета клиента.
        /// </summary>
        public ObservableCollection<Account> Accounts
        {
            get => accounts;
            set
            {
                accounts = value ?? new ObservableCollection<Account>();
                foreach (Account account in accounts)
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
        public Client() : base() { }
        public Client(string name = null, ObservableCollection<Account> accounts = null) : base(name) => Accounts = accounts;
        /// <summary>
        /// Печатает сведения о клиенте.
        /// </summary>
        /// <param name="tw">Райтер.</param>
        public void Print(TextWriter tw)
        {
            // Печатаем информацию о клиенте.
            tw.WriteLine("Клиент\n" + Info() + "\nСчета");
            tw.WriteLine(Account.header);
            // Печатаем сведения о счетах.
            foreach (Account account in Accounts)
            {
                account.PrintFields(tw);
            }
        }
    }
}
