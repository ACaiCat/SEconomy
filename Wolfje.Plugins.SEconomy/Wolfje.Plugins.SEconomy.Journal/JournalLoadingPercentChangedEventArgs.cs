using System;

namespace Wolfje.Plugins.SEconomy.Journal
{
	public class JournalLoadingPercentChangedEventArgs : EventArgs
	{
		public string Label { get; set; }

		public int JournalLength { get; set; }

		public int Percent { get; set; }
	}
}
