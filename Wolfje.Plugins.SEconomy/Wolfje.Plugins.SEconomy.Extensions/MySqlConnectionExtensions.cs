using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace Wolfje.Plugins.SEconomy.Extensions
{
	public static class MySqlConnectionExtensions
	{
		public static async Task<int> QueryAsync(this IDbConnection db, string query, params object[] args)
		{
			IDbConnection connection = db.CloneEx();
			IDbCommand command = null;
			int result;
			try
			{
				connection.Open();
				command = connection.CreateCommand();
				command.CommandText = query;
				command.CommandTimeout = 60;
				for (int i = 0; i < args.Length; i++)
				{
					command.AddParameter("@" + i, args[i]);
				}
				result = await Task.Run(() => command.ExecuteNonQuery());
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError("seconomy mysql: QueryAsync error: {0}", ex.Message);
				return -1;
			}
			finally
			{
				if (command != null)
				{
					command.Dispose();
				}
				connection?.Dispose();
			}
			return result;
		}

		public static IDataReader QueryReaderExisting(this IDbConnection db, string query, params object[] args)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			IDataReader dataReader = null;
			using (IDbCommand dbCommand = db.CreateCommand())
			{
				dbCommand.CommandText = query;
				dbCommand.CommandTimeout = 60;
				for (int i = 0; i < args.Length; i++)
				{
					dbCommand.AddParameter("@" + i, args[i]);
				}
				try
				{
					dataReader = dbCommand.ExecuteReader();
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError("seconomy mysql: Query error: {0}", ex.Message);
					dataReader?.Dispose();
				}
			}
			stopwatch.Stop();
			if (stopwatch.Elapsed.TotalSeconds > 10.0)
			{
				TShock.Log.ConsoleError("seconomy mysql: Your MySQL server took {0} seconds to respond!\r\nConsider squashing your journal.", stopwatch.Elapsed.TotalSeconds);
			}
			return dataReader;
		}

		public static int QueryTransaction(this IDbConnection db, IDbTransaction trans, string query, params object[] args)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			int result = 0;
			using (IDbCommand dbCommand = db.CreateCommand())
			{
				dbCommand.CommandText = query;
				dbCommand.Transaction = trans;
				dbCommand.CommandTimeout = 60;
				for (int i = 0; i < args.Length; i++)
				{
					dbCommand.AddParameter("@" + i, args[i]);
				}
				try
				{
					result = dbCommand.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError("seconomy mysql: Query error: {0}", ex.Message);
					result = -1;
				}
			}
			stopwatch.Stop();
			if (stopwatch.Elapsed.TotalSeconds > 10.0)
			{
				TShock.Log.ConsoleError("seconomy mysql: Your MySQL server took {0} seconds to respond!\r\nConsider squashing your journal.", stopwatch.Elapsed.TotalSeconds);
			}
			return result;
		}

		public static int QueryIdentity(this MySqlConnection olddb, string query, out long identity, params object[] args)
		{
			Stopwatch stopwatch = new Stopwatch();
			int result = 0;
			stopwatch.Start();
			identity = -1L;
			using (MySqlConnection mySqlConnection = new MySqlConnection(olddb.ConnectionString))
			{
				try
				{
					mySqlConnection.Open();
					using MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
					mySqlCommand.CommandText = query;
					mySqlCommand.CommandTimeout = 60;
					for (int i = 0; i < args.Length; i++)
					{
						mySqlCommand.AddParameter("@" + i, args[i]);
					}
					result = mySqlCommand.ExecuteNonQuery();
					identity = mySqlCommand.LastInsertedId;
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError("seconomy mysql: Query error: {0}", ex.Message);
					result = -1;
				}
			}
			stopwatch.Stop();
			if (stopwatch.Elapsed.TotalSeconds > 10.0)
			{
				TShock.Log.ConsoleError("seconomy mysql: Your MySQL server took {0} seconds to respond!\r\nConsider squashing your journal.", stopwatch.Elapsed.TotalSeconds);
			}
			return result;
		}

		public static int QueryIdentityTransaction(this MySqlConnection db, MySqlTransaction trans, string query, out long identity, params object[] args)
		{
			Stopwatch stopwatch = new Stopwatch();
			int result = 0;
			stopwatch.Start();
			using (MySqlCommand mySqlCommand = db.CreateCommand())
			{
				mySqlCommand.CommandText = query;
				mySqlCommand.Transaction = trans;
				mySqlCommand.CommandTimeout = 60;
				for (int i = 0; i < args.Length; i++)
				{
					mySqlCommand.AddParameter("@" + i, args[i]);
				}
				try
				{
					result = mySqlCommand.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError("seconomy mysql: Query error: {0}", ex.Message);
					result = -1;
				}
				identity = mySqlCommand.LastInsertedId;
			}
			stopwatch.Stop();
			if (stopwatch.Elapsed.TotalSeconds > 10.0)
			{
				TShock.Log.ConsoleError("seconomy mysql: Your MySQL server took {0} seconds to respond!\r\nConsider squashing your journal.", stopwatch.Elapsed.TotalSeconds);
			}
			return result;
		}

		public static T QueryScalar<T>(this MySqlConnection olddb, string query, params object[] args)
		{
			Stopwatch stopwatch = new Stopwatch();
			object obj = null;
			stopwatch.Start();
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(olddb.ConnectionString);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
				mySqlCommand.CommandText = query;
				mySqlCommand.CommandTimeout = 60;
				for (int i = 0; i < args.Length; i++)
				{
					mySqlCommand.AddParameter("@" + i, args[i]);
				}
				if ((obj = mySqlCommand.ExecuteScalar()) == null)
				{
					stopwatch.Stop();
					return default(T);
				}
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError("seconomy mysql: Query error: {0}", ex.Message);
				obj = default(T);
			}
			stopwatch.Stop();
			if (stopwatch.Elapsed.TotalSeconds > 10.0)
			{
				TShock.Log.ConsoleError("seconomy mysql: Your MySQL server took {0} seconds to respond!\r\nConsider squashing your journal.", stopwatch.Elapsed.TotalSeconds);
			}
			return (T)obj;
		}

		public static T QueryScalarExisting<T>(this IDbConnection db, string query, params object[] args)
		{
			Stopwatch stopwatch = new Stopwatch();
			object obj = null;
			stopwatch.Start();
			using (IDbCommand dbCommand = db.CreateCommand())
			{
				dbCommand.CommandText = query;
				dbCommand.CommandTimeout = 60;
				for (int i = 0; i < args.Length; i++)
				{
					dbCommand.AddParameter("@" + i, args[i]);
				}
				try
				{
					if ((obj = dbCommand.ExecuteScalar()) == null)
					{
						stopwatch.Stop();
						return default(T);
					}
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError("seconomy mysql: Query error: {0}", ex.Message);
					obj = default(T);
				}
			}
			stopwatch.Stop();
			if (stopwatch.Elapsed.TotalSeconds > 10.0)
			{
				TShock.Log.ConsoleError("seconomy mysql: Your MySQL server took {0} seconds to respond!\r\nConsider squashing your journal.", stopwatch.Elapsed.TotalSeconds);
				obj = default(T);
			}
			return (T)obj;
		}

		public static T QueryScalarTransaction<T>(this IDbConnection db, IDbTransaction trans, string query, params object[] args)
		{
			Stopwatch stopwatch = new Stopwatch();
			object obj = null;
			stopwatch.Start();
			using (IDbCommand dbCommand = db.CreateCommand())
			{
				dbCommand.CommandText = query;
				dbCommand.Transaction = trans;
				dbCommand.CommandTimeout = 60;
				for (int i = 0; i < args.Length; i++)
				{
					dbCommand.AddParameter("@" + i, args[i]);
				}
				try
				{
					if ((obj = dbCommand.ExecuteScalar()) == null)
					{
						return default(T);
					}
				}
				catch (Exception ex)
				{
					TShock.Log.ConsoleError("seconomy mysql: Query error: {0}", ex.Message);
					obj = default(T);
				}
			}
			stopwatch.Stop();
			if (stopwatch.Elapsed.TotalSeconds > 10.0)
			{
				TShock.Log.ConsoleError("seconomy mysql: Your MySQL server took {0} seconds to respond!\r\nConsider squashing your journal.", stopwatch.Elapsed.TotalSeconds);
			}
			return (T)obj;
		}
	}
}
