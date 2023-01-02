using System;
using System.Collections.Generic;
using System.Linq;
using Jint.Native;
using Jint.Native.Argument;
using Jint.Native.Array;
using Jint.Native.Boolean;
using Jint.Native.Date;
using Jint.Native.Error;
using Jint.Native.Function;
using Jint.Native.Global;
using Jint.Native.Json;
using Jint.Native.Math;
using Jint.Native.Number;
using Jint.Native.Object;
using Jint.Native.RegExp;
using Jint.Native.String;
using Jint.Parser;
using Jint.Parser.Ast;
using Jint.Runtime;
using Jint.Runtime.CallStack;
using Jint.Runtime.Debugger;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Environments;
using Jint.Runtime.Interop;
using Jint.Runtime.References;

namespace Jint
{
	public class Engine
	{
		public delegate StepMode DebugStepDelegate(object sender, DebugInformation e);

		public delegate StepMode BreakDelegate(object sender, DebugInformation e);

		private readonly ExpressionInterpreter _expressions;

		private readonly StatementInterpreter _statements;

		private readonly Stack<Runtime.Environments.ExecutionContext> _executionContexts;

		private JsValue _completionValue = JsValue.Undefined;

		private int _statementsCount;

		private long _timeoutTicks;

		private SyntaxNode _lastSyntaxNode;

		public ITypeConverter ClrTypeConverter;

		internal Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();

		internal JintCallStack CallStack = new JintCallStack();

		public LexicalEnvironment GlobalEnvironment;

		public GlobalObject Global { get; private set; }

		public ObjectConstructor Object { get; private set; }

		public FunctionConstructor Function { get; private set; }

		public ArrayConstructor Array { get; private set; }

		public StringConstructor String { get; private set; }

		public RegExpConstructor RegExp { get; private set; }

		public BooleanConstructor Boolean { get; private set; }

		public NumberConstructor Number { get; private set; }

		public DateConstructor Date { get; private set; }

		public MathInstance Math { get; private set; }

		public JsonInstance Json { get; private set; }

		public EvalFunctionInstance Eval { get; private set; }

		public ErrorConstructor Error { get; private set; }

		public ErrorConstructor EvalError { get; private set; }

		public ErrorConstructor SyntaxError { get; private set; }

		public ErrorConstructor TypeError { get; private set; }

		public ErrorConstructor RangeError { get; private set; }

		public ErrorConstructor ReferenceError { get; private set; }

		public ErrorConstructor UriError { get; private set; }

		public Runtime.Environments.ExecutionContext ExecutionContext => _executionContexts.Peek();

		internal Options Options { get; private set; }

		internal DebugHandler DebugHandler { get; private set; }

		public List<BreakPoint> BreakPoints { get; private set; }

		public event DebugStepDelegate Step;

		public event BreakDelegate Break;

		public Engine()
			: this(null)
		{
		}

