using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jint.Native.Object;
using Jint.Parser;
using Jint.Parser.Ast;
using Jint.Runtime;

namespace Jint.Native.Json
{
	public class JsonParser
	{
		private class Extra
		{
			public int? Loc;

			public int[] Range;

			public string Source;

			public List<Token> Tokens;
		}

		private enum Tokens
		{
			NullLiteral,
			BooleanLiteral,
			String,
			Number,
			Punctuator,
			EOF
		}

		private class Token
		{
			public Tokens Type;

			public object Value;

			public int[] Range;

			public int? LineNumber;

			public int LineStart;
		}

		private static class Messages
		{
			public const string UnexpectedToken = "Unexpected token {0}";

			public const string UnexpectedNumber = "Unexpected number";

			public const string UnexpectedString = "Unexpected string";

			public const string UnexpectedEOS = "Unexpected end of input";
		}

		private readonly Engine _engine;

		private Extra _extra;

		private int _index;

		private int _length;

		private int _lineNumber;

		private int _lineStart;

		private Location _location;

		private Token _lookahead;

		private string _source;

		private State _state;

		public JsonParser(Engine engine)
		{
			_engine = engine;
		}

		private static bool IsDecimalDigit(char ch)
		{
			if (ch >= '0')
			{
				return ch <= '9';
			}
			return false;
		}

		private static bool IsHexDigit(char ch)
		{
			if ((ch < '0' || ch > '9') && (ch < 'a' || ch > 'f'))
			{
				if (ch >= 'A')
				{
					return ch <= 'F';
				}
				return false;
			}
			return true;
		}

		private static bool IsOctalDigit(char ch)
		{
			if (ch >= '0')
			{
				return ch <= '7';
			}
			return false;
		}

		private static bool IsWhiteSpace(char ch)
		{
			if (ch != ' ' && ch != '\t' && ch != '\n')
			{
				return ch == '\r';
			}
			return true;
		}

		private static bool IsLineTerminator(char ch)
		{
			if (ch != '\n' && ch != '\r' && ch != '\u2028')
			{
				return ch == '\u2029';
			}
			return true;
		}

		private static bool IsNullChar(char ch)
		{
			if (ch != 'n' && ch != 'u' && ch != 'l')
			{
				return ch == 'l';
			}
			return true;
		}

		private static bool IsTrueOrFalseChar(char ch)
		{
			if (ch != 't' && ch != 'r' && ch != 'u' && ch != 'e' && ch != 'f' && ch != 'a' && ch != 'l')
			{
				return ch == 's';
			}
			return true;
		}

		private char ScanHexEscape(char prefix)
		{
			int num = 0;
			int num2 = ((prefix == 'u') ? 4 : 2);
			for (int i = 0; i < num2; i++)
			{
				if (_index < _length && IsHexDigit(_source.CharCodeAt(_index)))
				{
					num = num * 16 + "0123456789abcdef".IndexOf(_source.CharCodeAt(_index++).ToString(), StringComparison.OrdinalIgnoreCase);
					continue;
				}
				throw new JavaScriptException(_engine.SyntaxError, "Expected hexadecimal digit:" + _source);
			}
			return (char)num;
		}

		private void SkipWhiteSpace()
		{
			while (_index < _length)
			{
				char ch = _source.CharCodeAt(_index);
				if (IsWhiteSpace(ch))
				{
					_index++;
					continue;
				}
				break;
			}
		}

		private Token ScanPunctuator()
		{
			int index = _index;
			char c = _source.CharCodeAt(_index);
			switch (c)
			{
			case '(':
			case ')':
			case ',':
			case '.':
			case ':':
			case ';':
			case '?':
			case '[':
			case ']':
			case '{':
			case '}':
			case '~':
			{
				_index++;
				Token token = new Token();
				token.Type = Tokens.Punctuator;
				token.Value = c.ToString();
				token.LineNumber = _lineNumber;
				token.LineStart = _lineStart;
				token.Range = new int[2] { index, _index };
				return token;
			}
			default:
				throw new JavaScriptException(_engine.SyntaxError, $"Unexpected token {c}");
			}
		}

