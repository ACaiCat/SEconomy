using Jint.Native;
using Jint.Parser;

namespace Jint.Runtime
{
	public class Completion
	{
		public static string Normal = "normal";

		public static string Break = "break";

		public static string Continue = "continue";

		public static string Return = "return";

		public static string Throw = "throw";

		public string Type { get; private set; }

		public JsValue? Value { get; private set; }

		public string Identifier { get; private set; }

		public Location Location { get; set; }

		public Completion(string type, JsValue? value, string identifier)
		{
			Type = type;
			Value = value;
			Identifier = identifier;
		}

		public JsValue GetValueOrDefault()
		{
			if (!Value.HasValue)
			{
				return Undefined.Instance;
			}
			return Value.Value;
		}
	}
}
