using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Wolfje.Plugins.Jist.Framework
{
	public class ScriptContainer
	{
		protected JistEngine jistParent;

		protected readonly MatchEvaluator blankEvaluator = (Match str) => "";

		public List<JistScript> Scripts { get; set; }

		public ScriptContainer(JistEngine parent)
		{
			jistParent = parent;
			Scripts = new List<JistScript>();
		}

		public void PreprocessScript(JistScript script)
		{
			if (script != null && !string.IsNullOrEmpty(script.Script))
			{
				PreprocessComments(ref script);
				PreprocessRequires(ref script);
				PreprocessImports(ref script);
				PreprocessInlines(ref script);
			}
		}

		protected void PreprocessComments(ref JistScript script)
		{
			if (script != null && !string.IsNullOrEmpty(script.Script))
			{
				PreprocessorDirectives.multilineCommentRegex.Replace(script.Script, blankEvaluator);
				PreprocessorDirectives.singleLineCommentRegex.Replace(script.Script, blankEvaluator);
			}
		}

		protected void PreprocessRequires(ref JistScript script)
		{
			if (script == null || string.IsNullOrEmpty(script.Script) || !PreprocessorDirectives.requiresRegex.IsMatch(script.Script))
			{
				return;
			}
			foreach (Match item in PreprocessorDirectives.requiresRegex.Matches(script.Script))
			{
				string[] array = item.Groups[2].Value.Split(',');
				string[] array2 = array;
				string[] array3 = array2;
				foreach (string text in array3)
				{
					string text2 = text.Trim().Replace("\"", "");
					if (string.IsNullOrEmpty(text2))
					{
						return;
					}
					script.PackageRequirements.Add(text2);
					script.Script = script.Script.Replace(item.Value, "/** #pragma require \"" + text2 + "\" - DO NOT CHANGE THIS LINE **/\r\n");
				}
			}
		}

		protected void PreprocessImports(ref JistScript script)
		{
			if (!PreprocessorDirectives.importRegex.IsMatch(script.Script))
			{
				return;
			}
			foreach (Match item in PreprocessorDirectives.importRegex.Matches(script.Script))
			{
				string value = item.Groups[1].Value;
				if (!value.Equals(script.FilePathOrUri))
				{
					jistParent.LoadScript(value);
					string newValue = "/** #pragma import \"" + value + "\" - Imported by engine - DO NOT CHANGE THIS LINE **/";
					script.Script = script.Script.Replace(item.Value, newValue);
				}
			}
		}

		protected void PreprocessInlines(ref JistScript script)
		{
			if (!PreprocessorDirectives.inlineRegex.IsMatch(script.Script))
			{
				return;
			}
			foreach (Match item in PreprocessorDirectives.inlineRegex.Matches(script.Script))
			{
				string value = item.Groups[1].Value;
				if (!value.Equals(script.FilePathOrUri))
				{
					JistScript jistScript = jistParent.LoadScript(value, IncreaseRefCount: false);
					script.Script = script.Script.Replace(item.Value, "");
					script.Script = script.Script.Insert(item.Index, "/** #pragma inline \"" + value + "\" **/\r\n" + jistScript.Script);
				}
			}
		}
	}
}
