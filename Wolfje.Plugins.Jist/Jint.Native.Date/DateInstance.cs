using System;
using Jint.Native.Object;
using Jint.Runtime;

namespace Jint.Native.Date
{
	public class DateInstance : ObjectInstance
	{
		internal static readonly double Max = (DateTime.MaxValue - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

		internal static readonly double Min = 0.0 - (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) - DateTime.MinValue).TotalMilliseconds;

		public override string Class => "Date";

		public double PrimitiveValue { get; set; }

		public DateInstance(Engine engine)
			: base(engine)
		{
		}

		public DateTime ToDateTime()
		{
			if (double.IsNaN(PrimitiveValue) || PrimitiveValue > Max || PrimitiveValue < Min)
			{
				throw new JavaScriptException(base.Engine.RangeError);
			}
			return DateConstructor.Epoch.AddMilliseconds(PrimitiveValue);
		}
	}
}
