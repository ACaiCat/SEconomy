using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Jint.Native;
using Terraria;
using Wolfje.Plugins.Jist.Framework;

namespace Wolfje.Plugins.Jist.stdlib
{
	public class stdtask : stdlib_base
	{
		protected System.Timers.Timer oneSecondTimer;

		protected System.Timers.Timer highPrecisionTimer;

		private List<RecurringFunction> recurList;

		private List<RunAt> runAtList;

		private List<CancellationTokenSource> runAfterList;

		protected readonly object syncRoot = new object();

		public stdtask(JistEngine engine)
			: base(engine)
		{
			highPrecisionTimer = new System.Timers.Timer(100.0);
			oneSecondTimer = new System.Timers.Timer(1000.0);
			oneSecondTimer.Elapsed += oneSecondTimer_Elapsed;
			highPrecisionTimer.Elapsed += highPrecisionTimer_Elapsed;
			oneSecondTimer.Start();
			recurList = new List<RecurringFunction>();
			runAtList = new List<RunAt>();
			runAfterList = new List<CancellationTokenSource>();
			highPrecisionTimer.Start();
		}

		private void highPrecisionTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			double location = 0.0;
			Interlocked.Exchange(ref location, Main.time);
			if (!(location > 0.0) || !(location < 200.0))
			{
				return;
			}
			for (int i = 0; i < runAtList.Count; i++)
			{
				lock (syncRoot)
				{
					RunAt runAt = runAtList.ElementAtOrDefault(i);
					if (runAt != null)
					{
						runAt.ExecutedInIteration = false;
					}
				}
			}
		}

		protected void oneSecondTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			for (int i = 0; i < recurList.Count; i++)
			{
				RecurringFunction recurringFunction;
				lock (syncRoot)
				{
					recurringFunction = recurList.ElementAtOrDefault(i);
				}
				if (recurringFunction != null)
				{
					try
					{
						recurringFunction.ExecuteAndRecur();
					}
					catch (Exception ex)
					{
						ScriptLog.ErrorFormat("recurring", "Error on recurring rule: " + ex.Message);
					}
				}
			}
			for (int j = 0; j < runAtList.Count; j++)
			{
				RunAt runAt;
				lock (syncRoot)
				{
					runAt = runAtList.ElementAtOrDefault(j);
				}
				if (runAt == null || engine == null || Main.time <= runAt.AtTime || runAt.ExecutedInIteration)
				{
					continue;
				}
				try
				{
					engine.CallFunction(runAt.Func, runAt);
				}
				catch (Exception ex2)
				{
					ScriptLog.ErrorFormat("recurring", "Error on recurring rule: " + ex2.Message);
				}
				finally
				{
					runAt.ExecutedInIteration = true;
				}
			}
		}

		internal List<RecurringFunction> DumpTasks()
		{
			return recurList;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				lock (syncRoot)
				{
					recurList.Clear();
				}
				oneSecondTimer.Stop();
				oneSecondTimer.Dispose();
				highPrecisionTimer.Stop();
				highPrecisionTimer.Dispose();
				CancelRunAfters();
			}
		}

		[JavascriptFunction(new string[] { "run_after", "jist_run_after" })]
		public void RunAfterAsync(int AfterMilliseconds, JsValue Func, params object[] args)
		{
			CancellationTokenSource source = new CancellationTokenSource();
			lock (syncRoot)
			{
				runAfterList.Add(source);
			}
			Action action = async delegate
			{
				try
				{
					await Task.Delay(AfterMilliseconds, source.Token);
				}
				catch (TaskCanceledException)
				{
					return;
				}
				if (!source.Token.IsCancellationRequested)
				{
					try
					{
						engine.CallFunction(Func, null, args);
					}
					catch (TaskCanceledException)
					{
					}
					if (!source.Token.IsCancellationRequested)
					{
						lock (syncRoot)
						{
							runAfterList.Remove(source);
						}
					}
				}
			};
			Task.Factory.StartNew(action, source.Token);
		}

		internal void CancelRunAfters()
		{
			lock (syncRoot)
			{
				foreach (CancellationTokenSource runAfter in runAfterList)
				{
					runAfter.Cancel();
				}
				runAfterList.Clear();
			}
		}

		[JavascriptFunction(new string[] { "run_at", "jist_run_at" })]
		public void AddAt(int Hours, int Minutes, JsValue Func)
		{
			lock (syncRoot)
			{
				runAtList.Add(new RunAt(Hours, Minutes, Func));
			}
		}

		[JavascriptFunction(new string[] { "add_recurring", "jist_task_queue" })]
		public void AddRecurring(int Hours, int Minutes, int Seconds, JsValue Func)
		{
			lock (syncRoot)
			{
				recurList.Add(new RecurringFunction(Hours, Minutes, Seconds, Func));
			}
		}
	}
}
