using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ClassLibrary
{
    public static class RandomBank
    {
        /// <summary>
        /// Хранит максимально возможную сумму вклада.
        /// </summary>
        const double MaxSize = 1_000_000_000;
        /// <summary>
        /// Хранит максимально возможную доходность вклада в процентах.
        /// </summary>
        const float MaxRate = 10;
        /// <summary>
        /// Хранит ссылку на генератор случайных чисел.
        /// </summary>
        static public readonly Random random = new Random();
        /// <summary>
        /// Возвращает случайный банк.
        /// </summary>
        /// <returns></returns>
        static public Bank GetBank() => new Bank(GetRandomString(4, random), GetRandomDeps(random.Next(1, 5), random));
        /// <summary>
        /// Возвращает список случайных отделов.
        /// </summary>
        /// <param name="v">Число отделов.</param>
        /// <param name="random">Генератор случайных чисел.</param>
        /// <returns></returns>
        private static ObservableCollection<Dep> GetRandomDeps(int v, Random random) =>
                   new ObservableCollection<Dep>(Enumerable.Range(0, v).
            Select(index => new Dep(GetRandomString(random.Next(1, 6), random), GetRandomClients(random.Next(1, 20), random))));
        /// <summary>
        /// Возвращает список случайных клиентов.
        /// </summary>
        /// <param name="v">Число клиентов.</param>
        /// <param name="random">Генератор случайных чисел.</param>
        /// <returns></returns>
        private static ObservableCollection<Client> GetRandomClients(int v, Random random) =>
            new ObservableCollection<Client>(Enumerable.Range(0, v).
            Select(index => new Client(GetRandomString(random.Next(3, 6), random), GetRandomAccounts(random.Next(1, 5), random))).
            ToList());
        /// <summary>
        /// Возвращает список случайных счетов.
        /// </summary>
        /// <param name="v">Число счетов.</param>
        /// <param name="random">Генератор случайных чисел.</param>
        /// <returns></returns>
        private static ObservableCollection<Account> GetRandomAccounts(int v, Random random) =>
            new ObservableCollection<Account>(Enumerable.
            Range(0, v).
            Select(index => new Account()
            {
                // Случайна доходность.
                Rate = MaxRate * (float)random.NextDouble(),
                // Случайная капитализация.
                Cap = random.NextDouble() < .5,
                // Случайный размер вклада (положительный) или кредита (отрицательный).
                Size = (decimal)(MaxSize * (2 * random.NextDouble() - 1))
            }).
            ToList());
        /// <summary>
        /// Генерирует случайную строку из латинских букв нижнего регистра..
        /// </summary>
        /// <param name="length">Длина строки.</param>
        /// <param name="random">Генератор случайных чисел.</param>
        /// <returns></returns>
        public static string GetRandomString(int length, Random random)
            => new string(Enumerable.Range(0, length).Select(x => (char)random.Next('a', 'z' + 1)).ToArray());
    }
}
