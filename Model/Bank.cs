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
        public override string ToString() => "Bank " + base.ToString();
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
