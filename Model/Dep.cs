using System.Collections.Generic;
using System.IO;

namespace ClassLibrary
{
    public class Dep : Named
    {
        /// <summary>
        /// Хранит список клиентов.
        /// </summary>
        private List<Client> clients;
        #region Properties
        /// <summary>
        /// Устанавливает и возвращает ссылки на клиентов.
        /// </summary>
        public List<Client> Clients
        {
            get => clients; set
            {
                clients = value ?? new List<Client>();
                foreach (Client client in clients)
                {
                    client.DepID = ID;
                }
            }
        }
        public Dep() : base() { }
        #endregion
        public Dep(string name = null, List<Client> clients = null) : base(name) => Clients = clients;
        /// <summary>
        /// Печатает сведения об отделе.
        /// </summary>
        /// <param name="tw">Райтер.</param>
        public void Print(TextWriter tw)
        {
            // Печатаем информацию об отделе.
            tw.WriteLine("Отдел " + this);
            // Печатаем сведения о клиентах.
            foreach (Client client in Clients)
            {
                client.Print(tw);
            }
        }

    }
}
