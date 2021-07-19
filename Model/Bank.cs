using System.Collections.ObjectModel;
using System.IO;

namespace ClassLibrary
{
    public class Bank : Named
    {
        /// <summary>
        /// Хранит список отделов банка.
        /// </summary>
        private ObservableCollection<Dep> deps = new ObservableCollection<Dep>();
        /// <summary>
        /// Устанавливает и возвращает ссылки на отделы банка.
        /// </summary>
        public ObservableCollection<Dep> Deps { get => deps; set => deps = value ?? new ObservableCollection<Dep>(); }
        public Bank() : base() { }
        public Bank(string name = null, ObservableCollection<Dep> deps = null) : base(name) => Deps = deps;
        public override string ToString()
        {
            return "Bank " + base.ToString();
        }
        /// <summary>
        /// Возвращает список всех депозитов банка.
        /// </summary>
        public ObservableCollection<Account> Deposits
        {
            get
            {
                ObservableCollection<Account> deposits = new ObservableCollection<Account>();
                foreach (Dep dep in deps)
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
        /// Возвращает список всех кредитов банка.
        /// </summary>
        public ObservableCollection<Account> Loans
        {
            get
            {
                ObservableCollection<Account> loans = new ObservableCollection<Account>();
                foreach (Dep dep in deps)
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
                foreach (Dep dep in Deps)
                {
                    foreach (Client client in dep.Clients)
                    {
                        clients.Add(client);
                    }
                }
                return clients;
            }
        }
        /// <summary>
        /// Печатает сведения об отделе.
        /// </summary>
        /// <param name="tw">Райтер.</param>
        public void Print(TextWriter tw)
        {
            // Печатаем информацию о банке.
            tw.WriteLine(this);
            // Печатаем сведения об отделах.
            foreach (Dep dep in Deps)
            {
                dep.Print(tw);
            }
        }

    }
}
