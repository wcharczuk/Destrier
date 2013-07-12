using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Destrier
{
    public class Update<T>
    {
        public Update()
        {
            _t = typeof(T);
            _parameters = new Dictionary<String, Object>();
            _builder = new CommandBuilder<T>(_command, _parameters);
        }

        private StringBuilder _command = null;
        private Type _t = null;
        private IDictionary<String, Object> _parameters = null;

        private CommandBuilder<T> _builder = null;
        public Update<T> Set<F>(Expression<Func<T, F>> expression, F value)
        {
            _builder.AddSet(expression, value);
            return this;
        }

        public Update<T> Where(Expression<Func<T, Boolean>> expression)
        {
            _builder.AddWhere(expression);
            return this;
        }

        public void Execute()
        {
            using (var cmd = Destrier.Execute.Command(Model.ConnectionString(_t)))
            {
                cmd.CommandText = _builder.GenerateUpdate();
                cmd.CommandType = System.Data.CommandType.Text;
                Destrier.Execute.Utility.AddParametersToCommand(_parameters, cmd);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
