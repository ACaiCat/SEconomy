using System;
using System.Linq;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Number;
using Jint.Native.Object;
using Jint.Parser.Ast;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Environments;
using Jint.Runtime.References;

namespace Jint.Runtime
{
	public class ExpressionInterpreter
	{
		private readonly Engine _engine;

		public ExpressionInterpreter(Engine engine)
		{
			_engine = engine;
		}

		private object EvaluateExpression(Expression expression)
		{
			return _engine.EvaluateExpression(expression);
		}

		public JsValue EvaluateConditionalExpression(ConditionalExpression conditionalExpression)
		{
			object value = _engine.EvaluateExpression(conditionalExpression.Test);
			if (TypeConverter.ToBoolean(_engine.GetValue(value)))
			{
				object value2 = _engine.EvaluateExpression(conditionalExpression.Consequent);
				return _engine.GetValue(value2);
			}
			object value3 = _engine.EvaluateExpression(conditionalExpression.Alternate);
			return _engine.GetValue(value3);
		}

		public JsValue EvaluateAssignmentExpression(AssignmentExpression assignmentExpression)
		{
			Reference reference = EvaluateExpression(assignmentExpression.Left) as Reference;
			JsValue value = _engine.GetValue(EvaluateExpression(assignmentExpression.Right));
			if (reference == null)
			{
				throw new JavaScriptException(_engine.ReferenceError);
			}
			if (assignmentExpression.Operator == AssignmentOperator.Assign)
			{
				if (reference.IsStrict() && reference.GetBase().TryCast<EnvironmentRecord>() != null && (reference.GetReferencedName() == "eval" || reference.GetReferencedName() == "arguments"))
				{
					throw new JavaScriptException(_engine.SyntaxError);
				}
				_engine.PutValue(reference, value);
				return value;
			}
			JsValue value2 = _engine.GetValue(reference);
			switch (assignmentExpression.Operator)
			{
			case AssignmentOperator.PlusAssign:
			{
				JsValue jsValue = TypeConverter.ToPrimitive(value2);
				JsValue jsValue2 = TypeConverter.ToPrimitive(value);
				value2 = ((!jsValue.IsString() && !jsValue2.IsString()) ? ((JsValue)(TypeConverter.ToNumber(jsValue) + TypeConverter.ToNumber(jsValue2))) : ((JsValue)(TypeConverter.ToString(jsValue) + TypeConverter.ToString(jsValue2))));
				break;
			}
			case AssignmentOperator.MinusAssign:
				value2 = TypeConverter.ToNumber(value2) - TypeConverter.ToNumber(value);
				break;
			case AssignmentOperator.TimesAssign:
				value2 = ((!(value2 == Undefined.Instance) && !(value == Undefined.Instance)) ? ((JsValue)(TypeConverter.ToNumber(value2) * TypeConverter.ToNumber(value))) : Undefined.Instance);
				break;
			case AssignmentOperator.DivideAssign:
				value2 = Divide(value2, value);
				break;
			case AssignmentOperator.ModuloAssign:
				value2 = ((!(value2 == Undefined.Instance) && !(value == Undefined.Instance)) ? ((JsValue)(TypeConverter.ToNumber(value2) % TypeConverter.ToNumber(value))) : Undefined.Instance);
				break;
			case AssignmentOperator.BitwiseAndAssign:
				value2 = TypeConverter.ToInt32(value2) & TypeConverter.ToInt32(value);
				break;
			case AssignmentOperator.BitwiseOrAssign:
				value2 = TypeConverter.ToInt32(value2) | TypeConverter.ToInt32(value);
				break;
			case AssignmentOperator.BitwiseXOrAssign:
				value2 = TypeConverter.ToInt32(value2) ^ TypeConverter.ToInt32(value);
				break;
			case AssignmentOperator.LeftShiftAssign:
				value2 = TypeConverter.ToInt32(value2) << (int)(TypeConverter.ToUint32(value) & 0x1F);
				break;
			case AssignmentOperator.RightShiftAssign:
				value2 = TypeConverter.ToInt32(value2) >> (int)(TypeConverter.ToUint32(value) & 0x1F);
				break;
			case AssignmentOperator.UnsignedRightShiftAssign:
				value2 = (uint)TypeConverter.ToInt32(value2) >> (int)(TypeConverter.ToUint32(value) & 0x1F);
				break;
			default:
				throw new NotImplementedException();
			}
			_engine.PutValue(reference, value2);
			return value2;
		}

