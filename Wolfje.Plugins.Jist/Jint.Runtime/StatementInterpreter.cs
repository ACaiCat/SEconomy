using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jint.Native;
using Jint.Native.Object;
using Jint.Parser.Ast;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Environments;
using Jint.Runtime.References;

namespace Jint.Runtime
{
	public class StatementInterpreter
	{
		private readonly Engine _engine;

		public StatementInterpreter(Engine engine)
		{
			_engine = engine;
		}

		private Completion ExecuteStatement(Statement statement)
		{
			return _engine.ExecuteStatement(statement);
		}

		public Completion ExecuteEmptyStatement(EmptyStatement emptyStatement)
		{
			return new Completion(Completion.Normal, null, null);
		}

		public Completion ExecuteExpressionStatement(ExpressionStatement expressionStatement)
		{
			object value = _engine.EvaluateExpression(expressionStatement.Expression);
			return new Completion(Completion.Normal, _engine.GetValue(value), null);
		}

		public Completion ExecuteIfStatement(IfStatement ifStatement)
		{
			object value = _engine.EvaluateExpression(ifStatement.Test);
			if (TypeConverter.ToBoolean(_engine.GetValue(value)))
			{
				return ExecuteStatement(ifStatement.Consequent);
			}
			if (ifStatement.Alternate == null)
			{
				return new Completion(Completion.Normal, null, null);
			}
			return ExecuteStatement(ifStatement.Alternate);
		}

		public Completion ExecuteLabelledStatement(LabelledStatement labelledStatement)
		{
			labelledStatement.Body.LabelSet = labelledStatement.Label.Name;
			Completion completion = ExecuteStatement(labelledStatement.Body);
			if (completion.Type == Completion.Break && completion.Identifier == labelledStatement.Label.Name)
			{
				return new Completion(Completion.Normal, completion.Value, null);
			}
			return completion;
		}

		public Completion ExecuteDoWhileStatement(DoWhileStatement doWhileStatement)
		{
			JsValue value = Undefined.Instance;
			object value2;
			do
			{
				Completion completion = ExecuteStatement(doWhileStatement.Body);
				if (completion.Value.HasValue)
				{
					value = completion.Value.Value;
				}
				if (completion.Type != Completion.Continue || completion.Identifier != doWhileStatement.LabelSet)
				{
					if (completion.Type == Completion.Break && (completion.Identifier == null || completion.Identifier == doWhileStatement.LabelSet))
					{
						return new Completion(Completion.Normal, value, null);
					}
					if (completion.Type != Completion.Normal)
					{
						return completion;
					}
				}
				value2 = _engine.EvaluateExpression(doWhileStatement.Test);
			}
			while (TypeConverter.ToBoolean(_engine.GetValue(value2)));
			return new Completion(Completion.Normal, value, null);
		}

		public Completion ExecuteWhileStatement(WhileStatement whileStatement)
		{
			JsValue value = Undefined.Instance;
			Completion completion;
			while (true)
			{
				object value2 = _engine.EvaluateExpression(whileStatement.Test);
				if (!TypeConverter.ToBoolean(_engine.GetValue(value2)))
				{
					return new Completion(Completion.Normal, value, null);
				}
				completion = ExecuteStatement(whileStatement.Body);
				if (completion.Value.HasValue)
				{
					value = completion.Value.Value;
				}
				if (completion.Type != Completion.Continue || completion.Identifier != whileStatement.LabelSet)
				{
					if (completion.Type == Completion.Break && (completion.Identifier == null || completion.Identifier == whileStatement.LabelSet))
					{
						return new Completion(Completion.Normal, value, null);
					}
					if (completion.Type != Completion.Normal)
					{
						break;
					}
				}
			}
			return completion;
		}

		public Completion ExecuteForStatement(ForStatement forStatement)
		{
			if (forStatement.Init != null)
			{
				if (forStatement.Init.Type == SyntaxNodes.VariableDeclaration)
				{
					ExecuteStatement(forStatement.Init.As<Statement>());
				}
				else
				{
					_engine.GetValue(_engine.EvaluateExpression(forStatement.Init.As<Expression>()));
				}
			}
			JsValue value = Undefined.Instance;
			Completion completion;
			while (true)
			{
				if (forStatement.Test != null)
				{
					object value2 = _engine.EvaluateExpression(forStatement.Test);
					if (!TypeConverter.ToBoolean(_engine.GetValue(value2)))
					{
						return new Completion(Completion.Normal, value, null);
					}
				}
				completion = ExecuteStatement(forStatement.Body);
				if (completion.Value.HasValue)
				{
					value = completion.Value.Value;
				}
				if (completion.Type == Completion.Break && (completion.Identifier == null || completion.Identifier == forStatement.LabelSet))
				{
					return new Completion(Completion.Normal, value, null);
				}
				if ((completion.Type != Completion.Continue || (completion.Identifier != null && completion.Identifier != forStatement.LabelSet)) && completion.Type != Completion.Normal)
				{
					break;
				}
				if (forStatement.Update != null)
				{
					object value3 = _engine.EvaluateExpression(forStatement.Update);
					_engine.GetValue(value3);
				}
			}
			return completion;
		}

