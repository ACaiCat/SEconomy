namespace Wolfje.Plugins.Jist.Framework
{
	public class JavascriptFunctionsNeededEventArgs
	{
		public JistEngine Engine { get; protected set; }

		public JavascriptFunctionsNeededEventArgs(JistEngine engine)
		{
			Engine = engine;
		}
	}
}