		private JsValue Divide(JsValue lval, JsValue rval)
		{
			if (lval == Undefined.Instance || rval == Undefined.Instance)
			{
				return Undefined.Instance;
			}
			double num = TypeConverter.ToNumber(lval);
			double num2 = TypeConverter.ToNumber(rval);
			if (double.IsNaN(num2) || double.IsNaN(num))
			{
				return double.NaN;
			}
			if (double.IsInfinity(num) && double.IsInfinity(num2))
			{
				return double.NaN;
			}
			if (double.IsInfinity(num) && num2.Equals(0.0))
			{
				if (NumberInstance.IsNegativeZero(num2))
				{
					return 0.0 - num;
				}
				return num;
			}
			if (num.Equals(0.0) && num2.Equals(0.0))
			{
				return double.NaN;
			}
			if (num2.Equals(0.0))
			{
				if (NumberInstance.IsNegativeZero(num2))
				{
					return (num > 0.0) ? double.NegativeInfinity : double.PositiveInfinity;
				}
				return (num > 0.0) ? double.PositiveInfinity : double.NegativeInfinity;
			}
			return num / num2;
		}

		public JsValue EvaluateBinaryExpression(BinaryExpression expression)
		{
			object value = EvaluateExpression(expression.Left);
			JsValue value2 = _engine.GetValue(value);
			object value3 = EvaluateExpression(expression.Right);
			JsValue value4 = _engine.GetValue(value3);
			JsValue jsValue;
			switch (expression.Operator)
			{
			case BinaryOperator.Plus:
			{
				JsValue jsValue2 = TypeConverter.ToPrimitive(value2);
				JsValue jsValue3 = TypeConverter.ToPrimitive(value4);
				jsValue = ((!jsValue2.IsString() && !jsValue3.IsString()) ? ((JsValue)(TypeConverter.ToNumber(jsValue2) + TypeConverter.ToNumber(jsValue3))) : ((JsValue)(TypeConverter.ToString(jsValue2) + TypeConverter.ToString(jsValue3))));
				break;
			}
			case BinaryOperator.Minus:
				jsValue = TypeConverter.ToNumber(value2) - TypeConverter.ToNumber(value4);
				break;
			case BinaryOperator.Times:
				jsValue = ((!(value2 == Undefined.Instance) && !(value4 == Undefined.Instance)) ? ((JsValue)(TypeConverter.ToNumber(value2) * TypeConverter.ToNumber(value4))) : Undefined.Instance);
				break;
			case BinaryOperator.Divide:
				jsValue = Divide(value2, value4);
				break;
			case BinaryOperator.Modulo:
				jsValue = ((!(value2 == Undefined.Instance) && !(value4 == Undefined.Instance)) ? ((JsValue)(TypeConverter.ToNumber(value2) % TypeConverter.ToNumber(value4))) : Undefined.Instance);
				break;
			case BinaryOperator.Equal:
				jsValue = Equal(value2, value4);
				break;
			case BinaryOperator.NotEqual:
				jsValue = !Equal(value2, value4);
				break;
			case BinaryOperator.Greater:
				jsValue = Compare(value4, value2, leftFirst: false);
				if (jsValue == Undefined.Instance)
				{
					jsValue = false;
				}
				break;
			case BinaryOperator.GreaterOrEqual:
				jsValue = Compare(value2, value4);
				jsValue = ((!(jsValue == Undefined.Instance) && !jsValue.AsBoolean()) ? ((JsValue)true) : ((JsValue)false));
				break;
			case BinaryOperator.Less:
				jsValue = Compare(value2, value4);
				if (jsValue == Undefined.Instance)
				{
					jsValue = false;
				}
				break;
			case BinaryOperator.LessOrEqual:
				jsValue = Compare(value4, value2, leftFirst: false);
				jsValue = ((!(jsValue == Undefined.Instance) && !jsValue.AsBoolean()) ? ((JsValue)true) : ((JsValue)false));
				break;
			case BinaryOperator.StrictlyEqual:
				return StrictlyEqual(value2, value4);
			case BinaryOperator.StricltyNotEqual:
				return !StrictlyEqual(value2, value4);
			case BinaryOperator.BitwiseAnd:
				return TypeConverter.ToInt32(value2) & TypeConverter.ToInt32(value4);
			case BinaryOperator.BitwiseOr:
				return TypeConverter.ToInt32(value2) | TypeConverter.ToInt32(value4);
			case BinaryOperator.BitwiseXOr:
				return TypeConverter.ToInt32(value2) ^ TypeConverter.ToInt32(value4);
			case BinaryOperator.LeftShift:
				return TypeConverter.ToInt32(value2) << (int)(TypeConverter.ToUint32(value4) & 0x1F);
			case BinaryOperator.RightShift:
				return TypeConverter.ToInt32(value2) >> (int)(TypeConverter.ToUint32(value4) & 0x1F);
			case BinaryOperator.UnsignedRightShift:
				return (uint)TypeConverter.ToInt32(value2) >> (int)(TypeConverter.ToUint32(value4) & 0x1F);
			case BinaryOperator.InstanceOf:
			{
				FunctionInstance functionInstance = value4.TryCast<FunctionInstance>();
				if (functionInstance == null)
				{
					throw new JavaScriptException(_engine.TypeError, "instanceof can only be used with a function object");
				}
				jsValue = functionInstance.HasInstance(value2);
				break;
			}
			case BinaryOperator.In:
				if (!value4.IsObject())
				{
					throw new JavaScriptException(_engine.TypeError, "in can only be used with an object");
				}
				jsValue = value4.AsObject().HasProperty(TypeConverter.ToString(value2));
				break;
			default:
				throw new NotImplementedException();
			}
			return jsValue;
		}

