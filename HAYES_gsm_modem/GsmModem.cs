using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using System.IO.Ports;
using System.Threading;
using System.Text.RegularExpressions;

namespace HAYES_gsm_modem
{
    public class GsmModem : ILink, IMessage
    {
        /// <summary>
        /// COM-порт модема
        /// </summary>
        private readonly SerialPort _port;

        /// <summary>
        /// Основной таймер для отслеживания состояния модема
        /// </summary>
        private readonly System.Timers.Timer _timer;

        /// <summary>
        /// Таймер для отслеживания неактивности канала связи
        /// </summary>
        private readonly System.Timers.Timer _inactiveTimer; 

        /// <summary>
        /// Счетчик попыток установить соеденение
        /// </summary>
        private int currentTryNumber = 1;

        /// <summary>
        /// Текущее состояние подкючения
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Вызываемый номер сим-карты
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Средняя задержка в канале связи (2000 мс)
        /// </summary>
        public int LinkDelay { get; set; }

        /// <summary>
        /// Время ожидания соеденения (15000 мс)
        /// </summary>
        public int ConnectionTimeOut { get; set; }

        /// <summary>
        /// Время неактивности канала после которого он закрывается (сбрасывается при каждой отправке или получении данных) (120000 мс)
        /// </summary>
        public int InactiveTimeout { get; set; }

        /// <summary>
        /// Кол-во попыток установить соеденение
        /// </summary>
        public int TryCount { get; set; }

        /// <summary>
        /// Текщее сосотяние модема
        /// </summary>
        public string ModemState { get; private set; }

        /// <summary>
        /// Оператор
        /// </summary>
        public string Vendor { get; private set; }

        /// <summary>
        /// Уровень сигнала
        /// </summary>
        public double SignalStrenght { get; private set; }

        /// <summary>
        /// Событие получения информации
        /// </summary>
        public event EventHandler<LinkEventArgs> DataRecieved = delegate { };
        /// <summary>
        /// Событие установления соеденения
        /// </summary>
        public event EventHandler<EventArgs> Connected = delegate { };
        /// <summary>
        /// Событие разрва соеденения
        /// </summary>
        public event EventHandler<EventArgs> Disconnected = delegate { };
        /// <summary>
        /// Событие отправки сообщения
        /// </summary>
        public event EventHandler<MessageDataEventArgs> Message = delegate { };

        /// <summary>
        /// Создает экземпляр класса GSM модем c параметрами по умолчанию (8-N-1, 9600)
        /// </summary>
        /// <param name="portName">COM порт модема</param>
        public GsmModem(string portName) : this(portName, 8, StopBits.One, 9600, Parity.None)
        { }

        /// <summary>
        /// Создает экземпляр класса GSM модем
        /// </summary>
        /// <param name="portName">COM-порт</param>
        /// <param name="dataBits">Биты данных</param>
        /// <param name="stopBits">Стоповые биты</param>
        /// <param name="baudeRate">Скорость</param>
        /// <param name="parity">Четность</param>
        public GsmModem(string portName, int dataBits, StopBits stopBits, int baudeRate, Parity parity)
        {
            _port = new SerialPort();
            _port.PortName = portName;
            _port.StopBits = stopBits;
            _port.DataBits = dataBits;
            _port.BaudRate = baudeRate;
            _port.Parity = parity;

            LinkDelay = 2000;
            ConnectionTimeOut = 15000;
            InactiveTimeout = 60000;
            IsConnected = false;
            TryCount = 3;

            _timer = new System.Timers.Timer();
            _timer.Stop();

            _inactiveTimer = new System.Timers.Timer();
            _inactiveTimer.AutoReset = false;
            _inactiveTimer.Interval = InactiveTimeout;
            _inactiveTimer.Elapsed += _inactiveTimer_Elapsed;
            _inactiveTimer.Stop();

            
        }

        /// <summary>
        /// Обработчик события таймера неактивности канала связи
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _inactiveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Disconnect();
            ClosePort();
            ((System.Timers.Timer)sender).Stop();
        }

        /// <summary>
        /// Очистка входящего и исходящего буфера порта
        /// </summary>
        public void ClearBuffer()
        {
            if (_port != null)
            {
                if (_port.IsOpen)
                {
                    _port.DiscardInBuffer();
                    _port.DiscardOutBuffer();
                }
            }
        }

        /// <summary>
        /// Отправка команды установить соеденение
        /// </summary>
        /// <returns>Статус отправки команды</returns>
        public bool Connect()
        {
            if (OpenPort()) //Если удалось занять COM-порт
            {
                _timer.Stop();
                _timer.Interval = ConnectionTimeOut;

                string modemMessage = "Calling " + ConnectionString + ", try: " + currentTryNumber + " of: " + TryCount.ToString();
                Message(this, new MessageDataEventArgs() { MessageString = modemMessage, MessageType = MessageType.Normal });

                byte[] callString = Encoding.Default.GetBytes("ATDP" + ConnectionString + "\r");
                if (Send(callString)) return true;
                else return false;


            }
            else return false;
        }

        /// <summary>
        /// Отправка комманд на разрыв соеденения
        /// </summary>
        public void Disconnect()
        {
            if (OpenPort())
            {
                try
                {
                    _port.Write("AT%P");
                    Thread.Sleep(1500);

                    _port.Write("+++");
                    Thread.Sleep(1500);

                    _port.Write("ATH0\r");
                    Thread.Sleep(1500);

                    IsConnected = false;
                    Disconnected(this, null);
                    ClosePort();

                }
                catch (Exception ex)
                {
                    Message(this, new MessageDataEventArgs { MessageString = ex.Message, MessageType = MessageType.Error });
                }
            }
        }

