using System.Collections.Generic;
using System.Data;
using TShockAPI.DB;

namespace Wolfje.Plugins.SEconomy.Extensions
{
	public static class QueryResultExtensions
	{
		public static IEnumerable<IDataReader> AsEnumerable(this QueryResult res)
		{
			return res.Reader.AsEnumerable();
		}
	}
}