		private Token ScanNumericLiteral()
		{
			char c = _source.CharCodeAt(_index);
			int index = _index;
			string text = "";
			if (c == '-')
			{
				text += _source.CharCodeAt(_index++);
				c = _source.CharCodeAt(_index);
			}
			if (c != '.')
			{
				text += _source.CharCodeAt(_index++);
				c = _source.CharCodeAt(_index);
				if (text == "0" && c > '\0' && IsDecimalDigit(c))
				{
					throw new Exception("Unexpected token {0}");
				}
				while (IsDecimalDigit(_source.CharCodeAt(_index)))
				{
					text += _source.CharCodeAt(_index++);
				}
				c = _source.CharCodeAt(_index);
			}
			if (c == '.')
			{
				text += _source.CharCodeAt(_index++);
				while (IsDecimalDigit(_source.CharCodeAt(_index)))
				{
					text += _source.CharCodeAt(_index++);
				}
				c = _source.CharCodeAt(_index);
			}
			if (c == 'e' || c == 'E')
			{
				text += _source.CharCodeAt(_index++);
				c = _source.CharCodeAt(_index);
				if (c == '+' || c == '-')
				{
					text += _source.CharCodeAt(_index++);
				}
				if (!IsDecimalDigit(_source.CharCodeAt(_index)))
				{
					throw new Exception("Unexpected token {0}");
				}
				while (IsDecimalDigit(_source.CharCodeAt(_index)))
				{
					text += _source.CharCodeAt(_index++);
				}
			}
			Token token = new Token();
			token.Type = Tokens.Number;
			token.Value = double.Parse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
			token.LineNumber = _lineNumber;
			token.LineStart = _lineStart;
			token.Range = new int[2] { index, _index };
			return token;
		}

		private Token ScanBooleanLiteral()
		{
			int index = _index;
			string text = "";
			while (IsTrueOrFalseChar(_source.CharCodeAt(_index)))
			{
				text += _source.CharCodeAt(_index++);
			}
			if (text == "true" || text == "false")
			{
				Token token = new Token();
				token.Type = Tokens.BooleanLiteral;
				token.Value = text == "true";
				token.LineNumber = _lineNumber;
				token.LineStart = _lineStart;
				token.Range = new int[2] { index, _index };
				return token;
			}
			throw new JavaScriptException(_engine.SyntaxError, "Unexpected token " + text);
		}

		private Token ScanNullLiteral()
		{
			int index = _index;
			string text = "";
			while (IsNullChar(_source.CharCodeAt(_index)))
			{
				text += _source.CharCodeAt(_index++);
			}
			if (text == Null.Text)
			{
				Token token = new Token();
				token.Type = Tokens.NullLiteral;
				token.Value = Null.Instance;
				token.LineNumber = _lineNumber;
				token.LineStart = _lineStart;
				token.Range = new int[2] { index, _index };
				return token;
			}
			throw new JavaScriptException(_engine.SyntaxError, "Unexpected token " + text);
		}

