using System.Data;

namespace Wolfje.Plugins.SEconomy.Extensions
{
	public static class MySqlCommandExtensions
	{
		public static IDbDataParameter AddParameter(this IDbCommand command, string name, object data)
		{
			IDbDataParameter dbDataParameter = command.CreateParameter();
			dbDataParameter.ParameterName = name;
			dbDataParameter.Value = data;
			command.Parameters.Add(dbDataParameter);
			return dbDataParameter;
		}
	}
}
