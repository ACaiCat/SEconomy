using System;
using Jint.Native;
using TerrariaApi.Server;

namespace Wolfje.Plugins.Jist.stdlib
{
	public class JistHook<T> : IDisposable where T : EventArgs
	{
		protected HandlerCollection<T> collection;

		protected HookHandler<T> handler;

		protected TerrariaPlugin pluginInstance;

		protected bool enabled;

		public Guid HookID { get; protected set; }

		public JsValue Function { get; protected set; }

		public JistHook(JistEngine engine, HandlerCollection<T> collection, JsValue func)
		{
			JistHook<T> thisObject = this;
			HookID = default(Guid);
			enabled = true;
			this.collection = collection;
			pluginInstance = engine.PluginInstance;
			handler = delegate(T args)
			{
				engine.CallFunction(func, thisObject, args);
			};
			collection.Register(engine.PluginInstance, handler);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				collection.Deregister(pluginInstance, handler);
			}
		}
	}
}