		private Token ScanStringLiteral()
		{
			string text = "";
			char c = _source.CharCodeAt(_index);
			int index = _index;
			_index++;
			while (_index < _length)
			{
				char c2 = _source.CharCodeAt(_index++);
				if (c2 == c)
				{
					c = '\0';
					break;
				}
				if (c2 <= '\u001f')
				{
					throw new JavaScriptException(_engine.SyntaxError, $"Invalid character '{c2}', position:{_index}, string:{_source}");
				}
				if (c2 == '\\')
				{
					c2 = _source.CharCodeAt(_index++);
					if (c2 > '\0' || !IsLineTerminator(c2))
					{
						switch (c2)
						{
						case 'n':
							text += "\n";
							continue;
						case 'r':
							text += "\r";
							continue;
						case 't':
							text += "\t";
							continue;
						case 'u':
						case 'x':
						{
							int index2 = _index;
							char c3 = ScanHexEscape(c2);
							if (c3 > '\0')
							{
								text += c3;
								continue;
							}
							_index = index2;
							text += c2;
							continue;
						}
						case 'b':
							text += "\b";
							continue;
						case 'f':
							text += "\f";
							continue;
						case 'v':
							text += "\v";
							continue;
						}
						if (IsOctalDigit(c2))
						{
							int num = "01234567".IndexOf(c2);
							if (_index < _length && IsOctalDigit(_source.CharCodeAt(_index)))
							{
								num = num * 8 + "01234567".IndexOf(_source.CharCodeAt(_index++));
								if ("0123".IndexOf(c2) >= 0 && _index < _length && IsOctalDigit(_source.CharCodeAt(_index)))
								{
									num = num * 8 + "01234567".IndexOf(_source.CharCodeAt(_index++));
								}
							}
							text += (char)num;
						}
						else
						{
							text += c2;
						}
					}
					else
					{
						_lineNumber++;
						if (c2 == '\r' && _source.CharCodeAt(_index) == '\n')
						{
							_index++;
						}
					}
				}
				else
				{
					if (IsLineTerminator(c2))
					{
						break;
					}
					text += c2;
				}
			}
			if (c != 0)
			{
				throw new JavaScriptException(_engine.SyntaxError, "Unexpected token " + _source);
			}
			Token token = new Token();
			token.Type = Tokens.String;
			token.Value = text;
			token.LineNumber = _lineNumber;
			token.LineStart = _lineStart;
			token.Range = new int[2] { index, _index };
			return token;
		}

		private Token Advance()
		{
			SkipWhiteSpace();
			if (_index >= _length)
			{
				Token token = new Token();
				token.Type = Tokens.EOF;
				token.LineNumber = _lineNumber;
				token.LineStart = _lineStart;
				token.Range = new int[2] { _index, _index };
				return token;
			}
			char c = _source.CharCodeAt(_index);
			switch (c)
			{
			case '(':
			case ')':
			case ':':
				return ScanPunctuator();
			case '"':
				return ScanStringLiteral();
			case '.':
				if (IsDecimalDigit(_source.CharCodeAt(_index + 1)))
				{
					return ScanNumericLiteral();
				}
				return ScanPunctuator();
			case '-':
				if (IsDecimalDigit(_source.CharCodeAt(_index + 1)))
				{
					return ScanNumericLiteral();
				}
				return ScanPunctuator();
			default:
				if (IsDecimalDigit(c))
				{
					return ScanNumericLiteral();
				}
				switch (c)
				{
				case 'f':
				case 't':
					return ScanBooleanLiteral();
				case 'n':
					return ScanNullLiteral();
				default:
					return ScanPunctuator();
				}
			}
		}

		private Token CollectToken()
		{
			Location location = new Location();
			Position start = default(Position);
			start.Line = _lineNumber;
			start.Column = _index - _lineStart;
			Position position = (location.Start = start);
			_location = location;
			Token token = Advance();
			Location location2 = _location;
			start = default(Position);
			start.Line = _lineNumber;
			start.Column = _index - _lineStart;
			position = (location2.End = start);
			if (token.Type != Tokens.EOF)
			{
				int[] range = new int[2]
				{
					token.Range[0],
					token.Range[1]
				};
				string value = _source.Slice(token.Range[0], token.Range[1]);
				_extra.Tokens.Add(new Token
				{
					Type = token.Type,
					Value = value,
					Range = range
				});
			}
			return token;
		}

