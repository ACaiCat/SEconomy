namespace Jint.Parser
{
	public class Token
	{
		public static Token Empty = new Token();

		public Tokens Type;

		public string Literal;

		public object Value;

		public int[] Range;

		public int? LineNumber;

		public int LineStart;

		public bool Octal;

		public Location Location;

		public int Precedence;
	}
}
