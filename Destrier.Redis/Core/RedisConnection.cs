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
        public Boolean IsConnected { get { return _socket.Connected; } }
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
                Send(RedisCommandLiteral.AUTH, this.Password);
                ReadForError();
            }
        }

        public void ReadForError()
        {
            int c = _stream.ReadByte();
            if (c == -1)
                throw new RedisException("RedisConnection :: No more data.");

            var s = _readSingleStatement();
            if (c == '-')
                throw new RedisException(s.StartsWith("ERR") ? s.Substring(4) : s);
        }

        private string _readSingleStatement()
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

        public RedisValue ReadReply()
        {
            string response = _readSingleStatement();
            if (String.IsNullOrEmpty(response))
            {
                response = _readSingleStatement();
                if (String.IsNullOrEmpty(response))
                    throw new RedisException("Zero length response after retry.");
            }

            if (response.StartsWith("-"))
                return new RedisValue() { StringValue = response };

            var firstChar = response[0];
            switch (firstChar)
            {
                case ':':
                    {
                        var result = Int64.Parse(response.Substring(1));
                        return new RedisValue() { LongValue = result };
                    }
                case '+':
                    {
                        return new RedisValue() { StringValue = response.Substring(1) };
                    }
                case '$':
                    {
                        var value = _readSingleStatement();
                        return new RedisValue() { StringValue = value };
                    }
                case '*':
                    throw new RedisException("MultiBlock response detected while reading a single response value.");
                default:
                    throw new RedisException("RedisConnection :: Unexpected Reply");
            }
        }

        public Byte[] ReadBinaryReply()
        {
            var response = _readSingleStatement();
            if(response.StartsWith("-"))
                throw new RedisException("RedisConnection :: Error");

            var size = int.Parse(response.Substring(1));
            if (size == -1)
                return null;

            var buffer = new byte[size];
            _stream.Read(buffer, 0, size);
            return buffer;
        }

        public IEnumerable<RedisValue> ReadMultiBulkReply()
        {
            string response = _readSingleStatement();
            if (String.IsNullOrEmpty(response))
            {
                response = _readSingleStatement();
                if (String.IsNullOrEmpty(response))
                    throw new RedisException("Zero length response after retry.");
            }

            if (response.StartsWith("-"))
            {
                yield return new RedisValue() { StringValue = response };
            }
            else
            {
                var firstChar = response[0];
                if (firstChar != '*')
                    throw new RedisException("Non-MultiBlock response detected while reading a multiblock response.");

                var count = default(int);
                count = int.Parse(response.Substring(1));

                for (int x = 0; x < count; x++)
                {
                    var value_length_line = _readSingleStatement();
                    //assume this line starts with a '$';
                    var value_length = int.Parse(value_length_line.Substring(1));

                    if (value_length == -1)
                        yield return new RedisValue() { IsNull = true };
                    else
                    {

                        var value_line = _readSingleStatement();
                        switch (value_line[0])
                        {
                            case ':':
                                var result = Int64.Parse(value_line.Substring(1));
                                yield return new RedisValue() { LongValue = result };
                                break;
                            case '+':
                                yield return new RedisValue() { StringValue = value_line.Substring(1) };
                                break;
                            case '*':
                                break;
                            default:
                                yield return new RedisValue() { StringValue = value_line.Trim() };
                                break;
                        }
                    }
                }
            }
            
        }

        public void Send(String command, params object[] args)
        {
            if (_socket == null || !_socket.Connected)
                throw new RedisException("RedisConnection :: Not connected to server.");

            try
            {
                var argTotal = args != null ? args.Length + 1 : 1;
                var argTotalMessage = String.Format("*{0}\r\n", argTotal);

                var cmdLength = command.Length;
                var cmdLengthMessage = String.Format("${0}\r\n", cmdLength);

                var cmdMessage = String.Format("{0}\r\n", command);

                _socket.Send(RedisDataFormatUtil.Encoding.GetBytes(argTotalMessage));
                _socket.Send(RedisDataFormatUtil.Encoding.GetBytes(cmdLengthMessage));
                _socket.Send(RedisDataFormatUtil.Encoding.GetBytes(cmdMessage));

                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        if (arg is Byte[])
                        {
                            var data = new MemoryStream(arg as Byte[]);

                            var data_len = data.Length;
                            var data_len_str = String.Format("${0}\r\n", data_len);
                            _socket.Send(RedisDataFormatUtil.Encoding.GetBytes(data_len_str));

                            byte[] buffer = new byte[XMIT_BUFFER_SIZE];
                            int bytesRead = 0;
                            do
                            {
                                bytesRead = data.Read(buffer, 0, XMIT_BUFFER_SIZE);
                                _socket.Send(buffer, bytesRead, SocketFlags.None);
                            } while (bytesRead > 0);

                            _socket.Send(EOL);
                        }
                        else if (arg is Stream)
                        {
                            var data = arg as Stream;
                            var data_len = data.Length;
                            var data_len_str = String.Format("${0}\r\n", data_len);
                            _socket.Send(RedisDataFormatUtil.Encoding.GetBytes(data_len_str));

                            byte[] buffer = new byte[XMIT_BUFFER_SIZE];
                            int bytesRead = 0;
                            do
                            {
                                bytesRead = data.Read(buffer, 0, XMIT_BUFFER_SIZE);
                                _socket.Send(buffer, bytesRead, SocketFlags.None);
                            } while (bytesRead > 0);

                            _socket.Send(EOL);
                        }
                        else
                        {
                            var str_arg = arg.ToString();
                            var str_arg_len = str_arg.Length;

                            var len_msg = String.Format("${0}\r\n", str_arg_len);
                            var str_arg_msg = String.Format("{0}\r\n", str_arg);

                            _socket.Send(RedisDataFormatUtil.Encoding.GetBytes(len_msg));
                            _socket.Send(RedisDataFormatUtil.Encoding.GetBytes(str_arg_msg));
                        }
                    }
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.ToString());
                OnDisconnected(null);
            }
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
                Send(RedisCommandLiteral.QUIT);
                _socket.Close();
                _socket = null;
            }   
        }
    }
}
