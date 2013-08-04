using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cmd = Destrier.Redis.Core.RedisCommandLiteral;

namespace Destrier.Redis
{
    public interface IServerCommands
    {
        long GetDBSize();
        void Save();
        void BackgroundSave();
        void Shutdown();
        void FlushAll();
        void FlushDb();
        DateTime GetLastSave();
        string ConfigGet(String parameter);
        Boolean ConfigSet(String parameter, String value);
        Boolean ConfigRewrite();
        void ConfigResetStat();
        Boolean ClientKill(String host, int port);
        IEnumerable<String> ClientList();
        String ClientGetName();
        Boolean ClientSetName(String name);
        DateTime Time();
    }

    public partial class RedisClient : IServerCommands
    {
        public long GetDBSize()
        {
            _connection.Send(cmd.DBSIZE);
            return _connection.ReadReply().LongValue.Value;
        }

        public string ConfigGet(String parameter)
        {
            _connection.Send(cmd.CONFIG, cmd.GET, parameter);
            return _connection.ReadReply().ToString();
        }

        public Boolean ConfigSet(String parameter, String value)
        {
            _connection.Send(cmd.CONFIG, cmd.SET, parameter, value);
            return _connection.ReadReply().IsSuccess;
        }

        public Boolean ConfigRewrite()
        {
            _connection.Send(cmd.CONFIG, cmd.REWRITE);
            return _connection.ReadReply().IsSuccess;
        }

        public void ConfigResetStat()
        {
            _connection.Send(cmd.CONFIG, cmd.RESETSTAT);
            _connection.ReadReply();
        }

        public void Save()
        {
            _connection.Send(cmd.SAVE);
            _connection.ReadReply();
        }

        public void BackgroundSave()
        {
            _connection.Send(cmd.BGSAVE);
            _connection.ReadReply();
        }

        public void Shutdown()
        {
            _connection.Send(cmd.SHUTDOWN);
            _connection.ReadReply();
        }

        public void FlushAll()
        {
            _connection.Send(cmd.FLUSHALL);
            _connection.ReadReply();
        }

        public void FlushDb()
        {
            _connection.Send(cmd.FLUSHDB);
            _connection.ReadReply();
        }

        public DateTime GetLastSave()
        {
            _connection.Send(cmd.LASTSAVE);
            return _connection.ReadReply().DateTimeValue.Value;
        }

        public Boolean ClientKill(String host, int port)
        {
            _connection.Send(cmd.CLIENT, cmd.KILL, String.Format("{0}:{1}", host, port));
            return _connection.ReadReply().IsSuccess;
        }

        public IEnumerable<String> ClientList()
        {
            _connection.Send(cmd.CLIENT, cmd.LIST);
            return _connection.ReadMultiBulkReply().Select(r => r.ToString());
        }

        public String ClientGetName()
        {
            _connection.Send(cmd.CLIENT, cmd.GETNAME);
            return _connection.ReadReply().ToString();
        }

        public Boolean ClientSetName(String name)
        {
            _connection.Send(cmd.CLIENT, cmd.SETNAME, name);
            return _connection.ReadReply().IsSuccess;
        }

        public DateTime Time()
        {
            _connection.Send(cmd.TIME);
            return _connection.ReadReply().DateTimeValue.Value;
        }
    }
}