		public JsValue EvaluateLogicalExpression(LogicalExpression logicalExpression)
		{
			JsValue value = _engine.GetValue(EvaluateExpression(logicalExpression.Left));
			switch (logicalExpression.Operator)
			{
			case LogicalOperator.LogicalAnd:
				if (!TypeConverter.ToBoolean(value))
				{
					return value;
				}
				return _engine.GetValue(EvaluateExpression(logicalExpression.Right));
			case LogicalOperator.LogicalOr:
				if (TypeConverter.ToBoolean(value))
				{
					return value;
				}
				return _engine.GetValue(EvaluateExpression(logicalExpression.Right));
			default:
				throw new NotImplementedException();
			}
		}

		public static bool Equal(JsValue x, JsValue y)
		{
			Types type = x.Type;
			Types type2 = y.Type;
			if (type == type2)
			{
				switch (type)
				{
				case Types.Undefined:
				case Types.Null:
					return true;
				case Types.Number:
				{
					double d = TypeConverter.ToNumber(x);
					double num = TypeConverter.ToNumber(y);
					if (double.IsNaN(d) || double.IsNaN(num))
					{
						return false;
					}
					if (d.Equals(num))
					{
						return true;
					}
					return false;
				}
				case Types.String:
					return TypeConverter.ToString(x) == TypeConverter.ToString(y);
				case Types.Boolean:
					return x.AsBoolean() == y.AsBoolean();
				default:
					return x == y;
				}
			}
			if (x == Null.Instance && y == Undefined.Instance)
			{
				return true;
			}
			if (x == Undefined.Instance && y == Null.Instance)
			{
				return true;
			}
			if (type == Types.Number && type2 == Types.String)
			{
				return Equal(x, TypeConverter.ToNumber(y));
			}
			if (type == Types.String && type2 == Types.Number)
			{
				return Equal(TypeConverter.ToNumber(x), y);
			}
			if (type == Types.Boolean)
			{
				return Equal(TypeConverter.ToNumber(x), y);
			}
			int num2;
			switch (type2)
			{
			case Types.Boolean:
				return Equal(x, TypeConverter.ToNumber(y));
			case Types.Object:
				num2 = ((type == Types.String || type == Types.Number) ? 1 : 0);
				break;
			default:
				num2 = 0;
				break;
			}
			if (num2 != 0)
			{
				return Equal(x, TypeConverter.ToPrimitive(y));
			}
			if (type == Types.Object && (type2 == Types.String || type2 == Types.Number))
			{
				return Equal(TypeConverter.ToPrimitive(x), y);
			}
			return false;
		}

