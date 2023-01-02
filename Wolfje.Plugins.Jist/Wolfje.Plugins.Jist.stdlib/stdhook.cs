using System;
using System.Collections;
using System.Linq;
using Jint.Native;
using TerrariaApi.Server;
using Wolfje.Plugins.Jist.Framework;

namespace Wolfje.Plugins.Jist.stdlib
{
	public class stdhook : stdlib_base
	{
		protected ArrayList jistHooks;

		public stdhook(JistEngine engine)
			: base(engine)
		{
			base.Provides = "hook";
			jistHooks = new ArrayList();
		}

		protected Guid AddHook<T>(JistHook<T> hook) where T : EventArgs
		{
			if (hook == null)
			{
				return Guid.Empty;
			}
			lock (jistHooks)
			{
				jistHooks.Add(hook);
			}
			return hook.HookID;
		}

		public void UnregisterAllHooks()
		{
			if (jistHooks == null)
			{
				return;
			}
			lock (jistHooks)
			{
				foreach (IDisposable jistHook in jistHooks)
				{
					jistHook.Dispose();
				}
				jistHooks.Clear();
			}
		}

		[JavascriptFunction(new string[] { "jist_hook_register", "jist_hook" })]
		public Guid RegisterHook(string hookName, JsValue func)
		{
			Guid result = Guid.Empty;
			switch (hookName.ToLower())
			{
			case "on_chat":
			case "chat":
				result = AddHook(new JistHook<ServerChatEventArgs>(engine, ServerApi.Hooks.ServerChat, func));
				break;
			case "on_join":
				result = AddHook(new JistHook<JoinEventArgs>(engine, ServerApi.Hooks.ServerJoin, func));
				break;
			case "on_leave":
				result = AddHook(new JistHook<LeaveEventArgs>(engine, ServerApi.Hooks.ServerLeave, func));
				break;
			}
			return result;
		}

		[JavascriptFunction(new string[] { "jist_hook_unregister", "jist_unhook" })]
		public void UnregisterHook(Guid guid)
		{
			if (guid == Guid.Empty)
			{
				return;
			}
			JistHook<EventArgs> jistHook;
			lock (jistHooks)
			{
				if ((jistHook = jistHooks.OfType<JistHook<EventArgs>>().FirstOrDefault((JistHook<EventArgs> i) => i.HookID == guid)) == null)
				{
					return;
				}
			}
			jistHook.Dispose();
			lock (jistHooks)
			{
				jistHooks.Remove(jistHook);
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				UnregisterAllHooks();
			}
		}
	}
}
