using System;

namespace ClassLibrary
{
    /// <summary>
    /// Имеет идентификатор.
    /// </summary>
    public class GUIDed
    {
        /// <summary>
        /// Возвращает уникальный идентификатор.
        /// </summary>
        public Guid ID { get; private set; } = Guid.NewGuid();
    }
}
