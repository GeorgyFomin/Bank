using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfBank.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Устанавливает и возвращает ссылку на соединение с БД.
        /// </summary>
        public SqlConnection SqlConnection { get; set; }
        /// <summary>
        /// Хранит ссылку на обработчик события изменения свойства.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
