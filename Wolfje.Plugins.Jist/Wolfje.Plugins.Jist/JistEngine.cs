using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Parser;
using Jint.Runtime;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Wolfje.Plugins.Jist.Framework;
using Wolfje.Plugins.Jist.stdlib;

namespace Wolfje.Plugins.Jist
{
	public class JistEngine : IDisposable
	{
		protected JistPlugin plugin;

		protected Engine jsEngine;

		protected List<string> providedPackages;

		protected ScriptContainer scriptContainer;

		protected int totalLoadingItems;

		protected int doneItems;

		protected int oldPercent;

		protected static string scriptsDir = Path.Combine(Environment.CurrentDirectory, "serverscripts");

		protected readonly object syncRoot = new object();

		public std stdLib;

		public tshock stdTshock;

		public stdtask stdTask;

		public stdhook stdHook;

		public JistPlugin PluginInstance => plugin;

		internal event EventHandler<PercentChangedEventArgs> PercentChanged;

		public JistEngine(JistPlugin parent)
		{
			providedPackages = new List<string>();
			plugin = parent;
			scriptContainer = new ScriptContainer(this);
			ServerApi.Hooks.GamePostInitialize.Register(plugin, Game_PostInitialize,int.MinValue);
			PercentChanged += delegate(object sender, PercentChangedEventArgs args)
			{
				ConsoleEx.WriteBar(args);
			};
		}

		protected async void Game_PostInitialize(EventArgs args)
		{
			await LoadEngineAsync();
		}

		protected void RaisePercentChangedEvent(string label)
		{
			PercentChangedEventArgs percentChangedEventArgs = new PercentChangedEventArgs();
			double num = (double)(++doneItems) / (double)totalLoadingItems * 100.0;
			if (oldPercent != (int)num)
			{
				percentChangedEventArgs.Percent = (int)num;
				percentChangedEventArgs.Label = label;
				if (this.PercentChanged != null)
				{
					this.PercentChanged(this, percentChangedEventArgs);
				}
				oldPercent = (int)num;
			}
		}

		public async Task LoadEngineAsync()
		{
			Thread.Sleep(2000);
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(" * Jist正在加载...");
			Console.ResetColor();
			if (!Directory.Exists(scriptsDir))
			{
				try
				{
					Directory.CreateDirectory(scriptsDir);
				}
				catch
				{
					TShock.Log.ConsoleError("jist加载: 无法创建serverscript目录");
					return;
				}
			}
			totalLoadingItems = ScriptsCount() * 2 + 5;
			jsEngine = new Engine(delegate(Options o)
			{
				o.AllowClr(typeof(Main).Assembly, typeof(TShock).Assembly);
			});
			RaisePercentChangedEvent("Jist引擎");
			await Task.Run(delegate
			{
				LoadLibraries();
			});
			RaisePercentChangedEvent("库");
			await Task.Run(delegate
			{
				CreateScriptFunctions();
			});
			RaisePercentChangedEvent("方法(函数)");
			ExecuteHardCodedScripts();
			await Task.Run(delegate
			{
				LoadScripts();
			});
			RaisePercentChangedEvent("脚本");
			await Task.Run(delegate
			{
				ExecuteScripts();
			});
			RaisePercentChangedEvent("执行");
			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(" * 已加载{0}个脚本", ScriptsCount());
			Console.ResetColor();
			Console.WriteLine();
		}

		protected void ExecuteHardCodedScripts()
		{
			lock (syncRoot)
			{
				jsEngine.Execute("dump = function(o) {\r\n    var s = '';\r\n\r\n    if (typeof(o) == 'undefined') return 'undefined';\r\n\r\n    if (typeof o.valueOf == 'undefined') return \"'valueOf()' is missing on '\" + (typeof o) + \"' - if you are inheriting from V8ManagedObject, make sure you are not blocking the property.\";\r\n\r\n    if (typeof o.toString == 'undefined') return \"'toString()' is missing on '\" + o.valueOf() + \"' - if you are inheriting from V8ManagedObject, make sure you are not blocking the property.\";\r\n\r\n    for (var p in o) {\r\n        var ov = '',\r\n            pv = '';\r\n\r\n        try {\r\n            ov = o.valueOf();\r\n        } catch (e) {\r\n            ov = '{error: ' + e.message + ': ' + dump(o) + '}';\r\n        }\r\n\r\n        try {\r\n            pv = o[p];\r\n        } catch (e) {\r\n            pv = e.message;\r\n        }\r\n\r\n        s += '* ' + ov + '.' + p + ' = (' + pv + ')\\r\\n';\r\n    }\r\n\r\n    return s;\r\n}");
			}
		}

