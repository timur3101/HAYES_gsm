using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    /// <summary>
    /// Перечисление типов сообщений
    /// </summary>
    public enum MessageType : int { SendBytes, ReceiveBytes, Error, Warning, Normal, NormalBold, Good, ToolBarInfo, MsgBox }

    /// <summary>
    /// Аргументы для события сообщения из канала связи
    /// </summary>
    public class MessageDataEventArgs : EventArgs
    {
        /// <summary>
        /// Сообщение в массиве байт
        /// </summary>
        private byte[] bytes;

        /// <summary>
        /// Сообщение в строке
        /// </summary>
        private string str;

        /// <summary>
        /// Сообщение в строке
        /// </summary>
        public string MessageString
        {
            get { return str; }
            set
            {
                str = value;
                bytes = Encoding.Default.GetBytes(str);
            }
        }

        /// <summary>
        /// Сообщение в массиве байт
        /// </summary>
        public byte[] MessageBytes
        {
            get { return bytes; }
            set
            {
                bytes = value;
                str = Encoding.Default.GetString(bytes);
            }
        }

        /// <summary>
        /// Тип сообщения
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// Длинна сообщения
        /// </summary>
        public int Length { get; set; }
    }

    /// <summary>
    /// Интерфейс сообщение
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// События отправки сообщения, различные сообщения о статусе соединения, ошибках и тд
        /// </summary>
        event EventHandler<MessageDataEventArgs> Message;
    }
}
