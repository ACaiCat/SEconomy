using System.Linq;
using Jint.Native.Object;
using Jint.Parser;
using Jint.Parser.Ast;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Environments;

namespace Jint.Native.Function
{
	public sealed class ScriptFunctionInstance : FunctionInstance, IConstructor
	{
		private readonly IFunctionDeclaration _functionDeclaration;

		public ObjectInstance PrototypeObject { get; private set; }

		public ScriptFunctionInstance(Engine engine, IFunctionDeclaration functionDeclaration, LexicalEnvironment scope, bool strict)
			: base(engine, functionDeclaration.Parameters.Select((Identifier x) => x.Name).ToArray(), scope, strict)
		{
			_functionDeclaration = functionDeclaration;
			base.Engine = engine;
			base.Extensible = true;
			base.Prototype = engine.Function.PrototypeObject;
			DefineOwnProperty("length", new PropertyDescriptor(new JsValue(base.FormalParameters.Length), false, false, false), throwOnError: false);
			ObjectInstance objectInstance = engine.Object.Construct(Arguments.Empty);
			objectInstance.DefineOwnProperty("constructor", new PropertyDescriptor(this, true, false, true), throwOnError: false);
			DefineOwnProperty("prototype", new PropertyDescriptor(objectInstance, true, false, false), throwOnError: false);
			if (_functionDeclaration.Id != null)
			{
				DefineOwnProperty("name", new PropertyDescriptor(_functionDeclaration.Id.Name, null, null, null), throwOnError: false);
			}
			if (strict)
			{
				FunctionInstance throwTypeError = engine.Function.ThrowTypeError;
				DefineOwnProperty("caller", new PropertyDescriptor(throwTypeError, throwTypeError, false, false), throwOnError: false);
				DefineOwnProperty("arguments", new PropertyDescriptor(throwTypeError, throwTypeError, false, false), throwOnError: false);
			}
		}

		public override JsValue Call(JsValue thisArg, JsValue[] arguments)
		{
			using (new StrictModeScope(base.Strict, force: true))
			{
				JsValue thisBinding = (StrictModeScope.IsStrictModeCode ? thisArg : ((thisArg == Undefined.Instance || thisArg == Null.Instance) ? ((JsValue)base.Engine.Global) : (thisArg.IsObject() ? thisArg : ((JsValue)TypeConverter.ToObject(base.Engine, thisArg)))));
				LexicalEnvironment lexicalEnvironment = LexicalEnvironment.NewDeclarativeEnvironment(base.Engine, base.Scope);
				base.Engine.EnterExecutionContext(lexicalEnvironment, lexicalEnvironment, thisBinding);
				try
				{
					base.Engine.DeclarationBindingInstantiation(DeclarationBindingType.FunctionCode, _functionDeclaration.FunctionDeclarations, _functionDeclaration.VariableDeclarations, this, arguments);
					Completion completion = base.Engine.ExecuteStatement(_functionDeclaration.Body);
					if (completion.Type == Completion.Throw)
					{
						JavaScriptException ex = new JavaScriptException(completion.GetValueOrDefault());
						ex.Location = completion.Location;
						throw ex;
					}
					if (completion.Type == Completion.Return)
					{
						return completion.GetValueOrDefault();
					}
				}
				finally
				{
					base.Engine.LeaveExecutionContext();
				}
				return Undefined.Instance;
			}
		}

		public ObjectInstance Construct(JsValue[] arguments)
		{
			ObjectInstance objectInstance = Get("prototype").TryCast<ObjectInstance>();
			ObjectInstance objectInstance2 = new ObjectInstance(base.Engine);
			objectInstance2.Extensible = true;
			objectInstance2.Prototype = objectInstance ?? base.Engine.Object.PrototypeObject;
			ObjectInstance objectInstance3 = Call(objectInstance2, arguments).TryCast<ObjectInstance>();
			if (objectInstance3 != null)
			{
				return objectInstance3;
			}
			return objectInstance2;
		}
	}
}