		protected void LoadLibraries()
		{
			LoadLibrary(stdLib = new std(this));
			LoadLibrary(stdTshock = new tshock(this));
			LoadLibrary(stdTask = new stdtask(this));
			LoadLibrary(stdHook = new stdhook(this));
		}

		public void LoadLibrary(stdlib_base lib)
		{
			if (lib != null)
			{
				CreateScriptFunctions(lib.GetType(), lib);
			}
		}

		protected int ScriptsCount()
		{
			try
			{
				return Directory.EnumerateFiles(scriptsDir, "*.js").Count();
			}
			catch
			{
				return 0;
			}
		}

		protected void LoadScripts()
		{
			foreach (string item in Directory.EnumerateFiles(scriptsDir, "*.js"))
			{
				LoadScript(Path.GetFileName(item));
				RaisePercentChangedEvent("Scripts");
			}
		}

		public JistScript LoadScript(string ScriptPath, bool IncreaseRefCount = true)
		{
			JistScript jistScript;
			if (scriptContainer.Scripts.Count((JistScript i) => i.FilePathOrUri.Equals(ScriptPath, StringComparison.InvariantCultureIgnoreCase)) > 0)
			{
				jistScript = scriptContainer.Scripts.FirstOrDefault((JistScript i) => i.FilePathOrUri.Equals(ScriptPath, StringComparison.InvariantCultureIgnoreCase));
				if (IncreaseRefCount)
				{
					jistScript.ReferenceCount++;
				}
				return null;
			}
			jistScript = new JistScript();
			jistScript.FilePathOrUri = ScriptPath;
			jistScript.ReferenceCount = 1;
			try
			{
				jistScript.Script = File.ReadAllText(Path.Combine(scriptsDir, jistScript.FilePathOrUri));
			}
			catch (Exception ex)
			{
				ScriptLog.ErrorFormat("jist引擎", "无法加载脚本 {0}: {1}", ScriptPath, ex.Message);
				return null;
			}
			scriptContainer.PreprocessScript(jistScript);
			scriptContainer.Scripts.Add(jistScript);
			return jistScript;
		}

		public string Eval(string snippet)
		{
			JsValue jsValue = default(JsValue);
			if (jsEngine == null || string.IsNullOrEmpty(snippet))
			{
				return "undefined";
			}
			try
			{
				lock (syncRoot)
				{
					jsValue = jsEngine.GetValue(jsEngine.Execute(snippet).GetCompletionValue());
				}
				if (jsValue.Type == Types.None || jsValue.Type == Types.Null || jsValue.Type == Types.Undefined)
				{
					return "undefined";
				}
			}
			catch (JavaScriptException ex)
			{
				StringBuilder stringBuilder = new StringBuilder("JavaScript错误: " + ex.Message + "\r\n");
				stringBuilder.AppendLine(ex.StackTrace);
				return stringBuilder.ToString();
			}
			catch (ParserException ex2)
			{
				StringBuilder stringBuilder2 = new StringBuilder("JavaScript解析器错误: " + ex2.Message + "\r\n");
				stringBuilder2.AppendLine($" 在行 {ex2.LineNumber} 字符 {ex2.Column}");
				stringBuilder2.AppendLine(ex2.Source);
				stringBuilder2.AppendLine(ex2.StackTrace);
				return stringBuilder2.ToString();
			}
			catch (Exception ex3)
			{
				return ex3.ToString();
			}
			if (string.IsNullOrEmpty(jsValue.ToString()))
			{
				TShock.Log.ConsoleError("[JIST评估]\"{0}\"的结果为空", snippet);
				return "undefined";
			}
			return jsValue.ToString();
		}

		public JsValue ExecuteScript(JistScript script)
		{
			if (script == null || string.IsNullOrEmpty(script.Script))
			{
				return JsValue.Undefined;
			}
			try
			{
				lock (syncRoot)
				{
					return jsEngine.Execute(script.Script).GetCompletionValue();
				}
			}
			catch (Exception ex)
			{
				ScriptLog.ErrorFormat(script.FilePathOrUri, "执行错误: " + ex.Message);
				return JsValue.Undefined;
			}
		}

