using Jint.Parser;
using Jint.Parser.Ast;
using Jint.Runtime;
using Jint.Runtime.Environments;

namespace Jint.Native.Function
{
	public class EvalFunctionInstance : FunctionInstance
	{
		private readonly Engine _engine;

		public EvalFunctionInstance(Engine engine, string[] parameters, LexicalEnvironment scope, bool strict)
			: base(engine, parameters, scope, strict)
		{
			_engine = engine;
			base.Prototype = base.Engine.Function.PrototypeObject;
			FastAddProperty("length", 1.0, writable: false, enumerable: false, configurable: false);
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			return Call(thisObject, arguments, directCall: false);
		}

		public JsValue Call(JsValue thisObject, JsValue[] arguments, bool directCall)
		{
			if (arguments.At(0).Type != Types.String)
			{
				return arguments.At(0);
			}
			string code = TypeConverter.ToString(arguments.At(0));
			try
			{
				JavaScriptParser javaScriptParser = new JavaScriptParser(StrictModeScope.IsStrictModeCode);
				Program program = javaScriptParser.Parse(code);
				using (new StrictModeScope(program.Strict))
				{
					using (new EvalCodeScope())
					{
						LexicalEnvironment lexicalEnvironment = null;
						try
						{
							if (!directCall)
							{
								base.Engine.EnterExecutionContext(base.Engine.GlobalEnvironment, base.Engine.GlobalEnvironment, base.Engine.Global);
							}
							if (StrictModeScope.IsStrictModeCode)
							{
								lexicalEnvironment = LexicalEnvironment.NewDeclarativeEnvironment(base.Engine, base.Engine.ExecutionContext.LexicalEnvironment);
								base.Engine.EnterExecutionContext(lexicalEnvironment, lexicalEnvironment, base.Engine.ExecutionContext.ThisBinding);
							}
							base.Engine.DeclarationBindingInstantiation(DeclarationBindingType.EvalCode, program.FunctionDeclarations, program.VariableDeclarations, this, arguments);
							Completion completion = _engine.ExecuteStatement(program);
							if (completion.Type == Completion.Throw)
							{
								throw new JavaScriptException(completion.GetValueOrDefault());
							}
							return completion.GetValueOrDefault();
						}
						finally
						{
							if (lexicalEnvironment != null)
							{
								base.Engine.LeaveExecutionContext();
							}
							if (!directCall)
							{
								base.Engine.LeaveExecutionContext();
							}
						}
					}
				}
			}
			catch (ParserException)
			{
				throw new JavaScriptException(base.Engine.SyntaxError);
			}
		}
	}
}
