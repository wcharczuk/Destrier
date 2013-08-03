using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Destrier.Redis.Core
{
    public class RedisConnection : IDisposable
    {
        public const int SEND_TIMEOUT = -1;
        public const Int32 XMIT_BUFFER_SIZE = 64 * 1024; //64 kb
        public static readonly byte[] EOL = new byte[] { (byte)'\r', (byte)'\n' };

        private Socket _socket = null;
        private BufferedStream _stream = null;

        public RedisConnection(String host, int port, String password = null)
        {
            Host = host;
            Port = port;
            Password = password;
            Id = System.Guid.NewGuid();
        }

        /// <summary>
        /// Whether or not the connection has an active client session or not.
        /// </summary>
        public Boolean IsAssignedToClient { get; set; }

        public Guid Id { get; set; }

        #region Server Parameters
        public String Host { get; set; }
        public int Port { get; set; }
        public String Password { get; set; }
        #endregion

        #region Client Interactivity
        public Socket Socket { get { return _socket; } }
        public Stream Stream { get { return _stream; } }
        #endregion

        public void Connect()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.NoDelay = true;
            _socket.SendTimeout = SEND_TIMEOUT;
            _socket.Connect(Host, Port);
            if (!_socket.Connected)
            {
                _socket.Close();
                _socket = null;
                throw new RedisException("RedisConnection :: Cannot connect to server.");
            }
            _stream = new BufferedStream(new NetworkStream(_socket), 16*1024); //16kb buffer
            if (!String.IsNullOrWhiteSpace(Password))
            {
                SendCommand("AUTH {0}", this.Password);
                ReadForError();
            }
        }

        public void ReadForError()
        {
            int c = _stream.ReadByte();
            if (c == -1)
                throw new RedisException("RedisConnection :: No more data.");

            var s = ReadLine();
            if (c == '-')
                throw new RedisException(s.StartsWith("ERR") ? s.Substring(4) : s);
        }

        private string ReadLine()
        {
            var sb = new StringBuilder();
            byte c;

            while (true)
            {
                c = (byte)_stream.ReadByte();
                if (c == '\r')
                    continue;
                if (c == '\n')
                    break;
                sb.Append((char)c);
            }

            return sb.ToString();
        }

        public RedisDataValue ReadData()
        {
            string response = ReadLine();
            if (String.IsNullOrEmpty(response))
                response = ReadLine();
                    if(String.IsNullOrEmpty(response))
                        throw new RedisException("Zero length response after retry.");

            if (response.StartsWith("-"))
                return new RedisDataValue() { StringValue = response };

            var firstChar = response[0];
            switch (firstChar)
            {
                case '$':
                    {
                        if (response.Equals("$-1"))
                            return new RedisDataValue() { IsNull = true };

                        int length = int.Parse(response.Substring(1));

                        var buffer = new byte[length];
                        _stream.Read(buffer, 0, length);

                        return new RedisDataValue() { BinaryValue = buffer, StringValue = RedisDataFormat.FormatAsString(buffer) };
                    }
                case '*':
                    {
                        var length = int.Parse(response.Substring(1));
                        return length > 0 ? ReadData() : new RedisDataValue();
                    }
                case ':':
                    {
                        var result = Int64.Parse(response.Substring(1));
                        return new RedisDataValue() { LongValue = result };
                    }
                case '+':
                    {
                        return new RedisDataValue() { StringValue = response.Substring(1) };
                    }
                default:
                    throw new RedisException("RedisConnection :: Unexpected Reply");
            }
        }

        public void SendCommand(String commandFormat, params object[] arguments)
        {
            SendRawCommand(String.Format(commandFormat + "\r\n", arguments));
        }

        public void SendRawCommand(String command)
        {
            if (_socket == null)
                Connect();

            if (_socket == null)
                throw new RedisException("RedisConnection :: Cannot connect to server.");

            Log("Command => {0}", command);

            var buffer = Encoding.UTF8.GetBytes(command);

            try
            {
                _socket.Send(buffer);
            }
            catch (SocketException)
            {
                _socket.Close();
                _socket = null;

                OnDisconnected(null);
            }
        }

        public void SendData(Stream data)
        {
            byte[] buffer = new byte[XMIT_BUFFER_SIZE];

            int bytesRead = 0;
            do
            {
                bytesRead = data.Read(buffer, 0, XMIT_BUFFER_SIZE);
                _socket.Send(buffer, bytesRead, SocketFlags.None);
            } while (bytesRead > 0);

            _socket.Send(EOL);
        }

        private void Log(String message, params object[] args)
        {
#if DEBUG
            var messageFormatted = String.Format(message, args);
            var meta = String.Format("Redis Connection {0}", this.Id.ToString("N"));
            System.Diagnostics.Debug.WriteLine(String.Format("{0} :: {1}", meta, messageFormatted));
#endif
        }

        public event RedisConnectionStateChangedHandler ConnectionOpened;
        public void OnConnectionOpened(EventArgs e = null)
        {
            if (ConnectionOpened != null)
                ConnectionOpened(this, e);
        }

        public event RedisConnectionStateChangedHandler ConnectionReleased;
        public void OnConnectionReleased(EventArgs e = null)
        {
            if (ConnectionReleased != null)
                ConnectionReleased(this, e);
        }

        public event RedisConnectionStateChangedHandler Disconnected;
        public void OnDisconnected(EventArgs e)
        {
            if (Disconnected != null)
                Disconnected(this, e);
        }

        public void Dispose()
        {
            if (_socket != null)
            {
                SendRawCommand("QUIT\r\n");
                _socket.Close();
                _socket = null;
            }   
        }
    }
}