		public Completion ExecuteForInStatement(ForInStatement forInStatement)
		{
			Identifier expression = ((forInStatement.Left.Type == SyntaxNodes.VariableDeclaration) ? forInStatement.Left.As<VariableDeclaration>().Declarations.First().Id : forInStatement.Left.As<Identifier>());
			Reference reference = _engine.EvaluateExpression(expression) as Reference;
			object value = _engine.EvaluateExpression(forInStatement.Right);
			JsValue value2 = _engine.GetValue(value);
			if (value2 == Undefined.Instance || value2 == Null.Instance)
			{
				return new Completion(Completion.Normal, null, null);
			}
			ObjectInstance objectInstance = TypeConverter.ToObject(_engine, value2);
			JsValue value3 = Null.Instance;
			ObjectInstance objectInstance2 = objectInstance;
			HashSet<string> hashSet = new HashSet<string>();
			while (objectInstance2 != null)
			{
				string[] array = objectInstance2.GetOwnProperties().Select(delegate(KeyValuePair<string, PropertyDescriptor> x)
				{
					KeyValuePair<string, PropertyDescriptor> keyValuePair = x;
					return keyValuePair.Key;
				}).ToArray();
				string[] array2 = array;
				string[] array3 = array2;
				foreach (string text in array3)
				{
					if (hashSet.Contains(text))
					{
						continue;
					}
					hashSet.Add(text);
					if (!objectInstance2.HasOwnProperty(text))
					{
						continue;
					}
					PropertyDescriptor ownProperty = objectInstance2.GetOwnProperty(text);
					if (ownProperty.Enumerable.HasValue && ownProperty.Enumerable.Value)
					{
						_engine.PutValue(reference, text);
						Completion completion = ExecuteStatement(forInStatement.Body);
						if (completion.Value.HasValue)
						{
							value3 = completion.Value.Value;
						}
						if (completion.Type == Completion.Break)
						{
							return new Completion(Completion.Normal, value3, null);
						}
						if (completion.Type != Completion.Continue && completion.Type != Completion.Normal)
						{
							return completion;
						}
					}
				}
				objectInstance2 = objectInstance2.Prototype;
			}
			return new Completion(Completion.Normal, value3, null);
		}

		public Completion ExecuteContinueStatement(ContinueStatement continueStatement)
		{
			return new Completion(Completion.Continue, null, (continueStatement.Label != null) ? continueStatement.Label.Name : null);
		}

		public Completion ExecuteBreakStatement(BreakStatement breakStatement)
		{
			return new Completion(Completion.Break, null, (breakStatement.Label != null) ? breakStatement.Label.Name : null);
		}

		public Completion ExecuteReturnStatement(ReturnStatement statement)
		{
			if (statement.Argument == null)
			{
				return new Completion(Completion.Return, Undefined.Instance, null);
			}
			object value = _engine.EvaluateExpression(statement.Argument);
			return new Completion(Completion.Return, _engine.GetValue(value), null);
		}

		public Completion ExecuteWithStatement(WithStatement withStatement)
		{
			object value = _engine.EvaluateExpression(withStatement.Object);
			ObjectInstance objectInstance = TypeConverter.ToObject(_engine, _engine.GetValue(value));
			LexicalEnvironment lexicalEnvironment = _engine.ExecutionContext.LexicalEnvironment;
			LexicalEnvironment lexicalEnvironment2 = LexicalEnvironment.NewObjectEnvironment(_engine, objectInstance, lexicalEnvironment, provideThis: true);
			_engine.ExecutionContext.LexicalEnvironment = lexicalEnvironment2;
			try
			{
				return ExecuteStatement(withStatement.Body);
			}
			catch (JavaScriptException ex)
			{
				Completion completion = new Completion(Completion.Throw, ex.Error, null);
				completion.Location = withStatement.Location;
				return completion;
			}
			finally
			{
				_engine.ExecutionContext.LexicalEnvironment = lexicalEnvironment;
			}
		}

		public Completion ExecuteSwitchStatement(SwitchStatement switchStatement)
		{
			object value = _engine.EvaluateExpression(switchStatement.Discriminant);
			Completion completion = ExecuteSwitchBlock(switchStatement.Cases, _engine.GetValue(value));
			if (completion.Type == Completion.Break && completion.Identifier == switchStatement.LabelSet)
			{
				return new Completion(Completion.Normal, completion.Value, null);
			}
			return completion;
		}