		public Engine(Action<Options> options)
		{
			_executionContexts = new();
			Global = GlobalObject.CreateGlobalObject(this);
			Object = ObjectConstructor.CreateObjectConstructor(this);
			Function = FunctionConstructor.CreateFunctionConstructor(this);
			Array = ArrayConstructor.CreateArrayConstructor(this);
			String = StringConstructor.CreateStringConstructor(this);
			RegExp = RegExpConstructor.CreateRegExpConstructor(this);
			Number = NumberConstructor.CreateNumberConstructor(this);
			Boolean = BooleanConstructor.CreateBooleanConstructor(this);
			Date = DateConstructor.CreateDateConstructor(this);
			Math = MathInstance.CreateMathObject(this);
			Json = JsonInstance.CreateJsonObject(this);
			Error = ErrorConstructor.CreateErrorConstructor(this, "Error");
			EvalError = ErrorConstructor.CreateErrorConstructor(this, "EvalError");
			RangeError = ErrorConstructor.CreateErrorConstructor(this, "RangeError");
			ReferenceError = ErrorConstructor.CreateErrorConstructor(this, "ReferenceError");
			SyntaxError = ErrorConstructor.CreateErrorConstructor(this, "SyntaxError");
			TypeError = ErrorConstructor.CreateErrorConstructor(this, "TypeError");
			UriError = ErrorConstructor.CreateErrorConstructor(this, "URIError");
			Global.Configure();
			Object.Configure();
			Object.PrototypeObject.Configure();
			Function.Configure();
			Function.PrototypeObject.Configure();
			Array.Configure();
			Array.PrototypeObject.Configure();
			String.Configure();
			String.PrototypeObject.Configure();
			RegExp.Configure();
			RegExp.PrototypeObject.Configure();
			Number.Configure();
			Number.PrototypeObject.Configure();
			Boolean.Configure();
			Boolean.PrototypeObject.Configure();
			Date.Configure();
			Date.PrototypeObject.Configure();
			Math.Configure();
			Json.Configure();
			Error.Configure();
			Error.PrototypeObject.Configure();
			GlobalEnvironment = LexicalEnvironment.NewObjectEnvironment(this, Global, null, provideThis: false);
			EnterExecutionContext(GlobalEnvironment, GlobalEnvironment, Global);
			Options = new Options();
			options?.Invoke(Options);
			Eval = new EvalFunctionInstance(this, new string[0], LexicalEnvironment.NewDeclarativeEnvironment(this, ExecutionContext.LexicalEnvironment), StrictModeScope.IsStrictModeCode);
			Global.FastAddProperty("eval", Eval, writable: true, enumerable: false, configurable: true);
			_statements = new StatementInterpreter(this);
			_expressions = new ExpressionInterpreter(this);
			if (Options._IsClrAllowed)
			{
				Global.FastAddProperty("System", new NamespaceReference(this, "System"), writable: false, enumerable: false, configurable: false);
				Global.FastAddProperty("importNamespace", new ClrFunctionInstance(this, (JsValue thisObj, JsValue[] arguments) => new NamespaceReference(this, TypeConverter.ToString(arguments.At(0)))), writable: false, enumerable: false, configurable: false);
			}
			ClrTypeConverter = new DefaultTypeConverter(this);
			BreakPoints = new List<BreakPoint>();
			DebugHandler = new DebugHandler(this);
		}

		internal StepMode? InvokeStepEvent(DebugInformation info)
		{
			if (this.Step != null)
			{
				return this.Step(this, info);
			}
			return null;
		}

		internal StepMode? InvokeBreakEvent(DebugInformation info)
		{
			if (this.Break != null)
			{
				return this.Break(this, info);
			}
			return null;
		}

		public Runtime.Environments.ExecutionContext EnterExecutionContext(LexicalEnvironment lexicalEnvironment, LexicalEnvironment variableEnvironment, JsValue thisBinding)
		{
			var executionContext = new Runtime.Environments.ExecutionContext
			{
				LexicalEnvironment = lexicalEnvironment,
				VariableEnvironment = variableEnvironment,
				ThisBinding = thisBinding
			};
			_executionContexts.Push(executionContext);
			return executionContext;
		}

		public Engine SetValue(string name, Delegate value)
		{
			Global.FastAddProperty(name, new DelegateWrapper(this, value), writable: true, enumerable: false, configurable: true);
			return this;
		}

		public Engine SetValue(string name, string value)
		{
			return SetValue(name, new JsValue(value));
		}

		public Engine SetValue(string name, double value)
		{
			return SetValue(name, new JsValue(value));
		}

		public Engine SetValue(string name, bool value)
		{
			return SetValue(name, new JsValue(value));
		}

		public Engine SetValue(string name, JsValue value)
		{
			Global.Put(name, value, throwOnError: false);
			return this;
		}

		public Engine SetValue(string name, object obj)
		{
			return SetValue(name, JsValue.FromObject(this, obj));
		}

		public void LeaveExecutionContext()
		{
			_executionContexts.Pop();
		}

		public void ResetStatementsCount()
		{
			_statementsCount = 0;
		}

		public void ResetTimeoutTicks()
		{
			long ticks = Options._TimeoutInterval.Ticks;
			_timeoutTicks = ((ticks > 0) ? (DateTime.UtcNow.Ticks + ticks) : 0);
		}

		public void ResetCallStack()
		{
			CallStack.Clear();
		}

		public Engine Execute(string source)
		{
			JavaScriptParser javaScriptParser = new JavaScriptParser();
			return Execute(javaScriptParser.Parse(source));
		}

		public Engine Execute(string source, ParserOptions parserOptions)
		{
			JavaScriptParser javaScriptParser = new JavaScriptParser();
			return Execute(javaScriptParser.Parse(source, parserOptions));
		}

		public Engine Execute(Program program)
		{
			ResetStatementsCount();
			ResetTimeoutTicks();
			ResetLastStatement();
			ResetCallStack();
			using (new StrictModeScope(Options._IsStrict || program.Strict))
			{
				DeclarationBindingInstantiation(DeclarationBindingType.GlobalCode, program.FunctionDeclarations, program.VariableDeclarations, null, null);
				Completion completion = _statements.ExecuteProgram(program);
				if (completion.Type == Completion.Throw)
				{
					throw new JavaScriptException(completion.GetValueOrDefault())
					{
						Location = completion.Location
					};
				}
				_completionValue = completion.GetValueOrDefault();
				return this;
			}
		}

