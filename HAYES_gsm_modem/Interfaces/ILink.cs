using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public class LinkEventArgs : EventArgs
    {
        public byte[] Buffer { get; set; }
    }

    public interface ILink
    {
        /// <summary>
        /// Состояние соеденения
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Номер порта, телефон, IP адрес
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Возможная максимальная задержка возникающая в канале связи (завязана на типе канала)
        /// </summary>
        int LinkDelay { get; set; }

        /// <summary>
        /// Таймаут ожидания подключения
        /// </summary>
        int ConnectionTimeOut { get; set; }

        /// <summary>
        /// Таймаут простоя 
        /// </summary>
        int InactiveTimeout { get; set; }

        /// <summary>
        /// Отправить данные в канал
        /// </summary>
        /// <param name="data">Данные для отправки</param>
        /// <returns>Результат операции</returns>
        bool Send(byte[] data);

        /// <summary>
        /// Отправить данные в канал
        /// </summary>
        /// <param name="data">Данные для отправки</param>
        /// <param name="length">Длина массива для отправки</param>
        /// <returns>Результат операции</returns>
        bool Send(byte[] data, int length);

        /// <summary>
        /// Установить соеденение по ConnectionString
        /// </summary>
        /// <returns>Результат операции</returns>
        bool Connect();

        /// <summary>
        /// Разорвать соеденение
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Очистить буфер
        /// </summary>
        void ClearBuffer();

        event EventHandler<LinkEventArgs> DataRecieved;
        event EventHandler<EventArgs> Connected;
        event EventHandler<EventArgs> Disconnected;
    }
}
