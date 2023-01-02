using System;

namespace Wolfje.Plugins.Jist.stdlib
{
	public class stdlib_base : IDisposable, IStdLib
	{
		protected JistEngine engine;

		public string Provides { get; protected set; }

		public stdlib_base(JistEngine engine)
		{
			this.engine = engine;
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
