using System;
using System.Collections.Generic;
using System.Linq;
using Jint.Native.Object;
using Jint.Native.String;
using Jint.Parser;
using Jint.Parser.Ast;
using Jint.Runtime;
using Jint.Runtime.Environments;

namespace Jint.Native.Function
{
	public sealed class FunctionConstructor : FunctionInstance, IConstructor
	{
		private FunctionInstance _throwTypeError;

		public FunctionPrototype PrototypeObject { get; private set; }

		public FunctionInstance ThrowTypeError
		{
			get
			{
				if (_throwTypeError != null)
				{
					return _throwTypeError;
				}
				_throwTypeError = new ThrowTypeError(base.Engine);
				return _throwTypeError;
			}
		}

		private FunctionConstructor(Engine engine)
			: base(engine, null, null, strict: false)
		{
		}

		public static FunctionConstructor CreateFunctionConstructor(Engine engine)
		{
			FunctionConstructor functionConstructor = new FunctionConstructor(engine);
			functionConstructor.Extensible = true;
			functionConstructor.PrototypeObject = FunctionPrototype.CreatePrototypeObject(engine);
			functionConstructor.Prototype = functionConstructor.PrototypeObject;
			functionConstructor.FastAddProperty("prototype", functionConstructor.PrototypeObject, writable: false, enumerable: false, configurable: false);
			functionConstructor.FastAddProperty("length", 1.0, writable: false, enumerable: false, configurable: false);
			return functionConstructor;
		}

		public void Configure()
		{
		}

		public override JsValue Call(JsValue thisObject, JsValue[] arguments)
		{
			return Construct(arguments);
		}

		private string[] ParseArgumentNames(string parameterDeclaration)
		{
			string[] array = parameterDeclaration.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = new string[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = StringPrototype.TrimEx(array[i]);
			}
			return array2;
		}

		public ObjectInstance Construct(JsValue[] arguments)
		{
			int num = arguments.Length;
			string text = "";
			string text2 = "";
			if (num == 1)
			{
				text2 = TypeConverter.ToString(arguments[0]);
			}
			else if (num > 1)
			{
				JsValue jsValue = arguments[0];
				text = TypeConverter.ToString(jsValue);
				for (int i = 1; i < num - 1; i++)
				{
					JsValue jsValue2 = arguments[i];
					text = text + "," + TypeConverter.ToString(jsValue2);
				}
				text2 = TypeConverter.ToString(arguments[num - 1]);
			}
			string[] source = ParseArgumentNames(text);
			JavaScriptParser javaScriptParser = new JavaScriptParser();
			FunctionExpression functionExpression2;
			try
			{
				string functionExpression = "function(" + text + ") { " + text2 + "}";
				functionExpression2 = javaScriptParser.ParseFunctionExpression(functionExpression);
			}
			catch (ParserException)
			{
				throw new JavaScriptException(base.Engine.SyntaxError);
			}
			return new ScriptFunctionInstance(base.Engine, new FunctionDeclaration
			{
				Type = SyntaxNodes.FunctionDeclaration,
				Body = new BlockStatement
				{
					Type = SyntaxNodes.BlockStatement,
					Body = new Statement[1] { functionExpression2.Body }
				},
				Parameters = source.Select((string x) => new Identifier
				{
					Type = SyntaxNodes.Identifier,
					Name = x
				}).ToArray(),
				FunctionDeclarations = functionExpression2.FunctionDeclarations,
				VariableDeclarations = functionExpression2.VariableDeclarations
			}, LexicalEnvironment.NewDeclarativeEnvironment(base.Engine, base.Engine.ExecutionContext.LexicalEnvironment), functionExpression2.Strict)
			{
				Extensible = true
			};
		}

		public FunctionInstance CreateFunctionObject(FunctionDeclaration functionDeclaration)
		{
			return new ScriptFunctionInstance(base.Engine, functionDeclaration, LexicalEnvironment.NewDeclarativeEnvironment(base.Engine, base.Engine.ExecutionContext.LexicalEnvironment), functionDeclaration.Strict)
			{
				Extensible = true
			};
		}

		public object Apply(JsValue thisObject, JsValue[] arguments)
		{
			if (arguments.Length != 2)
			{
				throw new ArgumentException("Apply has to be called with two arguments.");
			}
			ICallable callable = thisObject.TryCast<ICallable>();
			JsValue thisObject2 = arguments[0];
			JsValue jsValue = arguments[1];
			if (callable == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			if (jsValue == Null.Instance || jsValue == Undefined.Instance)
			{
				return callable.Call(thisObject2, Arguments.Empty);
			}
			ObjectInstance objectInstance = jsValue.TryCast<ObjectInstance>();
			if (objectInstance == null)
			{
				throw new JavaScriptException(base.Engine.TypeError);
			}
			JsValue o = objectInstance.Get("length");
			uint num = TypeConverter.ToUint32(o);
			List<JsValue> list = new List<JsValue>();
			for (int i = 0; i < num; i++)
			{
				string propertyName = i.ToString();
				JsValue item = objectInstance.Get(propertyName);
				list.Add(item);
			}
			return callable.Call(thisObject2, list.ToArray());
		}
	}
}
