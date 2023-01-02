using System.Collections.Generic;

namespace Wolfje.Plugins.Jist.Framework
{
	public class JistScript
	{
		public string FilePathOrUri { get; set; }

		public int ReferenceCount { get; set; }

		public string Script { get; set; }

		public List<string> PackageRequirements { get; set; }

		public JistScript()
		{
			PackageRequirements = new List<string>();
		}
	}
}