		private Token Lex()
		{
			Token lookahead = _lookahead;
			_index = lookahead.Range[1];
			_lineNumber = (lookahead.LineNumber.HasValue ? lookahead.LineNumber.Value : 0);
			_lineStart = lookahead.LineStart;
			_lookahead = ((_extra.Tokens != null) ? CollectToken() : Advance());
			_index = lookahead.Range[1];
			_lineNumber = (lookahead.LineNumber.HasValue ? lookahead.LineNumber.Value : 0);
			_lineStart = lookahead.LineStart;
			return lookahead;
		}

		private void Peek()
		{
			int index = _index;
			int lineNumber = _lineNumber;
			int lineStart = _lineStart;
			_lookahead = ((_extra.Tokens != null) ? CollectToken() : Advance());
			_index = index;
			_lineNumber = lineNumber;
			_lineStart = lineStart;
		}

		private void MarkStart()
		{
			if (_extra.Loc.HasValue)
			{
				_state.MarkerStack.Push(_index - _lineStart);
				_state.MarkerStack.Push(_lineNumber);
			}
			if (_extra.Range != null)
			{
				_state.MarkerStack.Push(_index);
			}
		}

		private T MarkEnd<T>(T node) where T : SyntaxNode
		{
			if (_extra.Range != null)
			{
				node.Range = new int[2]
				{
					_state.MarkerStack.Pop(),
					_index
				};
			}
			if (_extra.Loc.HasValue)
			{
				Location location = new Location();
				Position start = default(Position);
				start.Line = _state.MarkerStack.Pop();
				start.Column = _state.MarkerStack.Pop();
				Position position = (location.Start = start);
				start = default(Position);
				start.Line = _lineNumber;
				start.Column = _index - _lineStart;
				position = (location.End = start);
				node.Location = location;
				PostProcess(node);
			}
			return node;
		}

		public T MarkEndIf<T>(T node) where T : SyntaxNode
		{
			if (node.Range != null || node.Location != null)
			{
				if (_extra.Loc.HasValue)
				{
					_state.MarkerStack.Pop();
					_state.MarkerStack.Pop();
				}
				if (_extra.Range != null)
				{
					_state.MarkerStack.Pop();
				}
			}
			else
			{
				MarkEnd(node);
			}
			return node;
		}

		public SyntaxNode PostProcess(SyntaxNode node)
		{
			if (_extra.Source != null)
			{
				node.Location.Source = _extra.Source;
			}
			return node;
		}

		public ObjectInstance CreateArrayInstance(IEnumerable<JsValue> values)
		{
			ObjectInstance objectInstance = _engine.Array.Construct(Arguments.Empty);
			_engine.Array.PrototypeObject.Push(objectInstance, values.ToArray());
			return objectInstance;
		}

		private void ThrowError(Token token, string messageFormat, params object[] arguments)
		{
			string text = string.Format(messageFormat, arguments);
			object obj;
			if (token.LineNumber.HasValue)
			{
				int? lineNumber = token.LineNumber;
				obj = new ParserException("Line " + lineNumber + ": " + text)
				{
					Index = token.Range[0],
					LineNumber = token.LineNumber.Value,
					Column = token.Range[0] - _lineStart + 1
				};
			}
			else
			{
				obj = new ParserException("Line " + _lineNumber + ": " + text)
				{
					Index = _index,
					LineNumber = _lineNumber,
					Column = _index - _lineStart + 1
				};
			}
			ParserException ex = (ParserException)obj;
			ex.Description = text;
			throw ex;
		}

		private void ThrowUnexpected(Token token)
		{
			if (token.Type == Tokens.EOF)
			{
				ThrowError(token, "Unexpected end of input");
			}
			if (token.Type == Tokens.Number)
			{
				ThrowError(token, "Unexpected number");
			}
			if (token.Type == Tokens.String)
			{
				ThrowError(token, "Unexpected string");
			}
			ThrowError(token, "Unexpected token {0}", token.Value as string);
		}

