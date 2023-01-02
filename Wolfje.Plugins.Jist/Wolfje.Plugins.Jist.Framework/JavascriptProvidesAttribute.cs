using System;

namespace Wolfje.Plugins.Jist.Framework
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class JavascriptProvidesAttribute : Attribute
	{
		public string PackageName { get; set; }

		public JavascriptProvidesAttribute(string PackageName)
		{
			this.PackageName = PackageName;
		}
	}
}
