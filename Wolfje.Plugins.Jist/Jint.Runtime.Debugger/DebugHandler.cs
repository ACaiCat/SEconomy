using System.Collections.Generic;
using System.Linq;
using Jint.Native;
using Jint.Parser.Ast;
using Jint.Runtime.Environments;

namespace Jint.Runtime.Debugger
{
	internal class DebugHandler
	{
		private readonly Stack<string> _debugCallStack;

		private StepMode _stepMode;

		private int _callBackStepOverDepth;

		private readonly Engine _engine;

		public DebugHandler(Engine engine)
		{
			_engine = engine;
			_debugCallStack = new Stack<string>();
			_stepMode = StepMode.Into;
		}

		internal void PopDebugCallStack()
		{
			if (_debugCallStack.Count > 0)
			{
				_debugCallStack.Pop();
			}
			if (_stepMode == StepMode.Out && _debugCallStack.Count < _callBackStepOverDepth)
			{
				_callBackStepOverDepth = _debugCallStack.Count;
				_stepMode = StepMode.Into;
			}
			else if (_stepMode == StepMode.Over && _debugCallStack.Count == _callBackStepOverDepth)
			{
				_callBackStepOverDepth = _debugCallStack.Count;
				_stepMode = StepMode.Into;
			}
		}

		internal void AddToDebugCallStack(CallExpression callExpression)
		{
			if (!(callExpression.Callee is Identifier identifier))
			{
				return;
			}
			string text = identifier.Name + "(";
			List<string> list = new List<string>();
			foreach (Expression argument in callExpression.Arguments)
			{
				if (argument != null)
				{
					Identifier identifier2 = argument as Identifier;
					list.Add((identifier2 != null) ? identifier2.Name : "null");
				}
				else
				{
					list.Add("null");
				}
			}
			text += string.Join(", ", list);
			text += ")";
			_debugCallStack.Push(text);
		}

		internal void OnStep(Statement statement)
		{
			StepMode stepMode = _stepMode;
			if (statement == null)
			{
				return;
			}
			BreakPoint breakPoint2 = _engine.BreakPoints.FirstOrDefault((BreakPoint breakPoint) => BpTest(statement, breakPoint));
			bool flag = false;
			if (breakPoint2 != null)
			{
				DebugInformation info = CreateDebugInformation(statement);
				StepMode? stepMode2 = _engine.InvokeBreakEvent(info);
				if (stepMode2.HasValue)
				{
					_stepMode = stepMode2.Value;
					flag = true;
				}
			}
			if (!flag && _stepMode == StepMode.Into)
			{
				DebugInformation info2 = CreateDebugInformation(statement);
				StepMode? stepMode3 = _engine.InvokeStepEvent(info2);
				if (stepMode3.HasValue)
				{
					_stepMode = stepMode3.Value;
				}
			}
			if (stepMode == StepMode.Into && _stepMode == StepMode.Out)
			{
				_callBackStepOverDepth = _debugCallStack.Count;
			}
			else if (stepMode == StepMode.Into && _stepMode == StepMode.Over)
			{
				if (statement is ExpressionStatement expressionStatement && expressionStatement.Expression is CallExpression)
				{
					_callBackStepOverDepth = _debugCallStack.Count;
				}
				else
				{
					_stepMode = StepMode.Into;
				}
			}
		}

		private bool BpTest(Statement statement, BreakPoint breakpoint)
		{
			if (breakpoint.Line != statement.Location.Start.Line || breakpoint.Char < statement.Location.Start.Column)
			{
				return false;
			}
			if (breakpoint.Line >= statement.Location.End.Line && (breakpoint.Line != statement.Location.End.Line || breakpoint.Char > statement.Location.End.Column))
			{
				return false;
			}
			if (!string.IsNullOrEmpty(breakpoint.Condition))
			{
				return _engine.Execute(breakpoint.Condition).GetCompletionValue().AsBoolean();
			}
			return true;
		}

		private DebugInformation CreateDebugInformation(Statement statement)
		{
			DebugInformation debugInformation = new DebugInformation
			{
				CurrentStatement = statement,
				CallStack = _debugCallStack
			};
			if (_engine.ExecutionContext != null && _engine.ExecutionContext.LexicalEnvironment != null)
			{
				LexicalEnvironment lexicalEnvironment = _engine.ExecutionContext.LexicalEnvironment;
				debugInformation.Locals = GetLocalVariables(lexicalEnvironment);
				debugInformation.Globals = GetGlobalVariables(lexicalEnvironment);
			}
			return debugInformation;
		}

		private static Dictionary<string, JsValue> GetLocalVariables(LexicalEnvironment lex)
		{
			Dictionary<string, JsValue> dictionary = new Dictionary<string, JsValue>();
			if (lex != null && lex.Record != null)
			{
				AddRecordsFromEnvironment(lex, dictionary);
			}
			return dictionary;
		}

		private static Dictionary<string, JsValue> GetGlobalVariables(LexicalEnvironment lex)
		{
			Dictionary<string, JsValue> dictionary = new Dictionary<string, JsValue>();
			LexicalEnvironment lexicalEnvironment = lex;
			while (lexicalEnvironment != null && lexicalEnvironment.Record != null)
			{
				AddRecordsFromEnvironment(lexicalEnvironment, dictionary);
				lexicalEnvironment = lexicalEnvironment.Outer;
			}
			return dictionary;
		}

		private static void AddRecordsFromEnvironment(LexicalEnvironment lex, Dictionary<string, JsValue> locals)
		{
			string[] allBindingNames = lex.Record.GetAllBindingNames();
			string[] array = allBindingNames;
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (!locals.ContainsKey(text))
				{
					JsValue bindingValue = lex.Record.GetBindingValue(text, strict: false);
					if (bindingValue.TryCast<ICallable>() == null)
					{
						locals.Add(text, bindingValue);
					}
				}
			}
		}
	}
}
