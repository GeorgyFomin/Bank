using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfBank
{
    /// <summary>
    /// Interaction logic for PopUpControl.xaml
    /// </summary>
    public partial class BankNameControl : ContentControl
    {
        //public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        //    "Text", typeof(string), typeof(PopUpControl), new PropertyMetadata(default(string)));

        //public string Text
        //{
        //    get => (string)GetValue(TextProperty);
        //    set => SetValue(TextProperty, value);
        //}
        public BankNameControl()
        {
            InitializeComponent();
        }
    }
}