		private void Expect(string value)
		{
			Token token = Lex();
			if (token.Type != Tokens.Punctuator || !value.Equals(token.Value))
			{
				ThrowUnexpected(token);
			}
		}

		private bool Match(string value)
		{
			if (_lookahead.Type == Tokens.Punctuator)
			{
				return value.Equals(_lookahead.Value);
			}
			return false;
		}

		private ObjectInstance ParseJsonArray()
		{
			List<JsValue> list = new List<JsValue>();
			Expect("[");
			while (!Match("]"))
			{
				if (Match(","))
				{
					Lex();
					list.Add(Null.Instance);
					continue;
				}
				list.Add(ParseJsonValue());
				if (!Match("]"))
				{
					Expect(",");
				}
			}
			Expect("]");
			return CreateArrayInstance(list);
		}

		public ObjectInstance ParseJsonObject()
		{
			Expect("{");
			ObjectInstance objectInstance = _engine.Object.Construct(Arguments.Empty);
			while (!Match("}"))
			{
				Tokens type = _lookahead.Type;
				if (type != Tokens.String)
				{
					ThrowUnexpected(Lex());
				}
				string text = Lex().Value.ToString();
				if (PropertyNameContainsInvalidChar0To31(text))
				{
					throw new JavaScriptException(_engine.SyntaxError, "Invalid character in property name '" + text + "'");
				}
				Expect(":");
				JsValue value = ParseJsonValue();
				objectInstance.FastAddProperty(text, value, writable: true, enumerable: true, configurable: true);
				if (!Match("}"))
				{
					Expect(",");
				}
			}
			Expect("}");
			return objectInstance;
		}

		private bool PropertyNameContainsInvalidChar0To31(string s)
		{
			foreach (int num in s)
			{
				if (num <= 31)
				{
					return true;
				}
			}
			return false;
		}

		private JsValue ParseJsonValue()
		{
			Tokens type = _lookahead.Type;
			MarkStart();
			switch (type)
			{
			case Tokens.NullLiteral:
			{
				object value = Lex().Value;
				return Null.Instance;
			}
			case Tokens.BooleanLiteral:
				return new JsValue((bool)Lex().Value);
			case Tokens.String:
				return new JsValue((string)Lex().Value);
			case Tokens.Number:
				return new JsValue((double)Lex().Value);
			default:
				if (Match("["))
				{
					return ParseJsonArray();
				}
				if (Match("{"))
				{
					return ParseJsonObject();
				}
				ThrowUnexpected(Lex());
				return Null.Instance;
			}
		}

		public JsValue Parse(string code)
		{
			return Parse(code, null);
		}

		public JsValue Parse(string code, ParserOptions options)
		{
			_source = code;
			_index = 0;
			_lineNumber = ((_source.Length > 0) ? 1 : 0);
			_lineStart = 0;
			_length = _source.Length;
			_lookahead = null;
			_state = new State
			{
				AllowIn = true,
				LabelSet = new HashSet<string>(),
				InFunctionBody = false,
				InIteration = false,
				InSwitch = false,
				LastCommentStart = -1,
				MarkerStack = new Stack<int>()
			};
			_extra = new Extra
			{
				Range = new int[0],
				Loc = 0
			};
			if (options != null)
			{
				if (!string.IsNullOrEmpty(options.Source))
				{
					_extra.Source = options.Source;
				}
				if (options.Tokens)
				{
					_extra.Tokens = new List<Token>();
				}
			}
			try
			{
				MarkStart();
				Peek();
				JsValue result = ParseJsonValue();
				Peek();
				Tokens type = _lookahead.Type;
				object value = _lookahead.Value;
				if (_lookahead.Type != Tokens.EOF)
				{
					throw new JavaScriptException(_engine.SyntaxError, $"Unexpected {_lookahead.Type} {_lookahead.Value}");
				}
				return result;
			}
			finally
			{
				_extra = new Extra();
			}
		}
	}
}