		protected void ExecuteScripts()
		{
			foreach (JistScript item in scriptContainer.Scripts.OrderByDescending((JistScript i) => i.ReferenceCount))
			{
				try
				{
					ExecuteScript(item);
					RaisePercentChangedEvent("Execute");
				}
				catch (Exception ex)
				{
					ScriptLog.ErrorFormat(item.FilePathOrUri, "执行错误: " + ex.Message);
				}
			}
		}

		public async Task CreateScriptFunctionsAsync(Type type, object instance)
		{
			await Task.Run(delegate
			{
				CreateScriptFunctions(type, instance);
			});
		}

		public void CreateScriptFunctions(Type type, object instance)
		{
			Delegate @delegate = null;
			Type type2 = null;
			string text = null;
			JavascriptFunctionAttribute javascriptFunctionAttribute = null;
			foreach (JavascriptProvidesAttribute item in type.GetCustomAttributes(inherit: true).OfType<JavascriptProvidesAttribute>())
			{
				if (!providedPackages.Contains(item.PackageName))
				{
					providedPackages.Add(item.PackageName);
				}
			}
			foreach (MethodInfo item2 in from i in type.GetMethods()
				where i.GetCustomAttributes(inherit: true).OfType<JavascriptFunctionAttribute>().Any()
				select i)
			{
				if ((instance == null && !item2.IsStatic) || (javascriptFunctionAttribute = item2.GetCustomAttributes(inherit: true).OfType<JavascriptFunctionAttribute>().FirstOrDefault()) == null)
				{
					continue;
				}
				string[] functionNames = javascriptFunctionAttribute.FunctionNames;
				string[] array = functionNames;
				foreach (string text2 in array)
				{
					text = text2 ?? item2.Name;
					try
					{
						type2 = Expression.GetDelegateType((from i in item2.GetParameters()
							select i.ParameterType).Concat(new Type[1] { item2.ReturnType }).ToArray());
						@delegate = ((instance == null) ? Delegate.CreateDelegate(type2, item2) : Delegate.CreateDelegate(type2, instance, item2));
						lock (syncRoot)
						{
							jsEngine.SetValue(text, @delegate);
						}
					}
					catch (Exception ex)
					{
						ScriptLog.ErrorFormat("jist引擎", "为{0}创建JavaScript函数时出错: {1}", text, ex.ToString());
					}
				}
			}
		}

		protected void CreateScriptFunctions()
		{
			JistPlugin.RequestExternalFunctions();
			lock (syncRoot)
			{
				jsEngine.SetValue("alert", new Action<object>(Console.WriteLine));
			}
		}

		public JsValue CallFunction(JsValue function, object thisObject, params object[] args)
		{
			object value = thisObject ?? this;
			try
			{
				lock (syncRoot)
				{
					return function.Invoke(JsValue.FromObject(jsEngine, value), args.ToJsValueArray(jsEngine));
				}
			}
			catch (JavaScriptException ex)
			{
				StringBuilder stringBuilder = new StringBuilder("JavaScript错误: " + ex.Message + "\r\n");
				stringBuilder.AppendLine($" 在行 {ex.LineNumber} 字符 {ex.Column}");
				stringBuilder.AppendLine(ex.Location.Source);
				stringBuilder.AppendLine(ex.StackTrace);
				TShock.Log.ConsoleError(stringBuilder.ToString());
			}
			catch (ParserException ex2)
			{
				StringBuilder stringBuilder2 = new StringBuilder("JavaScript解析器错误: " + ex2.Message + "\r\n");
				stringBuilder2.AppendLine($" 在行 {ex2.LineNumber} 字符 {ex2.Column}");
				stringBuilder2.AppendLine(ex2.Source);
				stringBuilder2.AppendLine(ex2.StackTrace);
				TShock.Log.ConsoleError(stringBuilder2.ToString());
			}
			catch (Exception ex3)
			{
				TShock.Log.ConsoleError(ex3.ToString());
			}
			return JsValue.Undefined;
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				stdLib.Dispose();
				stdTshock.Dispose();
				stdTask.Dispose();
				stdHook.Dispose();
				ServerApi.Hooks.GamePostInitialize.Deregister(plugin, Game_PostInitialize);
			}
		}
	}
}
