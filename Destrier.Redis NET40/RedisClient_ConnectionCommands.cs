using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cmd = Destrier.Redis.Core.RedisCommandLiteral;

namespace Destrier.Redis
{
    public interface IConnectionCommands
    {
        Boolean Auth(String password);
        String Echo(String message);
        Boolean Ping();
        void Quit();
        Boolean Select(int db);
    }

    public partial class RedisClient : IConnectionCommands
    {
        public Boolean Auth(String password)
        {
            _connection.Send(cmd.AUTH, password);
            return _connection.ReadReply().IsSuccess;
        }

        public String Echo(String message)
        {
            _connection.Send(cmd.ECHO, message);
            return _connection.ReadReply().ToString();
        }

        public Boolean Ping()
        {
            _connection.Send(cmd.PING);
            return _connection.ReadReply().IsSuccess;
        }

        public void Quit()
        {
            _connection.Send(cmd.QUIT);
        }

        public Boolean Select(int db)
        {
            _connection.Send(cmd.SELECT, db);
            return _connection.ReadReply().IsSuccess;
        }
    }
}
