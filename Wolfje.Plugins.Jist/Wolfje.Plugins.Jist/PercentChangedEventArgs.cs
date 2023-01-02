using System;

namespace Wolfje.Plugins.Jist
{
	internal class PercentChangedEventArgs : EventArgs
	{
		public string Label { get; set; }

		public decimal Percent { get; set; }
	}
}
