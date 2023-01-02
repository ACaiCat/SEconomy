using System;
using Jint.Native;

namespace Wolfje.Plugins.Jist.stdlib
{
	internal class RunAt
	{
		public Guid RunAtID { get; set; }

		public double AtTime { get; set; }

		public JsValue Func { get; set; }

		public bool Enabled { get; set; }

		public bool ExecutedInIteration { get; set; }

		public static double GetRawTime(int hours, int minutes)
		{
			decimal num = (decimal)hours + (decimal)minutes / 60.0m;
			num -= 4.50m;
			if (num < 0.00m)
			{
				num += 24.00m;
			}
			if (num >= 15.00m)
			{
				return (double)((num - 15.00m) * 3600.0m);
			}
			return (double)(num * 3600.0m);
		}

		public RunAt(double AtTime, JsValue func)
		{
			RunAtID = default(Guid);
			this.AtTime = AtTime;
			Func = func;
			Enabled = true;
		}

		public RunAt(int hours, int minutes, JsValue func)
		{
			RunAtID = default(Guid);
			AtTime = GetRawTime(hours, minutes);
			Func = func;
			Enabled = true;
		}
	}
}