        /// <summary>
        /// Отправить данные
        /// </summary>
        /// <param name="data">Массив байт для отправки</param>
        /// <returns></returns>
        public bool Send(byte[] data)
        {
            return Send(data, data.Length);
        }

        /// <summary>
        /// Отправить данные
        /// </summary>
        /// <param name="data">Массив байт для отправки</param>
        /// <param name="length">Длинна отправляемого массива</param>
        /// <returns></returns>
        public bool Send(byte[] data, int length)
        {
            _inactiveTimer.Stop();
            _inactiveTimer.Start();
            try
            {
                Thread.Sleep(250);
                _port.Write(data, 0, length);
                return true;
            }
            catch (Exception ex)
            {
                Message(this, new MessageDataEventArgs { MessageString = ex.Message, MessageType = MessageType.Error });
                return false;
            }
        }

        /// <summary>
        /// Открыть порт
        /// </summary>
        /// <returns>Статус операции</returns>
        private bool OpenPort()
        {
            if (!_port.IsOpen)
            {
                try
                {
                    _port.Open();
                    _port.DataReceived += Port_DataReceived;
                    _port.ErrorReceived += Port_ErrorReceived;
                    return true;
                }
                catch (Exception ex)
                {
                    Message(this, new MessageDataEventArgs { MessageString = ex.Message, MessageType = MessageType.Error });
                    return false;
                }
            }
            else return true;
        }

        /// <summary>
        /// Закрыть порт
        /// </summary>
        /// <returns>Статус операции</returns>
        private bool ClosePort()
        {
            if (_port.IsOpen)
            {
                try
                {
                    _port.Close();
                    _port.DataReceived -= Port_DataReceived;
                    _port.ErrorReceived -= Port_ErrorReceived;
                    return true;
                }
                catch (Exception ex)
                {
                    Message(this, new MessageDataEventArgs { MessageString = ex.Message, MessageType = MessageType.Error });
                    return false;
                }
            }
            else return true;
        }

        /// <summary>
        /// Обработчик события поступления данных в порт
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            _inactiveTimer.Stop();
            _inactiveTimer.Start();

            List<byte> buf = new List<byte>();
            
            do 
            {
                buf.Add(Convert.ToByte(_port.ReadByte()));

                if (_port.BytesToRead == 0)
                    Thread.Sleep(250);
            }
            while (_port.BytesToRead != 0);

            string strBuf = Encoding.Default.GetString(buf.ToArray());
            strBuf = strBuf.Replace("\r", "");
            strBuf = strBuf.Replace("\n", "");

            _timer.Stop();
            Thread.Sleep(250);

            if (strBuf.StartsWith("CON"))
            {
                IsConnected = true;
                Thread.Sleep(250);
                Connected(this, null);
                Message(this, new MessageDataEventArgs { MessageString = strBuf, MessageType = MessageType.Normal });
                currentTryNumber = 1;
                buf = null;
                return;
            }

            if (strBuf.StartsWith("DIA") || strBuf.StartsWith("RIN"))
            {
                Message(this, new MessageDataEventArgs { MessageString = strBuf, MessageType = MessageType.Normal });
                buf = null;
                return;
            }

            if ((strBuf.StartsWith("NO") || strBuf.StartsWith("BUS")))
            {
                Message(this, new MessageDataEventArgs { MessageString = strBuf, MessageType = MessageType.Normal });
                if (!IsConnected)
                {
                    if (currentTryNumber < TryCount)
                    {
                        currentTryNumber++;
                        ClosePort();
                        Connect();
                        return;
                    }
                    else
                    {
                        Message(this, new MessageDataEventArgs { MessageString = "Не удалось связаться с удаленным модемом", MessageType = MessageType.Error });
                        currentTryNumber = 1;
                    }
                }
                else
                {
                    Message(this, new MessageDataEventArgs { MessageString = "Соеденение разорвано", MessageType = MessageType.Error });
                    IsConnected = false;
                    Disconnected(this, null);
                    ClosePort();
                }
                return;
            }

            if (strBuf.StartsWith("ATDP"))
            {
                Message(this, new MessageDataEventArgs { MessageString = strBuf, MessageType = MessageType.Normal });
                buf = null;
                return;
            }

            if (strBuf == "OK")
            {
                ModemState = "OK";
                return;
            }

            if (strBuf.StartsWith("ATH"))
            {
                return;
            }

            if (strBuf.StartsWith("+CSQ"))
            {
                _timer.Stop();
                SignalStrenght = Convert.ToInt32(Regex.Replace(strBuf, @"[^\d]+", ""));
                SignalStrenght = -113 + (SignalStrenght / 100) * 2; // (signalStrenght * 100) / 300;
                return;
            }

            if (strBuf.StartsWith("+COPS"))
            {
                _timer.Stop();
                Regex regex = new Regex(@"\D*[^,],");
                Vendor = regex.Matches(strBuf)[1].Value.Replace(",", "");
                return;
            }
            DataRecieved(this, new LinkEventArgs() { Buffer = buf.ToArray() });
            buf = null;
        }

        /// <summary>
        /// Обработчик события ошибки порта
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Message(this, new MessageDataEventArgs { MessageString = "Ошибка порта " + e.EventType.ToString(), MessageType = MessageType.Error });
        }
    }
}