		private void ResetLastStatement()
		{
			_lastSyntaxNode = null;
		}

		public JsValue GetCompletionValue()
		{
			return _completionValue;
		}

		public Completion ExecuteStatement(Statement statement)
		{
			int maxStatements = Options._MaxStatements;
			if (maxStatements > 0 && _statementsCount++ > maxStatements)
			{
				throw new StatementsCountOverflowException();
			}
			if (_timeoutTicks > 0 && _timeoutTicks < DateTime.UtcNow.Ticks)
			{
				throw new TimeoutException();
			}
			_lastSyntaxNode = statement;
			if (Options._IsDebugMode)
			{
				DebugHandler.OnStep(statement);
			}
			return statement.Type switch
			{
				SyntaxNodes.BlockStatement => _statements.ExecuteBlockStatement(statement.As<BlockStatement>()), 
				SyntaxNodes.BreakStatement => _statements.ExecuteBreakStatement(statement.As<BreakStatement>()), 
				SyntaxNodes.ContinueStatement => _statements.ExecuteContinueStatement(statement.As<ContinueStatement>()), 
				SyntaxNodes.DoWhileStatement => _statements.ExecuteDoWhileStatement(statement.As<DoWhileStatement>()), 
				SyntaxNodes.DebuggerStatement => _statements.ExecuteDebuggerStatement(statement.As<DebuggerStatement>()), 
				SyntaxNodes.EmptyStatement => _statements.ExecuteEmptyStatement(statement.As<EmptyStatement>()), 
				SyntaxNodes.ExpressionStatement => _statements.ExecuteExpressionStatement(statement.As<ExpressionStatement>()), 
				SyntaxNodes.ForStatement => _statements.ExecuteForStatement(statement.As<ForStatement>()), 
				SyntaxNodes.ForInStatement => _statements.ExecuteForInStatement(statement.As<ForInStatement>()), 
				SyntaxNodes.FunctionDeclaration => new Completion(Completion.Normal, null, null), 
				SyntaxNodes.IfStatement => _statements.ExecuteIfStatement(statement.As<IfStatement>()), 
				SyntaxNodes.LabeledStatement => _statements.ExecuteLabelledStatement(statement.As<LabelledStatement>()), 
				SyntaxNodes.ReturnStatement => _statements.ExecuteReturnStatement(statement.As<ReturnStatement>()), 
				SyntaxNodes.SwitchStatement => _statements.ExecuteSwitchStatement(statement.As<SwitchStatement>()), 
				SyntaxNodes.ThrowStatement => _statements.ExecuteThrowStatement(statement.As<ThrowStatement>()), 
				SyntaxNodes.TryStatement => _statements.ExecuteTryStatement(statement.As<TryStatement>()), 
				SyntaxNodes.VariableDeclaration => _statements.ExecuteVariableDeclaration(statement.As<VariableDeclaration>()), 
				SyntaxNodes.WhileStatement => _statements.ExecuteWhileStatement(statement.As<WhileStatement>()), 
				SyntaxNodes.WithStatement => _statements.ExecuteWithStatement(statement.As<WithStatement>()), 
				SyntaxNodes.Program => _statements.ExecuteProgram(statement.As<Program>()), 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}

		public object EvaluateExpression(Expression expression)
		{
			_lastSyntaxNode = expression;
			return expression.Type switch
			{
				SyntaxNodes.AssignmentExpression => _expressions.EvaluateAssignmentExpression(expression.As<AssignmentExpression>()), 
				SyntaxNodes.ArrayExpression => _expressions.EvaluateArrayExpression(expression.As<ArrayExpression>()), 
				SyntaxNodes.BinaryExpression => _expressions.EvaluateBinaryExpression(expression.As<BinaryExpression>()), 
				SyntaxNodes.CallExpression => _expressions.EvaluateCallExpression(expression.As<CallExpression>()), 
				SyntaxNodes.ConditionalExpression => _expressions.EvaluateConditionalExpression(expression.As<ConditionalExpression>()), 
				SyntaxNodes.FunctionExpression => _expressions.EvaluateFunctionExpression(expression.As<FunctionExpression>()), 
				SyntaxNodes.Identifier => _expressions.EvaluateIdentifier(expression.As<Identifier>()), 
				SyntaxNodes.Literal => _expressions.EvaluateLiteral(expression.As<Literal>()), 
				SyntaxNodes.RegularExpressionLiteral => _expressions.EvaluateLiteral(expression.As<Literal>()), 
				SyntaxNodes.LogicalExpression => _expressions.EvaluateLogicalExpression(expression.As<LogicalExpression>()), 
				SyntaxNodes.MemberExpression => _expressions.EvaluateMemberExpression(expression.As<MemberExpression>()), 
				SyntaxNodes.NewExpression => _expressions.EvaluateNewExpression(expression.As<NewExpression>()), 
				SyntaxNodes.ObjectExpression => _expressions.EvaluateObjectExpression(expression.As<ObjectExpression>()), 
				SyntaxNodes.SequenceExpression => _expressions.EvaluateSequenceExpression(expression.As<SequenceExpression>()), 
				SyntaxNodes.ThisExpression => _expressions.EvaluateThisExpression(expression.As<ThisExpression>()), 
				SyntaxNodes.UpdateExpression => _expressions.EvaluateUpdateExpression(expression.As<UpdateExpression>()), 
				SyntaxNodes.UnaryExpression => _expressions.EvaluateUnaryExpression(expression.As<UnaryExpression>()), 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}

		public JsValue GetValue(object value)
		{
			if (!(value is Reference reference))
			{
				if (value is Completion completion)
				{
					return GetValue(completion.Value);
				}
				return (JsValue)value;
			}
			if (reference.IsUnresolvableReference())
			{
				throw new JavaScriptException(ReferenceError, reference.GetReferencedName() + " is not defined");
			}
			JsValue @base = reference.GetBase();
			if (reference.IsPropertyReference())
			{
				if (!reference.HasPrimitiveBase())
				{
					ObjectInstance objectInstance = TypeConverter.ToObject(this, @base);
					return objectInstance.Get(reference.GetReferencedName());
				}
				ObjectInstance objectInstance2 = TypeConverter.ToObject(this, @base);
				PropertyDescriptor property = objectInstance2.GetProperty(reference.GetReferencedName());
				if (property == PropertyDescriptor.Undefined)
				{
					return JsValue.Undefined;
				}
				if (property.IsDataDescriptor())
				{
					return property.Value.Value;
				}
				JsValue value2 = property.Get.Value;
				if (value2 == Undefined.Instance)
				{
					return Undefined.Instance;
				}
				ICallable callable = (ICallable)value2.AsObject();
				return callable.Call(@base, Arguments.Empty);
			}
			EnvironmentRecord environmentRecord = @base.As<EnvironmentRecord>();
			if (environmentRecord == null)
			{
				throw new ArgumentException();
			}
			return environmentRecord.GetBindingValue(reference.GetReferencedName(), reference.IsStrict());
		}

		public void PutValue(Reference reference, JsValue value)
		{
			if (reference.IsUnresolvableReference())
			{
				if (reference.IsStrict())
				{
					throw new JavaScriptException(ReferenceError);
				}
				Global.Put(reference.GetReferencedName(), value, throwOnError: false);
				return;
			}
			if (reference.IsPropertyReference())
			{
				JsValue @base = reference.GetBase();
				if (!reference.HasPrimitiveBase())
				{
					@base.AsObject().Put(reference.GetReferencedName(), value, reference.IsStrict());
				}
				else
				{
					PutPrimitiveBase(@base, reference.GetReferencedName(), value, reference.IsStrict());
				}
				return;
			}
			EnvironmentRecord environmentRecord = reference.GetBase().As<EnvironmentRecord>();
			if (environmentRecord == null)
			{
				throw new ArgumentNullException();
			}
			environmentRecord.SetMutableBinding(reference.GetReferencedName(), value, reference.IsStrict());
		}

		public void PutPrimitiveBase(JsValue b, string name, JsValue value, bool throwOnError)
		{
			ObjectInstance objectInstance = TypeConverter.ToObject(this, b);
			if (!objectInstance.CanPut(name))
			{
				if (throwOnError)
				{
					throw new JavaScriptException(TypeError);
				}
				return;
			}
			PropertyDescriptor ownProperty = objectInstance.GetOwnProperty(name);
			if (ownProperty.IsDataDescriptor())
			{
				if (!throwOnError)
				{
					return;
				}
				throw new JavaScriptException(TypeError);
			}
			PropertyDescriptor property = objectInstance.GetProperty(name);
			if (property.IsAccessorDescriptor())
			{
				ICallable callable = (ICallable)property.Set.Value.AsObject();
				callable.Call(b, new JsValue[1] { value });
			}
			else if (throwOnError)
			{
				throw new JavaScriptException(TypeError);
			}
		}

		public JsValue Invoke(string propertyName, params object[] arguments)
		{
			return Invoke(propertyName, null, arguments);
		}

		public JsValue Invoke(string propertyName, object thisObj, object[] arguments)
		{
			ICallable callable = GetValue(propertyName).TryCast<ICallable>();
			if (callable == null)
			{
				throw new ArgumentException("Can only invoke functions");
			}
			return callable.Call(JsValue.FromObject(this, thisObj), arguments.Select((object x) => JsValue.FromObject(this, x)).ToArray());
		}

		public JsValue GetValue(string propertyName)
		{
			return GetValue(Global, propertyName);
		}

		public SyntaxNode GetLastSyntaxNode()
		{
			return _lastSyntaxNode;
		}

		public JsValue GetValue(JsValue scope, string propertyName)
		{
			if (string.IsNullOrEmpty(propertyName))
			{
				throw new ArgumentException("propertyName");
			}
			Reference value = new Reference(scope, propertyName, Options._IsStrict);
			return GetValue(value);
		}

		public void DeclarationBindingInstantiation(DeclarationBindingType declarationBindingType, IList<FunctionDeclaration> functionDeclarations, IList<VariableDeclaration> variableDeclarations, FunctionInstance functionInstance, JsValue[] arguments)
		{
			EnvironmentRecord record = ExecutionContext.VariableEnvironment.Record;
			bool flag = declarationBindingType == DeclarationBindingType.EvalCode;
			bool isStrictModeCode = StrictModeScope.IsStrictModeCode;
			if (declarationBindingType == DeclarationBindingType.FunctionCode)
			{
				int num = arguments.Length;
				int num2 = 0;
				string[] formalParameters = functionInstance.FormalParameters;
				string[] array = formalParameters;
				foreach (string name in array)
				{
					num2++;
					JsValue value = ((num2 > num) ? Undefined.Instance : arguments[num2 - 1]);
					if (!record.HasBinding(name))
					{
						record.CreateMutableBinding(name);
					}
					record.SetMutableBinding(name, value, isStrictModeCode);
				}
			}
			foreach (FunctionDeclaration functionDeclaration in functionDeclarations)
			{
				string name2 = functionDeclaration.Id.Name;
				FunctionInstance functionInstance2 = Function.CreateFunctionObject(functionDeclaration);
				if (!record.HasBinding(name2))
				{
					record.CreateMutableBinding(name2, flag);
				}
				else if (record == GlobalEnvironment.Record)
				{
					GlobalObject global = Global;
					PropertyDescriptor property = global.GetProperty(name2);
					if (property.Configurable.Value)
					{
						global.DefineOwnProperty(name2, new PropertyDescriptor(Undefined.Instance, true, true, flag), throwOnError: true);
					}
					else if (property.IsAccessorDescriptor() || !property.Enumerable.Value)
					{
						throw new JavaScriptException(TypeError);
					}
				}
				record.SetMutableBinding(name2, functionInstance2, isStrictModeCode);
			}
			bool flag2 = record.HasBinding("arguments");
			if (declarationBindingType == DeclarationBindingType.FunctionCode && !flag2)
			{
				ArgumentsInstance argumentsInstance = ArgumentsInstance.CreateArgumentsObject(this, functionInstance, functionInstance.FormalParameters, arguments, record, isStrictModeCode);
				if (isStrictModeCode)
				{
					if (!(record is DeclarativeEnvironmentRecord declarativeEnvironmentRecord))
					{
						throw new ArgumentException();
					}
					declarativeEnvironmentRecord.CreateImmutableBinding("arguments");
					declarativeEnvironmentRecord.InitializeImmutableBinding("arguments", argumentsInstance);
				}
				else
				{
					record.CreateMutableBinding("arguments");
					record.SetMutableBinding("arguments", argumentsInstance, strict: false);
				}
			}
			foreach (VariableDeclarator item in variableDeclarations.SelectMany((VariableDeclaration x) => x.Declarations))
			{
				string name3 = item.Id.Name;
				if (!record.HasBinding(name3))
				{
					record.CreateMutableBinding(name3, flag);
					record.SetMutableBinding(name3, Undefined.Instance, isStrictModeCode);
				}
			}
		}
	}
}
