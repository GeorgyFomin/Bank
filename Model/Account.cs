using System;
using System.IO;

namespace ClassLibrary
{
    public class Account : GUIDed
    {
        /// <summary>
        /// Хранит заголовок для текстового представления счета.
        /// </summary>
        public static readonly string header = string.Format("№\t\tSize\t\t\tRate\tCap");
        #region Properties
        /// <summary>
        /// Возвращает номер счета.
        /// </summary>
        public uint Number { get; }
        /// <summary>
        /// Устанавливает и возвращает ссылку на ID клиента, которому принадлежит счет.
        /// </summary>
        public Guid ClientID { get; set; }
        /// <summary>
        /// Устанавливает и возвращает размеры счета.
        /// </summary>
        public decimal Size { get; set; }
        /// <summary>
        /// Устанавливает и возвращает доходность в процентах.
        /// </summary>
        public double Rate { get; set; }
        /// <summary>
        /// Устанавливает и возвращает флаг капитализации вклада.
        /// </summary>
        public bool Cap { get; set; }
        #endregion
        public Account() : base() => Number = (uint)GetHashCode();
        public string AccFields() => $"{Number,-16}{Size,-16:n}\t{Rate:g3}\t{Cap}";
        public string Info() => string.Format(header + "\n" + AccFields());
        #region Printing
        /// <summary>
        /// Печатает заголовок полей счета.
        /// </summary>
        /// <param name="tw">Райтер.</param>
        public static void PrintHeader(TextWriter tw) => tw.WriteLine(header);
        /// <summary>
        /// Печатает поля счета.
        /// </summary>
        /// <param name="tw">Райтер.</param>
        public void PrintFields(TextWriter tw) => tw.WriteLine(AccFields());
        /// <summary>
        /// Печатает информацию о счете.
        /// </summary>
        /// <param name="tw"></param>
        public void Print(TextWriter tw) => tw.WriteLine(this);
        #endregion
        public override string ToString() => (Size < 0 ? "Loan " : "Deposit ") + "№" + $"{Number};Size {Size};Rate {Rate:g3};Cap {Cap}";
    }
}