		public static bool StrictlyEqual(JsValue x, JsValue y)
		{
			Types type = x.Type;
			Types type2 = y.Type;
			if (type != type2)
			{
				return false;
			}
			switch (type)
			{
			case Types.Undefined:
			case Types.Null:
				return true;
			case Types.None:
				return true;
			case Types.Number:
			{
				double d = TypeConverter.ToNumber(x);
				double num = TypeConverter.ToNumber(y);
				if (double.IsNaN(d) || double.IsNaN(num))
				{
					return false;
				}
				if (d.Equals(num))
				{
					return true;
				}
				return false;
			}
			case Types.String:
				return TypeConverter.ToString(x) == TypeConverter.ToString(y);
			case Types.Boolean:
				return TypeConverter.ToBoolean(x) == TypeConverter.ToBoolean(y);
			default:
				return x == y;
			}
		}

		public static bool SameValue(JsValue x, JsValue y)
		{
			Types primitiveType = TypeConverter.GetPrimitiveType(x);
			Types primitiveType2 = TypeConverter.GetPrimitiveType(y);
			if (primitiveType != primitiveType2)
			{
				return false;
			}
			switch (primitiveType)
			{
			case Types.None:
				return true;
			case Types.Number:
			{
				double num = TypeConverter.ToNumber(x);
				double num2 = TypeConverter.ToNumber(y);
				if (double.IsNaN(num) && double.IsNaN(num2))
				{
					return true;
				}
				if (num.Equals(num2))
				{
					if (num.Equals(0.0))
					{
						return NumberInstance.IsNegativeZero(num) == NumberInstance.IsNegativeZero(num2);
					}
					return true;
				}
				return false;
			}
			case Types.String:
				return TypeConverter.ToString(x) == TypeConverter.ToString(y);
			case Types.Boolean:
				return TypeConverter.ToBoolean(x) == TypeConverter.ToBoolean(y);
			default:
				return x == y;
			}
		}

		public static JsValue Compare(JsValue x, JsValue y, bool leftFirst = true)
		{
			JsValue o;
			JsValue o2;
			if (leftFirst)
			{
				o = TypeConverter.ToPrimitive(x, Types.Number);
				o2 = TypeConverter.ToPrimitive(y, Types.Number);
			}
			else
			{
				o2 = TypeConverter.ToPrimitive(y, Types.Number);
				o = TypeConverter.ToPrimitive(x, Types.Number);
			}
			Types type = o.Type;
			Types type2 = o2.Type;
			if (type != Types.String || type2 != Types.String)
			{
				double num = TypeConverter.ToNumber(o);
				double num2 = TypeConverter.ToNumber(o2);
				if (double.IsNaN(num) || double.IsNaN(num2))
				{
					return Undefined.Instance;
				}
				if (num.Equals(num2))
				{
					return false;
				}
				if (double.IsPositiveInfinity(num))
				{
					return false;
				}
				if (double.IsPositiveInfinity(num2))
				{
					return true;
				}
				if (double.IsNegativeInfinity(num2))
				{
					return false;
				}
				if (double.IsNegativeInfinity(num))
				{
					return true;
				}
				return num < num2;
			}
			return string.CompareOrdinal(TypeConverter.ToString(x), TypeConverter.ToString(y)) < 0;
		}

		public Reference EvaluateIdentifier(Identifier identifier)
		{
			LexicalEnvironment lexicalEnvironment = _engine.ExecutionContext.LexicalEnvironment;
			bool isStrictModeCode = StrictModeScope.IsStrictModeCode;
			return LexicalEnvironment.GetIdentifierReference(lexicalEnvironment, identifier.Name, isStrictModeCode);
		}

		public JsValue EvaluateLiteral(Literal literal)
		{
			if (literal.Cached)
			{
				return literal.CachedValue;
			}
			literal.Cached = true;
			if (literal.Type == SyntaxNodes.RegularExpressionLiteral)
			{
				return literal.CachedValue = _engine.RegExp.Construct(literal.Raw);
			}
			return literal.CachedValue = JsValue.FromObject(_engine, literal.Value);
		}