		public Completion ExecuteSwitchBlock(IEnumerable<SwitchCase> switchBlock, JsValue input)
		{
			JsValue value = Undefined.Instance;
			SwitchCase switchCase = null;
			bool flag = false;
			foreach (SwitchCase item in switchBlock)
			{
				if (item.Test == null)
				{
					switchCase = item;
				}
				else
				{
					JsValue value2 = _engine.GetValue(_engine.EvaluateExpression(item.Test));
					if (ExpressionInterpreter.StrictlyEqual(value2, input))
					{
						flag = true;
					}
				}
				if (flag && item.Consequent != null)
				{
					Completion completion = ExecuteStatementList(item.Consequent);
					if (completion.Type != Completion.Normal)
					{
						return completion;
					}
					value = (completion.Value.HasValue ? completion.Value.Value : Undefined.Instance);
				}
			}
			if (!flag && switchCase != null)
			{
				Completion completion2 = ExecuteStatementList(switchCase.Consequent);
				if (completion2.Type != Completion.Normal)
				{
					return completion2;
				}
				value = (completion2.Value.HasValue ? completion2.Value.Value : Undefined.Instance);
			}
			return new Completion(Completion.Normal, value, null);
		}

		public Completion ExecuteStatementList(IEnumerable<Statement> statementList)
		{
			Completion completion = new Completion(Completion.Normal, null, null);
			Completion completion2 = completion;
			Statement statement = null;
			try
			{
				foreach (Statement statement2 in statementList)
				{
					statement = statement2;
					completion = ExecuteStatement(statement2);
					if (completion.Type != Completion.Normal)
					{
						return new Completion(completion.Type, completion.Value.HasValue ? completion.Value : completion2.Value, completion.Identifier)
						{
							Location = completion.Location
						};
					}
					completion2 = completion;
				}
			}
			catch (JavaScriptException ex)
			{
				completion = new Completion(Completion.Throw, ex.Error, null);
				completion.Location = statement.Location;
				return completion;
			}
			return new Completion(completion.Type, completion.GetValueOrDefault(), completion.Identifier);
		}

		public Completion ExecuteThrowStatement(ThrowStatement throwStatement)
		{
			object value = _engine.EvaluateExpression(throwStatement.Argument);
			Completion completion = new Completion(Completion.Throw, _engine.GetValue(value), null);
			completion.Location = throwStatement.Location;
			return completion;
		}

		public Completion ExecuteTryStatement(TryStatement tryStatement)
		{
			Completion completion = ExecuteStatement(tryStatement.Block);
			if (completion.Type == Completion.Throw && tryStatement.Handlers.Any())
			{
				foreach (CatchClause handler in tryStatement.Handlers)
				{
					JsValue value = _engine.GetValue(completion);
					LexicalEnvironment lexicalEnvironment = _engine.ExecutionContext.LexicalEnvironment;
					LexicalEnvironment lexicalEnvironment2 = LexicalEnvironment.NewDeclarativeEnvironment(_engine, lexicalEnvironment);
					lexicalEnvironment2.Record.CreateMutableBinding(handler.Param.Name);
					lexicalEnvironment2.Record.SetMutableBinding(handler.Param.Name, value, strict: false);
					_engine.ExecutionContext.LexicalEnvironment = lexicalEnvironment2;
					completion = ExecuteStatement(handler.Body);
					_engine.ExecutionContext.LexicalEnvironment = lexicalEnvironment;
				}
			}
			if (tryStatement.Finalizer != null)
			{
				Completion completion2 = ExecuteStatement(tryStatement.Finalizer);
				if (completion2.Type == Completion.Normal)
				{
					return completion;
				}
				return completion2;
			}
			return completion;
		}

		public Completion ExecuteProgram(Program program)
		{
			return ExecuteStatementList(program.Body);
		}

		public Completion ExecuteVariableDeclaration(VariableDeclaration statement)
		{
			foreach (VariableDeclarator declaration in statement.Declarations)
			{
				if (declaration.Init != null)
				{
					if (!(_engine.EvaluateExpression(declaration.Id) is Reference reference))
					{
						throw new ArgumentException();
					}
					if (reference.IsStrict() && reference.GetBase().TryCast<EnvironmentRecord>() != null && (reference.GetReferencedName() == "eval" || reference.GetReferencedName() == "arguments"))
					{
						throw new JavaScriptException(_engine.SyntaxError);
					}
					reference.GetReferencedName();
					JsValue value = _engine.GetValue(_engine.EvaluateExpression(declaration.Init));
					_engine.PutValue(reference, value);
				}
			}
			return new Completion(Completion.Normal, Undefined.Instance, null);
		}

		public Completion ExecuteBlockStatement(BlockStatement blockStatement)
		{
			return ExecuteStatementList(blockStatement.Body);
		}

		public Completion ExecuteDebuggerStatement(DebuggerStatement debuggerStatement)
		{
			if (_engine.Options._IsDebuggerStatementAllowed)
			{
				if (!System.Diagnostics.Debugger.IsAttached)
				{
					System.Diagnostics.Debugger.Launch();
				}
				System.Diagnostics.Debugger.Break();
			}
			return new Completion(Completion.Normal, null, null);
		}
	}
}
