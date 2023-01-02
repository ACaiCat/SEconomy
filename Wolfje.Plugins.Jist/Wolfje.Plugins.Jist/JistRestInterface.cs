using System;
using Rests;
using TShockAPI;

namespace Wolfje.Plugins.Jist
{
	public class JistRestInterface : IDisposable
	{
		protected JistPlugin _plugin;

		public JistRestInterface(JistPlugin plugin)
		{
			_plugin = plugin;
			TShock.RestApi.Register(new SecureRestCommand("/_jist/v1/send", rest_send, "jist.rest.send"));
		}

		private object rest_send(RestRequestArgs args)
		{
			string snippet;
			if (string.IsNullOrEmpty(snippet = args.Parameters["p"]))
			{
				return new RestObject("500") { { "response", "Parameter is null" } };
			}
			return new RestObject { 
			{
				"response",
				JistPlugin.Instance.Eval(snippet)
			} };
		}

		~JistRestInterface()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
		}
	}
}