		public JsValue EvaluateObjectExpression(ObjectExpression objectExpression)
		{
			ObjectInstance objectInstance = _engine.Object.Construct(Arguments.Empty);
			foreach (Property property in objectExpression.Properties)
			{
				string key = property.Key.GetKey();
				PropertyDescriptor ownProperty = objectInstance.GetOwnProperty(key);
				PropertyDescriptor propertyDescriptor;
				switch (property.Kind)
				{
				case PropertyKind.Data:
				{
					object value = _engine.EvaluateExpression(property.Value);
					JsValue value2 = _engine.GetValue(value);
					propertyDescriptor = new PropertyDescriptor(value2, true, true, true);
					break;
				}
				case PropertyKind.Get:
				{
					if (!(property.Value is FunctionExpression functionExpression2))
					{
						throw new JavaScriptException(_engine.SyntaxError);
					}
					ScriptFunctionInstance scriptFunctionInstance2;
					using (new StrictModeScope(functionExpression2.Strict))
					{
						scriptFunctionInstance2 = new ScriptFunctionInstance(_engine, functionExpression2, _engine.ExecutionContext.LexicalEnvironment, StrictModeScope.IsStrictModeCode);
					}
					propertyDescriptor = new PropertyDescriptor((JsValue?)(JsValue)scriptFunctionInstance2, (JsValue?)null, (bool?)true, (bool?)true);
					break;
				}
				case PropertyKind.Set:
				{
					if (!(property.Value is FunctionExpression functionExpression))
					{
						throw new JavaScriptException(_engine.SyntaxError);
					}
					ScriptFunctionInstance scriptFunctionInstance;
					using (new StrictModeScope(functionExpression.Strict))
					{
						scriptFunctionInstance = new ScriptFunctionInstance(_engine, functionExpression, _engine.ExecutionContext.LexicalEnvironment, StrictModeScope.IsStrictModeCode);
					}
					propertyDescriptor = new PropertyDescriptor(null, scriptFunctionInstance, true, true);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
				}
				if (ownProperty != PropertyDescriptor.Undefined)
				{
					if (StrictModeScope.IsStrictModeCode && ownProperty.IsDataDescriptor() && propertyDescriptor.IsDataDescriptor())
					{
						throw new JavaScriptException(_engine.SyntaxError);
					}
					if (ownProperty.IsDataDescriptor() && propertyDescriptor.IsAccessorDescriptor())
					{
						throw new JavaScriptException(_engine.SyntaxError);
					}
					if (ownProperty.IsAccessorDescriptor() && propertyDescriptor.IsDataDescriptor())
					{
						throw new JavaScriptException(_engine.SyntaxError);
					}
					if (ownProperty.IsAccessorDescriptor() && propertyDescriptor.IsAccessorDescriptor())
					{
						if (propertyDescriptor.Set.HasValue && ownProperty.Set.HasValue)
						{
							throw new JavaScriptException(_engine.SyntaxError);
						}
						if (propertyDescriptor.Get.HasValue && ownProperty.Get.HasValue)
						{
							throw new JavaScriptException(_engine.SyntaxError);
						}
					}
				}
				objectInstance.DefineOwnProperty(key, propertyDescriptor, throwOnError: false);
			}
			return objectInstance;
		}

		public Reference EvaluateMemberExpression(MemberExpression memberExpression)
		{
			object value = EvaluateExpression(memberExpression.Object);
			JsValue value2 = _engine.GetValue(value);
			Expression expression = memberExpression.Property;
			if (!memberExpression.Computed)
			{
				expression = new Literal
				{
					Type = SyntaxNodes.Literal,
					Value = memberExpression.Property.As<Identifier>().Name
				};
			}
			object value3 = EvaluateExpression(expression);
			JsValue value4 = _engine.GetValue(value3);
			TypeConverter.CheckObjectCoercible(_engine, value2);
			string name = TypeConverter.ToString(value4);
			return new Reference(value2, name, StrictModeScope.IsStrictModeCode);
		}

		public JsValue EvaluateFunctionExpression(FunctionExpression functionExpression)
		{
			LexicalEnvironment lexicalEnvironment = LexicalEnvironment.NewDeclarativeEnvironment(_engine, _engine.ExecutionContext.LexicalEnvironment);
			DeclarativeEnvironmentRecord declarativeEnvironmentRecord = (DeclarativeEnvironmentRecord)lexicalEnvironment.Record;
			if (functionExpression.Id != null && !string.IsNullOrEmpty(functionExpression.Id.Name))
			{
				declarativeEnvironmentRecord.CreateMutableBinding(functionExpression.Id.Name);
			}
			ScriptFunctionInstance scriptFunctionInstance = new ScriptFunctionInstance(_engine, functionExpression, lexicalEnvironment, functionExpression.Strict);
			if (functionExpression.Id != null && !string.IsNullOrEmpty(functionExpression.Id.Name))
			{
				declarativeEnvironmentRecord.InitializeImmutableBinding(functionExpression.Id.Name, scriptFunctionInstance);
			}
			return scriptFunctionInstance;
		}

		public JsValue EvaluateCallExpression(CallExpression callExpression)
		{
			object obj = EvaluateExpression(callExpression.Callee);
			if (_engine.Options._IsDebugMode)
			{
				_engine.DebugHandler.AddToDebugCallStack(callExpression);
			}
			JsValue[] array;
			if (callExpression.Cached)
			{
				array = callExpression.CachedArguments;
			}
			else
			{
				array = callExpression.Arguments.Select(EvaluateExpression).Select(_engine.GetValue).ToArray();
				if (callExpression.CanBeCached)
				{
					if (callExpression.Arguments.All((Expression x) => x is Literal))
					{
						callExpression.Cached = true;
						callExpression.CachedArguments = array;
					}
					else
					{
						callExpression.CanBeCached = false;
					}
				}
			}
			JsValue value = _engine.GetValue(obj);
			Reference reference = obj as Reference;
			if (_engine.Options._MaxRecursionDepth >= 0)
			{
				CallStackElement callStackElement = new CallStackElement(callExpression, value, (reference != null) ? reference.GetReferencedName() : "anonymous function");
				int num = _engine.CallStack.Push(callStackElement);
				if (num > _engine.Options._MaxRecursionDepth)
				{
					_engine.CallStack.Pop();
					throw new RecursionDepthOverflowException(_engine.CallStack, callStackElement.ToString());
				}
			}
			if (value == Undefined.Instance)
			{
				throw new JavaScriptException(_engine.TypeError, (reference == null) ? "" : ("Object has no method '" + (obj as Reference).GetReferencedName() + "'"));
			}
			if (!value.IsObject())
			{
				throw new JavaScriptException(_engine.TypeError, (reference == null) ? "" : ("Property '" + (obj as Reference).GetReferencedName() + "' of object is not a function"));
			}
			ICallable callable = value.TryCast<ICallable>();
			if (callable == null)
			{
				throw new JavaScriptException(_engine.TypeError);
			}
			JsValue thisObject;
			if (reference != null)
			{
				if (reference.IsPropertyReference())
				{
					thisObject = reference.GetBase();
				}
				else
				{
					EnvironmentRecord environmentRecord = reference.GetBase().TryCast<EnvironmentRecord>();
					thisObject = environmentRecord.ImplicitThisValue();
				}
			}
			else
			{
				thisObject = Undefined.Instance;
			}
			if (reference != null && reference.GetReferencedName() == "eval" && callable is EvalFunctionInstance)
			{
				return ((EvalFunctionInstance)callable).Call(thisObject, array, directCall: true);
			}
			JsValue result = callable.Call(thisObject, array);
			if (_engine.Options._IsDebugMode)
			{
				_engine.DebugHandler.PopDebugCallStack();
			}
			if (_engine.Options._MaxRecursionDepth >= 0)
			{
				_engine.CallStack.Pop();
			}
			return result;
		}

		public JsValue EvaluateSequenceExpression(SequenceExpression sequenceExpression)
		{
			JsValue result = Undefined.Instance;
			foreach (Expression expression in sequenceExpression.Expressions)
			{
				result = _engine.GetValue(_engine.EvaluateExpression(expression));
			}
			return result;
		}

		public JsValue EvaluateUpdateExpression(UpdateExpression updateExpression)
		{
			object obj = _engine.EvaluateExpression(updateExpression.Argument);
			switch (updateExpression.Operator)
			{
			case UnaryOperator.Increment:
			{
				Reference reference2 = obj as Reference;
				if (reference2 != null && reference2.IsStrict() && reference2.GetBase().TryCast<EnvironmentRecord>() != null && Array.IndexOf(new string[2] { "eval", "arguments" }, reference2.GetReferencedName()) != -1)
				{
					throw new JavaScriptException(_engine.SyntaxError);
				}
				double num3 = TypeConverter.ToNumber(_engine.GetValue(obj));
				double num4 = num3 + 1.0;
				_engine.PutValue(reference2, num4);
				return updateExpression.Prefix ? num4 : num3;
			}
			case UnaryOperator.Decrement:
			{
				Reference reference = obj as Reference;
				if (reference != null && reference.IsStrict() && reference.GetBase().TryCast<EnvironmentRecord>() != null && Array.IndexOf(new string[2] { "eval", "arguments" }, reference.GetReferencedName()) != -1)
				{
					throw new JavaScriptException(_engine.SyntaxError);
				}
				double num = TypeConverter.ToNumber(_engine.GetValue(obj));
				double num2 = num - 1.0;
				_engine.PutValue(reference, num2);
				return updateExpression.Prefix ? num2 : num;
			}
			default:
				throw new ArgumentException();
			}
		}

		public JsValue EvaluateThisExpression(ThisExpression thisExpression)
		{
			return _engine.ExecutionContext.ThisBinding;
		}

		public JsValue EvaluateNewExpression(NewExpression newExpression)
		{
			JsValue[] arguments = newExpression.Arguments.Select(EvaluateExpression).Select(_engine.GetValue).ToArray();
			IConstructor constructor = _engine.GetValue(EvaluateExpression(newExpression.Callee)).TryCast<IConstructor>();
			if (constructor == null)
			{
				throw new JavaScriptException(_engine.TypeError, "The object can't be used as constructor.");
			}
			ObjectInstance objectInstance = constructor.Construct(arguments);
			return objectInstance;
		}

		public JsValue EvaluateArrayExpression(ArrayExpression arrayExpression)
		{
			ObjectInstance objectInstance = _engine.Array.Construct(new JsValue[1] { arrayExpression.Elements.Count() });
			int num = 0;
			foreach (Expression element in arrayExpression.Elements)
			{
				if (element != null)
				{
					JsValue value = _engine.GetValue(EvaluateExpression(element));
					objectInstance.DefineOwnProperty(num.ToString(), new PropertyDescriptor(value, true, true, true), throwOnError: false);
				}
				num++;
			}
			return objectInstance;
		}

		public JsValue EvaluateUnaryExpression(UnaryExpression unaryExpression)
		{
			object obj = _engine.EvaluateExpression(unaryExpression.Argument);
			switch (unaryExpression.Operator)
			{
			case UnaryOperator.Plus:
				return TypeConverter.ToNumber(_engine.GetValue(obj));
			case UnaryOperator.Minus:
			{
				double num = TypeConverter.ToNumber(_engine.GetValue(obj));
				return double.IsNaN(num) ? double.NaN : (num * -1.0);
			}
			case UnaryOperator.BitwiseNot:
				return ~TypeConverter.ToInt32(_engine.GetValue(obj));
			case UnaryOperator.LogicalNot:
				return !TypeConverter.ToBoolean(_engine.GetValue(obj));
			case UnaryOperator.Delete:
			{
				if (!(obj is Reference reference2))
				{
					return true;
				}
				if (reference2.IsUnresolvableReference())
				{
					if (reference2.IsStrict())
					{
						throw new JavaScriptException(_engine.SyntaxError);
					}
					return true;
				}
				if (reference2.IsPropertyReference())
				{
					ObjectInstance objectInstance = TypeConverter.ToObject(_engine, reference2.GetBase());
					return objectInstance.Delete(reference2.GetReferencedName(), reference2.IsStrict());
				}
				if (reference2.IsStrict())
				{
					throw new JavaScriptException(_engine.SyntaxError);
				}
				EnvironmentRecord environmentRecord = reference2.GetBase().TryCast<EnvironmentRecord>();
				return environmentRecord.DeleteBinding(reference2.GetReferencedName());
			}
			case UnaryOperator.Void:
				_engine.GetValue(obj);
				return Undefined.Instance;
			case UnaryOperator.TypeOf:
			{
				if (obj is Reference reference && reference.IsUnresolvableReference())
				{
					return "undefined";
				}
				JsValue value = _engine.GetValue(obj);
				if (value == Undefined.Instance)
				{
					return "undefined";
				}
				if (value == Null.Instance)
				{
					return "object";
				}
				switch (value.Type)
				{
				case Types.Boolean:
					return "boolean";
				case Types.Number:
					return "number";
				case Types.String:
					return "string";
				default:
					if (value.TryCast<ICallable>() != null)
					{
						return "function";
					}
					return "object";
				}
			}
			default:
				throw new ArgumentException();
			}
		}
	}
}
