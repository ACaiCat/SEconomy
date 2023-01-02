using System;
using Jint.Native;

namespace Wolfje.Plugins.Jist.stdlib
{
	internal class RecurringFunction
	{
		public Guid RecurrenceID { get; private set; }

		public int Seconds { get; private set; }

		public JsValue Function { get; private set; }

		public DateTime NextRunTime { get; private set; }

		public RecurringFunction(int Hours, int Minutes, int Seconds, JsValue Func)
		{
			RecurrenceID = Guid.NewGuid();
			Function = Func;
			this.Seconds += Hours * 3600;
			this.Seconds += Minutes * 60;
			this.Seconds += Seconds;
			NextRunTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(this.Seconds));
		}

		public void ExecuteAndRecur()
		{
			if (DateTime.UtcNow < NextRunTime || JistPlugin.Instance == null)
			{
				return;
			}
			try
			{
				JistPlugin.Instance.CallFunction(Function, this);
			}
			catch (Exception ex)
			{
				ScriptLog.ErrorFormat("recurring", "Error occured on a recurring task function: " + ex.Message);
			}
			finally
			{
				Recur();
			}
		}

		protected void Recur()
		{
			NextRunTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(Seconds));
		}

		public override string ToString()
		{
			return string.Format("Task {0}: {1} secs, next in {2}", arg2: NextRunTime.Subtract(DateTime.UtcNow).ToString("hh\\:mm\\:ss"), arg0: RecurrenceID, arg1: Seconds);
		}
	}
}
