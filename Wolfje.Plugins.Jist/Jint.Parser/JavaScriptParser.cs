using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Jint.Native.String;
using Jint.Parser.Ast;

namespace Jint.Parser
{
	public class JavaScriptParser
	{
		private class Extra
		{
			public int? Loc;

			public int[] Range;

			public string Source;

			public List<Comment> Comments;

			public List<Token> Tokens;

			public List<ParserException> Errors;
		}

		private class LocationMarker
		{
			private readonly int[] _marker;

			public LocationMarker(int index, int lineNumber, int lineStart)
			{
				_marker = new int[6]
				{
					index,
					lineNumber,
					index - lineStart,
					0,
					0,
					0
				};
			}

			public void End(int index, int lineNumber, int lineStart)
			{
				_marker[3] = index;
				_marker[4] = lineNumber;
				_marker[5] = index - lineStart;
			}

			public void Apply(SyntaxNode node, Extra extra, Func<SyntaxNode, SyntaxNode> postProcess)
			{
				if (extra.Range.Length != 0)
				{
					node.Range = new int[2]
					{
						_marker[0],
						_marker[3]
					};
				}
				if (extra.Loc.HasValue)
				{
					SyntaxNode syntaxNode = node;
					Location location = new Location();
					Position start = default(Position);
					start.Line = _marker[1];
					start.Column = _marker[2];
					Position position = (location.Start = start);
					start = default(Position);
					start.Line = _marker[4];
					start.Column = _marker[5];
					position = (location.End = start);
					syntaxNode.Location = location;
				}
				node = postProcess(node);
			}
		}

		private struct ParsedParameters
		{
			public Token FirstRestricted;

			public string Message;

			public IEnumerable<Identifier> Parameters;

			public Token Stricted;
		}

		private static class Regexes
		{
			public static readonly Regex NonAsciiIdentifierStart = new Regex("[ªµºÀ-ÖØ-öø-ˁˆ-ˑˠ-ˤˬˮͰ-ʹͶͷͺ-ͽΆΈ-ΊΌΎ-ΡΣ-ϵϷ-ҁҊ-ԧԱ-Ֆՙա-ևא-תװ-ײؠ-يٮٯٱ-ۓەۥۦۮۯۺ-ۼۿܐܒ-ܯݍ-ޥޱߊ-ߪߴߵߺࠀ-ࠕࠚࠤࠨࡀ-ࡘࢠࢢ-ࢬऄ-हऽॐक़-ॡॱ-ॷॹ-ॿঅ-ঌএঐও-নপ-রলশ-হঽৎড়ঢ়য়-ৡৰৱਅ-ਊਏਐਓ-ਨਪ-ਰਲਲ਼ਵਸ਼ਸਹਖ਼-ੜਫ਼ੲ-ੴઅ-ઍએ-ઑઓ-નપ-રલળવ-હઽૐૠૡଅ-ଌଏଐଓ-ନପ-ରଲଳଵ-ହଽଡ଼ଢ଼ୟ-ୡୱஃஅ-ஊஎ-ஐஒ-கஙசஜஞடணதந-பம-ஹௐఅ-ఌఎ-ఐఒ-నప-ళవ-హఽౘౙౠౡಅ-ಌಎ-ಐಒ-ನಪ-ಳವ-ಹಽೞೠೡೱೲഅ-ഌഎ-ഐഒ-ഺഽൎൠൡൺ-ൿඅ-ඖක-නඳ-රලව-ෆก-ะาำเ-ๆກຂຄງຈຊຍດ-ທນ-ຟມ-ຣລວສຫອ-ະາຳຽເ-ໄໆໜ-ໟༀཀ-ཇཉ-ཬྈ-ྌက-ဪဿၐ-ၕၚ-ၝၡၥၦၮ-ၰၵ-ႁႎႠ-ჅჇჍა-ჺჼ-ቈቊ-ቍቐ-ቖቘቚ-ቝበ-ኈኊ-ኍነ-ኰኲ-ኵኸ-ኾዀዂ-ዅወ-ዖዘ-ጐጒ-ጕጘ-ፚᎀ-ᎏᎠ-Ᏼᐁ-ᙬᙯ-ᙿᚁ-ᚚᚠ-ᛪᛮ-ᛰᜀ-ᜌᜎ-ᜑᜠ-ᜱᝀ-ᝑᝠ-ᝬᝮ-ᝰក-ឳៗៜᠠ-ᡷᢀ-ᢨᢪᢰ-ᣵᤀ-ᤜᥐ-ᥭᥰ-ᥴᦀ-ᦫᧁ-ᧇᨀ-ᨖᨠ-ᩔᪧᬅ-ᬳᭅ-ᭋᮃ-ᮠᮮᮯᮺ-ᯥᰀ-ᰣᱍ-ᱏᱚ-ᱽᳩ-ᳬᳮ-ᳱᳵᳶᴀ-ᶿḀ-ἕἘ-Ἕἠ-ὅὈ-Ὅὐ-ὗὙὛὝὟ-ώᾀ-ᾴᾶ-ᾼιῂ-ῄῆ-ῌῐ-ΐῖ-Ίῠ-Ῥῲ-ῴῶ-ῼⁱⁿₐ-ₜℂℇℊ-ℓℕℙ-ℝℤΩℨK-ℭℯ-ℹℼ-ℿⅅ-ⅉⅎⅠ-ↈⰀ-Ⱞⰰ-ⱞⱠ-ⳤⳫ-ⳮⳲⳳⴀ-ⴥⴧⴭⴰ-ⵧⵯⶀ-ⶖⶠ-ⶦⶨ-ⶮⶰ-ⶶⶸ-ⶾⷀ-ⷆⷈ-ⷎⷐ-ⷖⷘ-ⷞⸯ々-〇〡-〩〱-〵〸-〼ぁ-ゖゝ-ゟァ-ヺー-ヿㄅ-ㄭㄱ-ㆎㆠ-ㆺㇰ-ㇿ㐀-䶵一-鿌ꀀ-ꒌꓐ-ꓽꔀ-ꘌꘐ-ꘟꘪꘫꙀ-ꙮꙿ-ꚗꚠ-ꛯꜗ-ꜟꜢ-ꞈꞋ-ꞎꞐ-ꞓꞠ-Ɦꟸ-ꠁꠃ-ꠅꠇ-ꠊꠌ-ꠢꡀ-ꡳꢂ-ꢳꣲ-ꣷꣻꤊ-ꤥꤰ-ꥆꥠ-ꥼꦄ-ꦲꧏꨀ-ꨨꩀ-ꩂꩄ-ꩋꩠ-ꩶꩺꪀ-ꪯꪱꪵꪶꪹ-ꪽꫀꫂꫛ-ꫝꫠ-ꫪꫲ-ꫴꬁ-ꬆꬉ-ꬎꬑ-ꬖꬠ-ꬦꬨ-ꬮꯀ-ꯢ가-힣ힰ-ퟆퟋ-ퟻ豈-舘並-龎ﬀ-ﬆﬓ-ﬗיִײַ-ﬨשׁ-זּטּ-לּמּנּסּףּפּצּ-ﮱﯓ-ﴽﵐ-ﶏﶒ-ﷇﷰ-ﷻﹰ-ﹴﹶ-ﻼＡ-Ｚａ-ｚｦ-ﾾￂ-ￇￊ-ￏￒ-ￗￚ-ￜ]");

			public static readonly Regex NonAsciiIdentifierPart = new Regex("[ªµºÀ-ÖØ-öø-ˁˆ-ˑˠ-ˤˬˮ\u0300-ʹͶͷͺ-ͽΆΈ-ΊΌΎ-ΡΣ-ϵϷ-ҁ\u0483-\u0487Ҋ-ԧԱ-Ֆՙա-և\u0591-\u05bd\u05bf\u05c1\u05c2\u05c4\u05c5\u05c7א-תװ-ײ\u0610-\u061aؠ-٩ٮ-ۓە-\u06dc\u06df-\u06e8\u06ea-ۼۿܐ-\u074aݍ-ޱ߀-ߵߺࠀ-\u082dࡀ-\u085bࢠࢢ-ࢬ\u08e4-\u08fe\u0900-\u0963०-९ॱ-ॷॹ-ॿ\u0981-\u0983অ-ঌএঐও-নপ-রলশ-হ\u09bc-\u09c4\u09c7\u09c8\u09cb-ৎ\u09d7ড়ঢ়য়-\u09e3০-ৱ\u0a01-\u0a03ਅ-ਊਏਐਓ-ਨਪ-ਰਲਲ਼ਵਸ਼ਸਹ\u0a3c\u0a3e-\u0a42\u0a47\u0a48\u0a4b-\u0a4d\u0a51ਖ਼-ੜਫ਼੦-\u0a75\u0a81-\u0a83અ-ઍએ-ઑઓ-નપ-રલળવ-હ\u0abc-\u0ac5\u0ac7-\u0ac9\u0acb-\u0acdૐૠ-\u0ae3૦-૯\u0b01-\u0b03ଅ-ଌଏଐଓ-ନପ-ରଲଳଵ-ହ\u0b3c-\u0b44\u0b47\u0b48\u0b4b-\u0b4d\u0b56\u0b57ଡ଼ଢ଼ୟ-\u0b63୦-୯ୱ\u0b82ஃஅ-ஊஎ-ஐஒ-கஙசஜஞடணதந-பம-ஹ\u0bbe-\u0bc2\u0bc6-\u0bc8\u0bca-\u0bcdௐ\u0bd7௦-௯\u0c01-\u0c03అ-ఌఎ-ఐఒ-నప-ళవ-హఽ-\u0c44\u0c46-\u0c48\u0c4a-\u0c4d\u0c55\u0c56ౘౙౠ-\u0c63౦-౯\u0c82\u0c83ಅ-ಌಎ-ಐಒ-ನಪ-ಳವ-ಹ\u0cbc-\u0cc4\u0cc6-\u0cc8\u0cca-\u0ccd\u0cd5\u0cd6ೞೠ-\u0ce3೦-೯ೱೲ\u0d02\u0d03അ-ഌഎ-ഐഒ-ഺഽ-\u0d44\u0d46-\u0d48\u0d4a-ൎ\u0d57ൠ-\u0d63൦-൯ൺ-ൿ\u0d82\u0d83අ-ඖක-නඳ-රලව-ෆ\u0dca\u0dcf-\u0dd4\u0dd6\u0dd8-\u0ddf\u0df2\u0df3ก-\u0e3aเ-\u0e4e๐-๙ກຂຄງຈຊຍດ-ທນ-ຟມ-ຣລວສຫອ-\u0eb9\u0ebb-ຽເ-ໄໆ\u0ec8-\u0ecd໐-໙ໜ-ໟༀ\u0f18\u0f19༠-༩\u0f35\u0f37\u0f39\u0f3e-ཇཉ-ཬ\u0f71-\u0f84\u0f86-\u0f97\u0f99-\u0fbc\u0fc6က-၉ၐ-\u109dႠ-ჅჇჍა-ჺჼ-ቈቊ-ቍቐ-ቖቘቚ-ቝበ-ኈኊ-ኍነ-ኰኲ-ኵኸ-ኾዀዂ-ዅወ-ዖዘ-ጐጒ-ጕጘ-ፚ\u135d-\u135fᎀ-ᎏᎠ-Ᏼᐁ-ᙬᙯ-ᙿᚁ-ᚚᚠ-ᛪᛮ-ᛰᜀ-ᜌᜎ-\u1714ᜠ-\u1734ᝀ-\u1753ᝠ-ᝬᝮ-ᝰ\u1772\u1773ក-\u17d3ៗៜ\u17dd០-៩\u180b-\u180d᠐-᠙ᠠ-ᡷᢀ-ᢪᢰ-ᣵᤀ-ᤜ\u1920-\u192b\u1930-\u193b᥆-ᥭᥰ-ᥴᦀ-ᦫᦰ-ᧉ᧐-᧙ᨀ-\u1a1bᨠ-\u1a5e\u1a60-\u1a7c\u1a7f-᪉᪐-᪙ᪧ\u1b00-ᭋ᭐-᭙\u1b6b-\u1b73\u1b80-\u1bf3ᰀ-\u1c37᱀-᱉ᱍ-ᱽ\u1cd0-\u1cd2\u1cd4-ᳶᴀ-\u1de6\u1dfc-ἕἘ-Ἕἠ-ὅὈ-Ὅὐ-ὗὙὛὝὟ-ώᾀ-ᾴᾶ-ᾼιῂ-ῄῆ-ῌῐ-ΐῖ-Ίῠ-Ῥῲ-ῴῶ-ῼ\u200c\u200d\u203f\u2040\u2054ⁱⁿₐ-ₜ\u20d0-\u20dc\u20e1\u20e5-\u20f0ℂℇℊ-ℓℕℙ-ℝℤΩℨK-ℭℯ-ℹℼ-ℿⅅ-ⅉⅎⅠ-ↈⰀ-Ⱞⰰ-ⱞⱠ-ⳤⳫ-ⳳⴀ-ⴥⴧⴭⴰ-ⵧⵯ\u2d7f-ⶖⶠ-ⶦⶨ-ⶮⶰ-ⶶⶸ-ⶾⷀ-ⷆⷈ-ⷎⷐ-ⷖⷘ-ⷞ\u2de0-\u2dffⸯ々-〇〡-\u302f〱-〵〸-〼ぁ-ゖ\u3099\u309aゝ-ゟァ-ヺー-ヿㄅ-ㄭㄱ-ㆎㆠ-ㆺㇰ-ㇿ㐀-䶵一-鿌ꀀ-ꒌꓐ-ꓽꔀ-ꘌꘐ-ꘫꙀ-\ua66f\ua674-\ua67dꙿ-ꚗ\ua69f-\ua6f1ꜗ-ꜟꜢ-ꞈꞋ-ꞎꞐ-ꞓꞠ-Ɦꟸ-\ua827ꡀ-ꡳ\ua880-\ua8c4꣐-꣙\ua8e0-ꣷꣻ꤀-\ua92dꤰ-\ua953ꥠ-ꥼ\ua980-\ua9c0ꧏ-꧙ꨀ-\uaa36ꩀ-\uaa4d꩐-꩙ꩠ-ꩶꩺ\uaa7bꪀ-ꫂꫛ-ꫝꫠ-\uaaefꫲ-\uaaf6ꬁ-ꬆꬉ-ꬎꬑ-ꬖꬠ-ꬦꬨ-ꬮꯀ-\uabea\uabec\uabed꯰-꯹가-힣ힰ-ퟆퟋ-ퟻ豈-舘並-龎ﬀ-ﬆﬓ-ﬗיִ-ﬨשׁ-זּטּ-לּמּנּסּףּפּצּ-ﮱﯓ-ﴽﵐ-ﶏﶒ-ﷇﷰ-ﷻ\ufe00-\ufe0f\ufe20-\ufe26\ufe33\ufe34\ufe4d-\ufe4fﹰ-ﹴﹶ-ﻼ０-９Ａ-Ｚ\uff3fａ-ｚｦ-ﾾￂ-ￇￊ-ￏￒ-ￗￚ-ￜ]");
		}

		private static readonly HashSet<string> Keywords = new HashSet<string>
		{
			"if", "in", "do", "var", "for", "new", "try", "let", "this", "else",
			"case", "void", "with", "enum", "while", "break", "catch", "throw", "const", "yield",
			"class", "super", "return", "typeof", "delete", "switch", "export", "import", "default", "finally",
			"extends", "function", "continue", "debugger", "instanceof"
		};

		private static readonly HashSet<string> StrictModeReservedWords = new HashSet<string> { "implements", "interface", "package", "private", "protected", "public", "static", "yield", "let" };

		private static readonly HashSet<string> FutureReservedWords = new HashSet<string> { "class", "enum", "export", "extends", "import", "super" };

		private Extra _extra;

		private int _index;

		private int _length;

		private int _lineNumber;

		private int _lineStart;

		private Location _location;

		private Token _lookahead;

		private string _source;

		private State _state;

		private bool _strict;

		private readonly Stack<IVariableScope> _variableScopes = new Stack<IVariableScope>();

		private readonly Stack<IFunctionScope> _functionScopes = new Stack<IFunctionScope>();

		public JavaScriptParser()
		{
		}

		public JavaScriptParser(bool strict)
		{
			_strict = strict;
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
			if (ch != ' ' && ch != '\t' && ch != '\v' && ch != '\f' && ch != '\u00a0')
			{
				if (ch >= '\u1680')
				{
					switch (ch)
					{
					default:
						if (ch != '\u202f' && ch != '\u205f' && ch != '\u3000')
						{
							return ch == '\ufeff';
						}
						break;
					case '\u1680':
					case '\u180e':
					case '\u2000':
					case '\u2001':
					case '\u2002':
					case '\u2003':
					case '\u2004':
					case '\u2005':
					case '\u2006':
					case '\u2007':
					case '\u2008':
					case '\u2009':
					case '\u200a':
						break;
					}
					return true;
				}
				return false;
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

		private static bool IsIdentifierStart(char ch)
		{
			switch (ch)
			{
			default:
				if ((ch < 'a' || ch > 'z') && ch != '\\')
				{
					if (ch >= '\u0080')
					{
						return Regexes.NonAsciiIdentifierStart.IsMatch(ch.ToString());
					}
					return false;
				}
				break;
			case '$':
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
			case 'H':
			case 'I':
			case 'J':
			case 'K':
			case 'L':
			case 'M':
			case 'N':
			case 'O':
			case 'P':
			case 'Q':
			case 'R':
			case 'S':
			case 'T':
			case 'U':
			case 'V':
			case 'W':
			case 'X':
			case 'Y':
			case 'Z':
			case '_':
				break;
			}
			return true;
		}

		private static bool IsIdentifierPart(char ch)
		{
			switch (ch)
			{
			default:
				if ((ch < 'a' || ch > 'z') && (ch < '0' || ch > '9') && ch != '\\')
				{
					if (ch >= '\u0080')
					{
						return Regexes.NonAsciiIdentifierPart.IsMatch(ch.ToString());
					}
					return false;
				}
				break;
			case '$':
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
			case 'H':
			case 'I':
			case 'J':
			case 'K':
			case 'L':
			case 'M':
			case 'N':
			case 'O':
			case 'P':
			case 'Q':
			case 'R':
			case 'S':
			case 'T':
			case 'U':
			case 'V':
			case 'W':
			case 'X':
			case 'Y':
			case 'Z':
			case '_':
				break;
			}
			return true;
		}

		private static bool IsFutureReservedWord(string id)
		{
			return FutureReservedWords.Contains(id);
		}

		private static bool IsStrictModeReservedWord(string id)
		{
			return StrictModeReservedWords.Contains(id);
		}

		private static bool IsRestrictedWord(string id)
		{
			if (!"eval".Equals(id))
			{
				return "arguments".Equals(id);
			}
			return true;
		}

		private bool IsKeyword(string id)
		{
			if (_strict && IsStrictModeReservedWord(id))
			{
				return true;
			}
			return Keywords.Contains(id);
		}

		private void AddComment(string type, string value, int start, int end, Location location)
		{
			if (_state.LastCommentStart < start)
			{
				_state.LastCommentStart = start;
				Comment comment = new Comment
				{
					Type = type,
					Value = value
				};
				if (_extra.Range != null)
				{
					comment.Range = new int[2] { start, end };
				}
				if (_extra.Loc.HasValue)
				{
					comment.Location = location;
				}
				_extra.Comments.Add(comment);
			}
		}

		private void SkipSingleLineComment(int offset)
		{
			int num = _index - offset;
			Location location = new Location();
			Position start = default(Position);
			start.Line = _lineNumber;
			start.Column = _index - _lineStart - offset;
			Position position = (location.Start = start);
			_location = location;
			while (_index < _length)
			{
				char c = _source.CharCodeAt(_index);
				_index++;
				if (IsLineTerminator(c))
				{
					if (_extra.Comments != null)
					{
						string value = _source.Slice(num + 2, _index - 1);
						Location location2 = _location;
						start = default(Position);
						start.Line = _lineNumber;
						start.Column = _index - _lineStart - 1;
						position = (location2.End = start);
						AddComment("Line", value, num, _index - 1, _location);
					}
					if (c == '\r' && _source.CharCodeAt(_index) == '\n')
					{
						_index++;
					}
					_lineNumber++;
					_lineStart = _index;
					return;
				}
			}
			if (_extra.Comments != null)
			{
				string value2 = _source.Slice(num + offset, _index);
				Location location3 = _location;
				start = default(Position);
				start.Line = _lineNumber;
				start.Column = _index - _lineStart;
				position = (location3.End = start);
				AddComment("Line", value2, num, _index, _location);
			}
		}

		private void SkipMultiLineComment()
		{
			int num = 0;
			if (_extra.Comments != null)
			{
				num = _index - 2;
				Location location = new Location();
				Position start = default(Position);
				start.Line = _lineNumber;
				start.Column = _index - _lineStart - 2;
				Position position = (location.Start = start);
				_location = location;
			}
			while (_index < _length)
			{
				char c = _source.CharCodeAt(_index);
				if (IsLineTerminator(c))
				{
					if (c == '\r' && _source.CharCodeAt(_index + 1) == '\n')
					{
						_index++;
					}
					_lineNumber++;
					_index++;
					_lineStart = _index;
					if (_index >= _length)
					{
						ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
					}
				}
				else if (c == '*')
				{
					if (_source.CharCodeAt(_index + 1) == '/')
					{
						_index++;
						_index++;
						if (_extra.Comments != null)
						{
							string value = _source.Slice(num + 2, _index - 2);
							Location location2 = _location;
							Position start = default(Position);
							start.Line = _lineNumber;
							start.Column = _index - _lineStart;
							Position position = (location2.End = start);
							AddComment("Block", value, num, _index, _location);
						}
						return;
					}
					_index++;
				}
				else
				{
					_index++;
				}
			}
			ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
		}

		private void SkipComment()
		{
			bool flag = _index == 0;
			while (_index < _length)
			{
				char c = _source.CharCodeAt(_index);
				if (IsWhiteSpace(c))
				{
					_index++;
				}
				else if (IsLineTerminator(c))
				{
					_index++;
					if (c == '\r' && _source.CharCodeAt(_index) == '\n')
					{
						_index++;
					}
					_lineNumber++;
					_lineStart = _index;
					flag = true;
				}
				else if (c == '/')
				{
					switch (_source.CharCodeAt(_index + 1))
					{
					default:
						return;
					case '/':
						_index++;
						_index++;
						SkipSingleLineComment(2);
						flag = true;
						break;
					case '*':
						_index++;
						_index++;
						SkipMultiLineComment();
						break;
					}
				}
				else if (flag && c == '-')
				{
					if (_source.CharCodeAt(_index + 1) != '-' || _source.CharCodeAt(_index + 2) != '>')
					{
						break;
					}
					_index += 3;
					SkipSingleLineComment(3);
				}
				else
				{
					if (c != '<' || !(_source.Slice(_index + 1, _index + 4) == "!--"))
					{
						break;
					}
					_index++;
					_index++;
					_index++;
					_index++;
					SkipSingleLineComment(4);
				}
			}
		}

		private bool ScanHexEscape(char prefix, out char result)
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
				result = '\0';
				return false;
			}
			result = (char)num;
			return true;
		}

		private string GetEscapedIdentifier()
		{
			char result = _source.CharCodeAt(_index++);
			StringBuilder stringBuilder = new StringBuilder(result.ToString());
			if (result == '\\')
			{
				if (_source.CharCodeAt(_index) != 'u')
				{
					ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
				}
				_index++;
				if (!ScanHexEscape('u', out result) || result == '\\' || !IsIdentifierStart(result))
				{
					ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
				}
				stringBuilder = new StringBuilder(result.ToString());
			}
			while (_index < _length)
			{
				result = _source.CharCodeAt(_index);
				if (!IsIdentifierPart(result))
				{
					break;
				}
				_index++;
				if (result == '\\')
				{
					if (_source.CharCodeAt(_index) != 'u')
					{
						ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
					}
					_index++;
					if (!ScanHexEscape('u', out result) || result == '\\' || !IsIdentifierPart(result))
					{
						ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
					}
					stringBuilder.Append(result);
				}
				else
				{
					stringBuilder.Append(result);
				}
			}
			return stringBuilder.ToString();
		}

		private string GetIdentifier()
		{
			int num = _index++;
			while (_index < _length)
			{
				char c = _source.CharCodeAt(_index);
				if (c == '\\')
				{
					_index = num;
					return GetEscapedIdentifier();
				}
				if (!IsIdentifierPart(c))
				{
					break;
				}
				_index++;
			}
			return _source.Slice(num, _index);
		}

		private Token ScanIdentifier()
		{
			int index = _index;
			string text = ((_source.CharCodeAt(_index) == '\\') ? GetEscapedIdentifier() : GetIdentifier());
			Tokens type = ((text.Length == 1) ? Tokens.Identifier : (IsKeyword(text) ? Tokens.Keyword : ("null".Equals(text) ? Tokens.NullLiteral : (("true".Equals(text) || "false".Equals(text)) ? Tokens.BooleanLiteral : Tokens.Identifier))));
			Token token = new Token();
			token.Type = type;
			token.Value = text;
			token.LineNumber = _lineNumber;
			token.LineStart = _lineStart;
			token.Range = new int[2] { index, _index };
			return token;
		}

		private Token ScanPunctuator()
		{
			int index = _index;
			char c = _source.CharCodeAt(_index);
			char c2 = _source.CharCodeAt(_index);
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
				Token token9 = new Token();
				token9.Type = Tokens.Punctuator;
				token9.Value = c.ToString();
				token9.LineNumber = _lineNumber;
				token9.LineStart = _lineStart;
				token9.Range = new int[2] { index, _index };
				return token9;
			}
			default:
			{
				char c3 = _source.CharCodeAt(_index + 1);
				if (c3 == '=')
				{
					switch (c)
					{
					case '%':
					case '&':
					case '*':
					case '+':
					case '-':
					case '/':
					case '<':
					case '>':
					case '^':
					case '|':
					{
						_index += 2;
						Token token2 = new Token();
						token2.Type = Tokens.Punctuator;
						token2.Value = c.ToString() + c3;
						token2.LineNumber = _lineNumber;
						token2.LineStart = _lineStart;
						token2.Range = new int[2] { index, _index };
						return token2;
					}
					case '!':
					case '=':
					{
						_index += 2;
						if (_source.CharCodeAt(_index) == '=')
						{
							_index++;
						}
						Token token = new Token();
						token.Type = Tokens.Punctuator;
						token.Value = _source.Slice(index, _index);
						token.LineNumber = _lineNumber;
						token.LineStart = _lineStart;
						token.Range = new int[2] { index, _index };
						return token;
					}
					}
				}
				char c4 = _source.CharCodeAt(_index + 1);
				char c5 = _source.CharCodeAt(_index + 2);
				char c6 = _source.CharCodeAt(_index + 3);
				if (c2 == '>' && c4 == '>' && c5 == '>' && c6 == '=')
				{
					_index += 4;
					Token token3 = new Token();
					token3.Type = Tokens.Punctuator;
					token3.Value = ">>>=";
					token3.LineNumber = _lineNumber;
					token3.LineStart = _lineStart;
					token3.Range = new int[2] { index, _index };
					return token3;
				}
				if (c2 == '>' && c4 == '>' && c5 == '>')
				{
					_index += 3;
					Token token4 = new Token();
					token4.Type = Tokens.Punctuator;
					token4.Value = ">>>";
					token4.LineNumber = _lineNumber;
					token4.LineStart = _lineStart;
					token4.Range = new int[2] { index, _index };
					return token4;
				}
				if (c2 == '<' && c4 == '<' && c5 == '=')
				{
					_index += 3;
					Token token5 = new Token();
					token5.Type = Tokens.Punctuator;
					token5.Value = "<<=";
					token5.LineNumber = _lineNumber;
					token5.LineStart = _lineStart;
					token5.Range = new int[2] { index, _index };
					return token5;
				}
				if (c2 == '>' && c4 == '>' && c5 == '=')
				{
					_index += 3;
					Token token6 = new Token();
					token6.Type = Tokens.Punctuator;
					token6.Value = ">>=";
					token6.LineNumber = _lineNumber;
					token6.LineStart = _lineStart;
					token6.Range = new int[2] { index, _index };
					return token6;
				}
				if (c2 == c4 && "+-<>&|".IndexOf(c2) >= 0)
				{
					_index += 2;
					Token token7 = new Token();
					token7.Type = Tokens.Punctuator;
					token7.Value = c2.ToString() + c4;
					token7.LineNumber = _lineNumber;
					token7.LineStart = _lineStart;
					token7.Range = new int[2] { index, _index };
					return token7;
				}
				if ("<>=!+-*%&|^/".IndexOf(c2) >= 0)
				{
					_index++;
					Token token8 = new Token();
					token8.Type = Tokens.Punctuator;
					token8.Value = c2.ToString();
					token8.LineNumber = _lineNumber;
					token8.LineStart = _lineStart;
					token8.Range = new int[2] { index, _index };
					return token8;
				}
				ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
				return null;
			}
			}
		}

		private Token ScanHexLiteral(int start)
		{
			string text = "";
			while (_index < _length && IsHexDigit(_source.CharCodeAt(_index)))
			{
				text += _source.CharCodeAt(_index++);
			}
			if (text.Length == 0)
			{
				ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
			}
			if (IsIdentifierStart(_source.CharCodeAt(_index)))
			{
				ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
			}
			Token token = new Token();
			token.Type = Tokens.NumericLiteral;
			token.Value = Convert.ToInt64(text, 16);
			token.LineNumber = _lineNumber;
			token.LineStart = _lineStart;
			token.Range = new int[2] { start, _index };
			return token;
		}

		private Token ScanOctalLiteral(int start)
		{
			string text = "0" + _source.CharCodeAt(_index++);
			while (_index < _length && IsOctalDigit(_source.CharCodeAt(_index)))
			{
				text += _source.CharCodeAt(_index++);
			}
			if (IsIdentifierStart(_source.CharCodeAt(_index)) || IsDecimalDigit(_source.CharCodeAt(_index)))
			{
				ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
			}
			Token token = new Token();
			token.Type = Tokens.NumericLiteral;
			token.Value = Convert.ToInt32(text, 8);
			token.Octal = true;
			token.LineNumber = _lineNumber;
			token.LineStart = _lineStart;
			token.Range = new int[2] { start, _index };
			return token;
		}

		private Token ScanNumericLiteral()
		{
			char c = _source.CharCodeAt(_index);
			int index = _index;
			string text = "";
			if (c != '.')
			{
				text = _source.CharCodeAt(_index++).ToString();
				c = _source.CharCodeAt(_index);
				if (text == "0")
				{
					if (c == 'x' || c == 'X')
					{
						_index++;
						return ScanHexLiteral(index);
					}
					if (IsOctalDigit(c))
					{
						return ScanOctalLiteral(index);
					}
					if (c > '\0' && IsDecimalDigit(c))
					{
						ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
					}
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
				if (IsDecimalDigit(_source.CharCodeAt(_index)))
				{
					while (IsDecimalDigit(_source.CharCodeAt(_index)))
					{
						text += _source.CharCodeAt(_index++);
					}
				}
				else
				{
					ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
				}
			}
			if (IsIdentifierStart(_source.CharCodeAt(_index)))
			{
				ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
			}
			double num;
			try
			{
				num = double.Parse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
				if (num > double.MaxValue)
				{
					num = double.PositiveInfinity;
				}
				else if (num < double.MinValue)
				{
					num = double.NegativeInfinity;
				}
			}
			catch (OverflowException)
			{
				num = (StringPrototype.TrimEx(text).StartsWith("-") ? double.NegativeInfinity : double.PositiveInfinity);
			}
			catch (Exception)
			{
				num = double.NaN;
			}
			Token token = new Token();
			token.Type = Tokens.NumericLiteral;
			token.Value = num;
			token.LineNumber = _lineNumber;
			token.LineStart = _lineStart;
			token.Range = new int[2] { index, _index };
			return token;
		}

		private Token ScanStringLiteral()
		{
			StringBuilder stringBuilder = new StringBuilder();
			bool octal = false;
			int lineStart = _lineStart;
			int lineNumber = _lineNumber;
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
				if (c2 == '\\')
				{
					c2 = _source.CharCodeAt(_index++);
					if (c2 == '\0' || !IsLineTerminator(c2))
					{
						switch (c2)
						{
						case 'n':
							stringBuilder.Append('\n');
							continue;
						case 'r':
							stringBuilder.Append('\r');
							continue;
						case 't':
							stringBuilder.Append('\t');
							continue;
						case 'u':
						case 'x':
						{
							int index2 = _index;
							if (ScanHexEscape(c2, out var result))
							{
								stringBuilder.Append(result);
								continue;
							}
							_index = index2;
							stringBuilder.Append(c2);
							continue;
						}
						case 'b':
							stringBuilder.Append("\b");
							continue;
						case 'f':
							stringBuilder.Append("\f");
							continue;
						case 'v':
							stringBuilder.Append("\v");
							continue;
						}
						if (IsOctalDigit(c2))
						{
							int num = "01234567".IndexOf(c2);
							if (num != 0)
							{
								octal = true;
							}
							if (_index < _length && IsOctalDigit(_source.CharCodeAt(_index)))
							{
								octal = true;
								num = num * 8 + "01234567".IndexOf(_source.CharCodeAt(_index++));
								if ("0123".IndexOf(c2) >= 0 && _index < _length && IsOctalDigit(_source.CharCodeAt(_index)))
								{
									num = num * 8 + "01234567".IndexOf(_source.CharCodeAt(_index++));
								}
							}
							stringBuilder.Append((char)num);
						}
						else
						{
							stringBuilder.Append(c2);
						}
					}
					else
					{
						_lineNumber++;
						if (c2 == '\r' && _source.CharCodeAt(_index) == '\n')
						{
							_index++;
						}
						_lineStart = _index;
					}
				}
				else
				{
					if (IsLineTerminator(c2))
					{
						break;
					}
					stringBuilder.Append(c2);
				}
			}
			if (c != 0)
			{
				ThrowError(null, Messages.UnexpectedToken, "ILLEGAL");
			}
			Token token = new Token();
			token.Type = Tokens.StringLiteral;
			token.Value = stringBuilder.ToString();
			token.Octal = octal;
			token.LineNumber = _lineNumber;
			token.LineStart = _lineStart;
			token.Range = new int[2] { index, _index };
			return token;
		}

		private Token ScanRegExp()
		{
			bool flag = false;
			bool flag2 = false;
			SkipComment();
			int index = _index;
			StringBuilder stringBuilder = new StringBuilder(_source.CharCodeAt(_index++).ToString());
			while (_index < _length)
			{
				char c = _source.CharCodeAt(_index++);
				stringBuilder.Append(c);
				if (c == '\\')
				{
					c = _source.CharCodeAt(_index++);
					if (IsLineTerminator(c))
					{
						ThrowError(null, Messages.UnterminatedRegExp);
					}
					stringBuilder.Append(c);
					continue;
				}
				if (IsLineTerminator(c))
				{
					ThrowError(null, Messages.UnterminatedRegExp);
					continue;
				}
				if (flag)
				{
					if (c == ']')
					{
						flag = false;
					}
					continue;
				}
				switch (c)
				{
				case '[':
					flag = true;
					continue;
				case '/':
					break;
				default:
					continue;
				}
				flag2 = true;
				break;
			}
			if (!flag2)
			{
				ThrowError(null, Messages.UnterminatedRegExp);
			}
			string text = stringBuilder.ToString().Substring(1, stringBuilder.Length - 2);
			string text2 = "";
			while (_index < _length)
			{
				char c2 = _source.CharCodeAt(_index);
				if (!IsIdentifierPart(c2))
				{
					break;
				}
				_index++;
				if (c2 == '\\' && _index < _length)
				{
					c2 = _source.CharCodeAt(_index);
					if (c2 == 'u')
					{
						_index++;
						int i = _index;
						if (ScanHexEscape('u', out c2))
						{
							text2 += c2;
							stringBuilder.Append("\\u");
							for (; i < _index; i++)
							{
								stringBuilder.Append(_source.CharCodeAt(i).ToString());
							}
						}
						else
						{
							_index = i;
							text2 += "u";
							stringBuilder.Append("\\u");
						}
					}
					else
					{
						stringBuilder.Append("\\");
					}
				}
				else
				{
					text2 += c2;
					stringBuilder.Append(c2.ToString());
				}
			}
			Peek();
			Token token = new Token();
			token.Type = Tokens.RegularExpression;
			token.Literal = stringBuilder.ToString();
			token.Value = text + text2;
			token.Range = new int[2] { index, _index };
			return token;
		}

		private Token CollectRegex()
		{
			SkipComment();
			int index = _index;
			Location location = new Location();
			Position start = default(Position);
			start.Line = _lineNumber;
			start.Column = _index - _lineStart;
			Position position = (location.Start = start);
			Location location2 = location;
			Token token = ScanRegExp();
			start = default(Position);
			start.Line = _lineNumber;
			start.Column = _index - _lineStart;
			position = (location2.End = start);
			if (_extra.Tokens != null)
			{
				Token token2 = _extra.Tokens[_extra.Tokens.Count - 1];
				if (token2.Range[0] == index && token2.Type == Tokens.Punctuator && ("/".Equals(token2.Value) || "/=".Equals(token2.Value)))
				{
					_extra.Tokens.RemoveAt(_extra.Tokens.Count - 1);
				}
				_extra.Tokens.Add(new Token
				{
					Type = Tokens.RegularExpression,
					Value = token.Literal,
					Range = new int[2] { index, _index },
					Location = location2
				});
			}
			return token;
		}

		private bool IsIdentifierName(Token token)
		{
			if (token.Type != Tokens.Identifier && token.Type != Tokens.Keyword && token.Type != Tokens.BooleanLiteral)
			{
				return token.Type == Tokens.NullLiteral;
			}
			return true;
		}

		private Token Advance()
		{
			SkipComment();
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
			case '\'':
				return ScanStringLiteral();
			default:
				if (IsIdentifierStart(c))
				{
					return ScanIdentifier();
				}
				if (c == '.')
				{
					if (IsDecimalDigit(_source.CharCodeAt(_index + 1)))
					{
						return ScanNumericLiteral();
					}
					return ScanPunctuator();
				}
				if (IsDecimalDigit(c))
				{
					return ScanNumericLiteral();
				}
				return ScanPunctuator();
			}
		}

		private Token CollectToken()
		{
			SkipComment();
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
					Range = range,
					Location = _location
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
			SkipComment();
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

		public ArrayExpression CreateArrayExpression(IEnumerable<Expression> elements)
		{
			return new ArrayExpression
			{
				Type = SyntaxNodes.ArrayExpression,
				Elements = elements
			};
		}

		public AssignmentExpression CreateAssignmentExpression(string op, Expression left, Expression right)
		{
			return new AssignmentExpression
			{
				Type = SyntaxNodes.AssignmentExpression,
				Operator = AssignmentExpression.ParseAssignmentOperator(op),
				Left = left,
				Right = right
			};
		}

		public Expression CreateBinaryExpression(string op, Expression left, Expression right)
		{
			if (!(op == "||") && !(op == "&&"))
			{
				return new BinaryExpression
				{
					Type = SyntaxNodes.BinaryExpression,
					Operator = BinaryExpression.ParseBinaryOperator(op),
					Left = left,
					Right = right
				};
			}
			return new LogicalExpression
			{
				Type = SyntaxNodes.LogicalExpression,
				Operator = LogicalExpression.ParseLogicalOperator(op),
				Left = left,
				Right = right
			};
		}

		public BlockStatement CreateBlockStatement(IEnumerable<Statement> body)
		{
			return new BlockStatement
			{
				Type = SyntaxNodes.BlockStatement,
				Body = body
			};
		}

		public BreakStatement CreateBreakStatement(Identifier label)
		{
			return new BreakStatement
			{
				Type = SyntaxNodes.BreakStatement,
				Label = label
			};
		}

		public CallExpression CreateCallExpression(Expression callee, IList<Expression> args)
		{
			return new CallExpression
			{
				Type = SyntaxNodes.CallExpression,
				Callee = callee,
				Arguments = args
			};
		}

		public CatchClause CreateCatchClause(Identifier param, BlockStatement body)
		{
			return new CatchClause
			{
				Type = SyntaxNodes.CatchClause,
				Param = param,
				Body = body
			};
		}

		public ConditionalExpression CreateConditionalExpression(Expression test, Expression consequent, Expression alternate)
		{
			return new ConditionalExpression
			{
				Type = SyntaxNodes.ConditionalExpression,
				Test = test,
				Consequent = consequent,
				Alternate = alternate
			};
		}

		public ContinueStatement CreateContinueStatement(Identifier label)
		{
			return new ContinueStatement
			{
				Type = SyntaxNodes.ContinueStatement,
				Label = label
			};
		}

		public DebuggerStatement CreateDebuggerStatement()
		{
			return new DebuggerStatement
			{
				Type = SyntaxNodes.DebuggerStatement
			};
		}

		public DoWhileStatement CreateDoWhileStatement(Statement body, Expression test)
		{
			return new DoWhileStatement
			{
				Type = SyntaxNodes.DoWhileStatement,
				Body = body,
				Test = test
			};
		}

		public EmptyStatement CreateEmptyStatement()
		{
			return new EmptyStatement
			{
				Type = SyntaxNodes.EmptyStatement
			};
		}

		public ExpressionStatement CreateExpressionStatement(Expression expression)
		{
			return new ExpressionStatement
			{
				Type = SyntaxNodes.ExpressionStatement,
				Expression = expression
			};
		}

		public ForStatement CreateForStatement(SyntaxNode init, Expression test, Expression update, Statement body)
		{
			return new ForStatement
			{
				Type = SyntaxNodes.ForStatement,
				Init = init,
				Test = test,
				Update = update,
				Body = body
			};
		}

		public ForInStatement CreateForInStatement(SyntaxNode left, Expression right, Statement body)
		{
			return new ForInStatement
			{
				Type = SyntaxNodes.ForInStatement,
				Left = left,
				Right = right,
				Body = body,
				Each = false
			};
		}

		public FunctionDeclaration CreateFunctionDeclaration(Identifier id, IEnumerable<Identifier> parameters, IEnumerable<Expression> defaults, Statement body, bool strict)
		{
			FunctionDeclaration functionDeclaration = new FunctionDeclaration
			{
				Type = SyntaxNodes.FunctionDeclaration,
				Id = id,
				Parameters = parameters,
				Defaults = defaults,
				Body = body,
				Strict = strict,
				Rest = null,
				Generator = false,
				Expression = false,
				VariableDeclarations = LeaveVariableScope(),
				FunctionDeclarations = LeaveFunctionScope()
			};
			_functionScopes.Peek().FunctionDeclarations.Add(functionDeclaration);
			return functionDeclaration;
		}

		public FunctionExpression CreateFunctionExpression(Identifier id, IEnumerable<Identifier> parameters, IEnumerable<Expression> defaults, Statement body, bool strict)
		{
			return new FunctionExpression
			{
				Type = SyntaxNodes.FunctionExpression,
				Id = id,
				Parameters = parameters,
				Defaults = defaults,
				Body = body,
				Strict = strict,
				Rest = null,
				Generator = false,
				Expression = false,
				VariableDeclarations = LeaveVariableScope(),
				FunctionDeclarations = LeaveFunctionScope()
			};
		}

		public Identifier CreateIdentifier(string name)
		{
			return new Identifier
			{
				Type = SyntaxNodes.Identifier,
				Name = name
			};
		}

		public IfStatement CreateIfStatement(Expression test, Statement consequent, Statement alternate)
		{
			return new IfStatement
			{
				Type = SyntaxNodes.IfStatement,
				Test = test,
				Consequent = consequent,
				Alternate = alternate
			};
		}

		public LabelledStatement CreateLabeledStatement(Identifier label, Statement body)
		{
			return new LabelledStatement
			{
				Type = SyntaxNodes.LabeledStatement,
				Label = label,
				Body = body
			};
		}

		public Literal CreateLiteral(Token token)
		{
			if (token.Type == Tokens.RegularExpression)
			{
				return new Literal
				{
					Type = SyntaxNodes.RegularExpressionLiteral,
					Value = token.Value,
					Raw = _source.Slice(token.Range[0], token.Range[1])
				};
			}
			return new Literal
			{
				Type = SyntaxNodes.Literal,
				Value = token.Value,
				Raw = _source.Slice(token.Range[0], token.Range[1])
			};
		}

		public MemberExpression CreateMemberExpression(char accessor, Expression obj, Expression property)
		{
			return new MemberExpression
			{
				Type = SyntaxNodes.MemberExpression,
				Computed = (accessor == '['),
				Object = obj,
				Property = property
			};
		}

		public NewExpression CreateNewExpression(Expression callee, IEnumerable<Expression> args)
		{
			return new NewExpression
			{
				Type = SyntaxNodes.NewExpression,
				Callee = callee,
				Arguments = args
			};
		}

		public ObjectExpression CreateObjectExpression(IEnumerable<Property> properties)
		{
			return new ObjectExpression
			{
				Type = SyntaxNodes.ObjectExpression,
				Properties = properties
			};
		}

		public UpdateExpression CreatePostfixExpression(string op, Expression argument)
		{
			return new UpdateExpression
			{
				Type = SyntaxNodes.UpdateExpression,
				Operator = UnaryExpression.ParseUnaryOperator(op),
				Argument = argument,
				Prefix = false
			};
		}

		public Program CreateProgram(ICollection<Statement> body, bool strict)
		{
			return new Program
			{
				Type = SyntaxNodes.Program,
				Body = body,
				Strict = strict,
				VariableDeclarations = LeaveVariableScope(),
				FunctionDeclarations = LeaveFunctionScope()
			};
		}

		public Property CreateProperty(PropertyKind kind, IPropertyKeyExpression key, Expression value)
		{
			return new Property
			{
				Type = SyntaxNodes.Property,
				Key = key,
				Value = value,
				Kind = kind
			};
		}

		public ReturnStatement CreateReturnStatement(Expression argument)
		{
			return new ReturnStatement
			{
				Type = SyntaxNodes.ReturnStatement,
				Argument = argument
			};
		}

		public SequenceExpression CreateSequenceExpression(IList<Expression> expressions)
		{
			return new SequenceExpression
			{
				Type = SyntaxNodes.SequenceExpression,
				Expressions = expressions
			};
		}

		public SwitchCase CreateSwitchCase(Expression test, IEnumerable<Statement> consequent)
		{
			return new SwitchCase
			{
				Type = SyntaxNodes.SwitchCase,
				Test = test,
				Consequent = consequent
			};
		}

		public SwitchStatement CreateSwitchStatement(Expression discriminant, IEnumerable<SwitchCase> cases)
		{
			return new SwitchStatement
			{
				Type = SyntaxNodes.SwitchStatement,
				Discriminant = discriminant,
				Cases = cases
			};
		}

		public ThisExpression CreateThisExpression()
		{
			return new ThisExpression
			{
				Type = SyntaxNodes.ThisExpression
			};
		}

		public ThrowStatement CreateThrowStatement(Expression argument)
		{
			return new ThrowStatement
			{
				Type = SyntaxNodes.ThrowStatement,
				Argument = argument
			};
		}

		public TryStatement CreateTryStatement(Statement block, IEnumerable<Statement> guardedHandlers, IEnumerable<CatchClause> handlers, Statement finalizer)
		{
			return new TryStatement
			{
				Type = SyntaxNodes.TryStatement,
				Block = block,
				GuardedHandlers = guardedHandlers,
				Handlers = handlers,
				Finalizer = finalizer
			};
		}

		public UnaryExpression CreateUnaryExpression(string op, Expression argument)
		{
			if (op == "++" || op == "--")
			{
				return new UpdateExpression
				{
					Type = SyntaxNodes.UpdateExpression,
					Operator = UnaryExpression.ParseUnaryOperator(op),
					Argument = argument,
					Prefix = true
				};
			}
			return new UnaryExpression
			{
				Type = SyntaxNodes.UnaryExpression,
				Operator = UnaryExpression.ParseUnaryOperator(op),
				Argument = argument,
				Prefix = true
			};
		}

		public VariableDeclaration CreateVariableDeclaration(IEnumerable<VariableDeclarator> declarations, string kind)
		{
			VariableDeclaration variableDeclaration = new VariableDeclaration
			{
				Type = SyntaxNodes.VariableDeclaration,
				Declarations = declarations,
				Kind = kind
			};
			_variableScopes.Peek().VariableDeclarations.Add(variableDeclaration);
			return variableDeclaration;
		}

		public VariableDeclarator CreateVariableDeclarator(Identifier id, Expression init)
		{
			return new VariableDeclarator
			{
				Type = SyntaxNodes.VariableDeclarator,
				Id = id,
				Init = init
			};
		}

		public WhileStatement CreateWhileStatement(Expression test, Statement body)
		{
			return new WhileStatement
			{
				Type = SyntaxNodes.WhileStatement,
				Test = test,
				Body = body
			};
		}

		public WithStatement CreateWithStatement(Expression obj, Statement body)
		{
			return new WithStatement
			{
				Type = SyntaxNodes.WithStatement,
				Object = obj,
				Body = body
			};
		}

		private bool PeekLineTerminator()
		{
			int index = _index;
			int lineNumber = _lineNumber;
			int lineStart = _lineStart;
			SkipComment();
			bool result = _lineNumber != lineNumber;
			_index = index;
			_lineNumber = lineNumber;
			_lineStart = lineStart;
			return result;
		}

		private void ThrowError(Token token, string messageFormat, params object[] arguments)
		{
			string text = string.Format(messageFormat, arguments);
			object obj;
			if (token != null && token.LineNumber.HasValue)
			{
				int? lineNumber = token.LineNumber;
				obj = new ParserException("Line " + lineNumber + ": " + text)
				{
					Index = token.Range[0],
					LineNumber = token.LineNumber.Value,
					Column = token.Range[0] - _lineStart + 1,
					Source = _extra.Source
				};
			}
			else
			{
				obj = new ParserException("Line " + _lineNumber + ": " + text)
				{
					Index = _index,
					LineNumber = _lineNumber,
					Column = _index - _lineStart + 1,
					Source = _extra.Source
				};
			}
			ParserException ex = (ParserException)obj;
			ex.Description = text;
			throw ex;
		}

		private void ThrowErrorTolerant(Token token, string messageFormat, params object[] arguments)
		{
			try
			{
				ThrowError(token, messageFormat, arguments);
			}
			catch (Exception ex)
			{
				if (_extra.Errors == null)
				{
					throw;
				}
				_extra.Errors.Add(new ParserException(ex.Message)
				{
					Source = _extra.Source
				});
			}
		}

		private void ThrowUnexpected(Token token)
		{
			if (token.Type == Tokens.EOF)
			{
				ThrowError(token, Messages.UnexpectedEOS);
			}
			if (token.Type == Tokens.NumericLiteral)
			{
				ThrowError(token, Messages.UnexpectedNumber);
			}
			if (token.Type == Tokens.StringLiteral)
			{
				ThrowError(token, Messages.UnexpectedString);
			}
			if (token.Type == Tokens.Identifier)
			{
				ThrowError(token, Messages.UnexpectedIdentifier);
			}
			if (token.Type == Tokens.Keyword)
			{
				if (IsFutureReservedWord(token.Value as string))
				{
					ThrowError(token, Messages.UnexpectedReserved);
				}
				else if (_strict && IsStrictModeReservedWord(token.Value as string))
				{
					ThrowErrorTolerant(token, Messages.StrictReservedWord);
					return;
				}
				ThrowError(token, Messages.UnexpectedToken, token.Value as string);
			}
			ThrowError(token, Messages.UnexpectedToken, token.Value as string);
		}

		private void Expect(string value)
		{
			Token token = Lex();
			if (token.Type != Tokens.Punctuator || !value.Equals(token.Value))
			{
				ThrowUnexpected(token);
			}
		}

		private void ExpectKeyword(string keyword)
		{
			Token token = Lex();
			if (token.Type != Tokens.Keyword || !keyword.Equals(token.Value))
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

		private bool MatchKeyword(object keyword)
		{
			if (_lookahead.Type == Tokens.Keyword)
			{
				return keyword.Equals(_lookahead.Value);
			}
			return false;
		}

		private bool MatchAssign()
		{
			if (_lookahead.Type != Tokens.Punctuator)
			{
				return false;
			}
			string text = _lookahead.Value as string;
			int num;
			switch (text)
			{
			default:
				num = ((text == "|=") ? 1 : 0);
				break;
			case "=":
			case "*=":
			case "/=":
			case "%=":
			case "+=":
			case "-=":
			case "<<=":
			case ">>=":
			case ">>>=":
			case "&=":
			case "^=":
				num = 1;
				break;
			}
			return (byte)num != 0;
		}

		private void ConsumeSemicolon()
		{
			if (_source.CharCodeAt(_index) == ';')
			{
				Lex();
				return;
			}
			int lineNumber = _lineNumber;
			SkipComment();
			if (_lineNumber == lineNumber)
			{
				if (Match(";"))
				{
					Lex();
				}
				else if (_lookahead.Type != Tokens.EOF && !Match("}"))
				{
					ThrowUnexpected(_lookahead);
				}
			}
		}

		private bool isLeftHandSide(Expression expr)
		{
			if (expr.Type != SyntaxNodes.Identifier)
			{
				return expr.Type == SyntaxNodes.MemberExpression;
			}
			return true;
		}

		private ArrayExpression ParseArrayInitialiser()
		{
			List<Expression> list = new List<Expression>();
			Expect("[");
			while (!Match("]"))
			{
				if (Match(","))
				{
					Lex();
					list.Add(null);
					continue;
				}
				list.Add(ParseAssignmentExpression());
				if (!Match("]"))
				{
					Expect(",");
				}
			}
			Expect("]");
			return CreateArrayExpression(list);
		}

		private FunctionExpression ParsePropertyFunction(Identifier[] parameters, Token first = null)
		{
			EnterVariableScope();
			EnterFunctionScope();
			bool strict = _strict;
			MarkStart();
			Statement body = ParseFunctionSourceElements();
			if (first != null && _strict && IsRestrictedWord(parameters[0].Name))
			{
				ThrowErrorTolerant(first, Messages.StrictParamName);
			}
			bool strict2 = _strict;
			_strict = strict;
			return MarkEnd(CreateFunctionExpression(null, parameters, new Expression[0], body, strict2));
		}

		private IPropertyKeyExpression ParseObjectPropertyKey()
		{
			MarkStart();
			Token token = Lex();
			if (token.Type == Tokens.StringLiteral || token.Type == Tokens.NumericLiteral)
			{
				if (_strict && token.Octal)
				{
					ThrowErrorTolerant(token, Messages.StrictOctalLiteral);
				}
				return MarkEnd(CreateLiteral(token));
			}
			return MarkEnd(CreateIdentifier((string)token.Value));
		}

		private Property ParseObjectProperty()
		{
			Token lookahead = _lookahead;
			MarkStart();
			Expression value;
			if (lookahead.Type == Tokens.Identifier)
			{
				IPropertyKeyExpression key = ParseObjectPropertyKey();
				if ("get".Equals(lookahead.Value) && !Match(":"))
				{
					IPropertyKeyExpression key2 = ParseObjectPropertyKey();
					Expect("(");
					Expect(")");
					value = ParsePropertyFunction(new Identifier[0]);
					return MarkEnd(CreateProperty(PropertyKind.Get, key2, value));
				}
				if ("set".Equals(lookahead.Value) && !Match(":"))
				{
					IPropertyKeyExpression key3 = ParseObjectPropertyKey();
					Expect("(");
					lookahead = _lookahead;
					if (lookahead.Type != Tokens.Identifier)
					{
						Expect(")");
						ThrowErrorTolerant(lookahead, Messages.UnexpectedToken, (string)lookahead.Value);
						value = ParsePropertyFunction(new Identifier[0]);
					}
					else
					{
						Identifier[] parameters = new Identifier[1] { ParseVariableIdentifier() };
						Expect(")");
						value = ParsePropertyFunction(parameters, lookahead);
					}
					return MarkEnd(CreateProperty(PropertyKind.Set, key3, value));
				}
				Expect(":");
				value = ParseAssignmentExpression();
				return MarkEnd(CreateProperty(PropertyKind.Data, key, value));
			}
			if (lookahead.Type == Tokens.EOF || lookahead.Type == Tokens.Punctuator)
			{
				ThrowUnexpected(lookahead);
				return null;
			}
			IPropertyKeyExpression key4 = ParseObjectPropertyKey();
			Expect(":");
			value = ParseAssignmentExpression();
			return MarkEnd(CreateProperty(PropertyKind.Data, key4, value));
		}

		private ObjectExpression ParseObjectInitialiser()
		{
			List<Property> list = new List<Property>();
			Dictionary<string, PropertyKind> dictionary = new Dictionary<string, PropertyKind>();
			Expect("{");
			while (!Match("}"))
			{
				Property property = ParseObjectProperty();
				string key = property.Key.GetKey();
				PropertyKind kind = property.Kind;
				string key2 = "$" + key;
				if (dictionary.ContainsKey(key2))
				{
					if (dictionary[key2] == PropertyKind.Data)
					{
						if (_strict && kind == PropertyKind.Data)
						{
							ThrowErrorTolerant(Token.Empty, Messages.StrictDuplicateProperty);
						}
						else if (kind != PropertyKind.Data)
						{
							ThrowErrorTolerant(Token.Empty, Messages.AccessorDataProperty);
						}
					}
					else if (kind == PropertyKind.Data)
					{
						ThrowErrorTolerant(Token.Empty, Messages.AccessorDataProperty);
					}
					else if ((dictionary[key2] & kind) == kind)
					{
						ThrowErrorTolerant(Token.Empty, Messages.AccessorGetSet);
					}
					dictionary[key2] |= kind;
				}
				else
				{
					dictionary[key2] = kind;
				}
				list.Add(property);
				if (!Match("}"))
				{
					Expect(",");
				}
			}
			Expect("}");
			return CreateObjectExpression(list);
		}

		private Expression ParseGroupExpression()
		{
			Expect("(");
			Expression result = ParseExpression();
			Expect(")");
			return result;
		}

		private Expression ParsePrimaryExpression()
		{
			Expression expression = null;
			if (Match("("))
			{
				return ParseGroupExpression();
			}
			Tokens type = _lookahead.Type;
			MarkStart();
			switch (type)
			{
			case Tokens.Identifier:
				expression = CreateIdentifier((string)Lex().Value);
				break;
			case Tokens.NumericLiteral:
			case Tokens.StringLiteral:
				if (_strict && _lookahead.Octal)
				{
					ThrowErrorTolerant(_lookahead, Messages.StrictOctalLiteral);
				}
				expression = CreateLiteral(Lex());
				break;
			case Tokens.Keyword:
				if (MatchKeyword("this"))
				{
					Lex();
					expression = CreateThisExpression();
				}
				else if (MatchKeyword("function"))
				{
					expression = ParseFunctionExpression();
				}
				break;
			case Tokens.BooleanLiteral:
			{
				Token token2 = Lex();
				token2.Value = "true".Equals(token2.Value);
				expression = CreateLiteral(token2);
				break;
			}
			case Tokens.NullLiteral:
			{
				Token token = Lex();
				token.Value = null;
				expression = CreateLiteral(token);
				break;
			}
			default:
				if (Match("["))
				{
					expression = ParseArrayInitialiser();
				}
				else if (Match("{"))
				{
					expression = ParseObjectInitialiser();
				}
				else if (Match("/") || Match("/="))
				{
					expression = CreateLiteral((_extra.Tokens != null) ? CollectRegex() : ScanRegExp());
				}
				break;
			}
			if (expression != null)
			{
				return MarkEnd(expression);
			}
			ThrowUnexpected(Lex());
			return null;
		}

		private IList<Expression> ParseArguments()
		{
			List<Expression> list = new List<Expression>();
			Expect("(");
			if (!Match(")"))
			{
				while (_index < _length)
				{
					list.Add(ParseAssignmentExpression());
					if (Match(")"))
					{
						break;
					}
					Expect(",");
				}
			}
			Expect(")");
			return list;
		}

		private Identifier ParseNonComputedProperty()
		{
			MarkStart();
			Token token = Lex();
			if (!IsIdentifierName(token))
			{
				ThrowUnexpected(token);
			}
			return MarkEnd(CreateIdentifier((string)token.Value));
		}

		private Identifier ParseNonComputedMember()
		{
			Expect(".");
			return ParseNonComputedProperty();
		}

		private Expression ParseComputedMember()
		{
			Expect("[");
			Expression result = ParseExpression();
			Expect("]");
			return result;
		}

		private NewExpression ParseNewExpression()
		{
			MarkStart();
			ExpectKeyword("new");
			Expression callee = ParseLeftHandSideExpression();
			IList<Expression> list2;
			if (!Match("("))
			{
				IList<Expression> list = new AssignmentExpression[0];
				list2 = list;
			}
			else
			{
				list2 = ParseArguments();
			}
			IEnumerable<Expression> args = list2;
			return MarkEnd(CreateNewExpression(callee, args));
		}

		private Expression ParseLeftHandSideExpressionAllowCall()
		{
			LocationMarker locationMarker = CreateLocationMarker();
			bool allowIn = _state.AllowIn;
			_state.AllowIn = true;
			Expression expression = (MatchKeyword("new") ? ParseNewExpression() : ParsePrimaryExpression());
			_state.AllowIn = allowIn;
			while (Match(".") || Match("[") || Match("("))
			{
				if (Match("("))
				{
					IList<Expression> args = ParseArguments();
					expression = CreateCallExpression(expression, args);
				}
				else if (Match("["))
				{
					Expression property = ParseComputedMember();
					expression = CreateMemberExpression('[', expression, property);
				}
				else
				{
					Identifier property2 = ParseNonComputedMember();
					expression = CreateMemberExpression('.', expression, property2);
				}
				if (locationMarker != null)
				{
					locationMarker.End(_index, _lineNumber, _lineStart);
					locationMarker.Apply(expression, _extra, PostProcess);
				}
			}
			return expression;
		}

		private Expression ParseLeftHandSideExpression()
		{
			LocationMarker locationMarker = CreateLocationMarker();
			bool allowIn = _state.AllowIn;
			Expression expression = (MatchKeyword("new") ? ParseNewExpression() : ParsePrimaryExpression());
			_state.AllowIn = allowIn;
			while (Match(".") || Match("["))
			{
				if (Match("["))
				{
					Expression property = ParseComputedMember();
					expression = CreateMemberExpression('[', expression, property);
				}
				else
				{
					Identifier property2 = ParseNonComputedMember();
					expression = CreateMemberExpression('.', expression, property2);
				}
				if (locationMarker != null)
				{
					locationMarker.End(_index, _lineNumber, _lineStart);
					locationMarker.Apply(expression, _extra, PostProcess);
				}
			}
			return expression;
		}

		private Expression ParsePostfixExpression()
		{
			MarkStart();
			Expression expression = ParseLeftHandSideExpressionAllowCall();
			if (_lookahead.Type == Tokens.Punctuator && (Match("++") || Match("--")) && !PeekLineTerminator())
			{
				if (_strict && expression.Type == SyntaxNodes.Identifier && IsRestrictedWord(((Identifier)expression).Name))
				{
					ThrowErrorTolerant(Token.Empty, Messages.StrictLHSPostfix);
				}
				if (!isLeftHandSide(expression))
				{
					ThrowErrorTolerant(Token.Empty, Messages.InvalidLHSInAssignment);
				}
				Token token = Lex();
				expression = CreatePostfixExpression((string)token.Value, expression);
			}
			return MarkEndIf(expression);
		}

		private Expression ParseUnaryExpression()
		{
			MarkStart();
			Expression node;
			if (_lookahead.Type != Tokens.Punctuator && _lookahead.Type != Tokens.Keyword)
			{
				node = ParsePostfixExpression();
			}
			else if (Match("++") || Match("--"))
			{
				Token token = Lex();
				node = ParseUnaryExpression();
				if (_strict && node.Type == SyntaxNodes.Identifier && IsRestrictedWord(((Identifier)node).Name))
				{
					ThrowErrorTolerant(Token.Empty, Messages.StrictLHSPrefix);
				}
				if (!isLeftHandSide(node))
				{
					ThrowErrorTolerant(Token.Empty, Messages.InvalidLHSInAssignment);
				}
				node = CreateUnaryExpression((string)token.Value, node);
			}
			else if (Match("+") || Match("-") || Match("~") || Match("!"))
			{
				Token token2 = Lex();
				node = ParseUnaryExpression();
				node = CreateUnaryExpression((string)token2.Value, node);
			}
			else if (MatchKeyword("delete") || MatchKeyword("void") || MatchKeyword("typeof"))
			{
				Token token3 = Lex();
				node = ParseUnaryExpression();
				UnaryExpression unaryExpression = CreateUnaryExpression((string)token3.Value, node);
				if (_strict && unaryExpression.Operator == UnaryOperator.Delete && unaryExpression.Argument.Type == SyntaxNodes.Identifier)
				{
					ThrowErrorTolerant(Token.Empty, Messages.StrictDelete);
				}
				node = unaryExpression;
			}
			else
			{
				node = ParsePostfixExpression();
			}
			return MarkEndIf(node);
		}

		private int binaryPrecedence(Token token, bool allowIn)
		{
			int result = 0;
			if (token.Type != Tokens.Punctuator && token.Type != Tokens.Keyword)
			{
				return 0;
			}
			switch ((string)token.Value)
			{
			case "||":
				result = 1;
				break;
			case "&&":
				result = 2;
				break;
			case "|":
				result = 3;
				break;
			case "^":
				result = 4;
				break;
			case "&":
				result = 5;
				break;
			case "==":
			case "!=":
			case "===":
			case "!==":
				result = 6;
				break;
			case "<":
			case ">":
			case "<=":
			case ">=":
			case "instanceof":
				result = 7;
				break;
			case "in":
				result = (allowIn ? 7 : 0);
				break;
			case "<<":
			case ">>":
			case ">>>":
				result = 8;
				break;
			case "+":
			case "-":
				result = 9;
				break;
			case "*":
			case "/":
			case "%":
				result = 11;
				break;
			}
			return result;
		}

		private Expression ParseBinaryExpression()
		{
			LocationMarker locationMarker = CreateLocationMarker();
			Expression expression = ParseUnaryExpression();
			Token lookahead = _lookahead;
			int num = binaryPrecedence(lookahead, _state.AllowIn);
			if (num == 0)
			{
				return expression;
			}
			lookahead.Precedence = num;
			Lex();
			Stack<LocationMarker> stack = new Stack<LocationMarker>(new LocationMarker[2]
			{
				locationMarker,
				CreateLocationMarker()
			});
			Expression expression2 = ParseUnaryExpression();
			List<object> list = new List<object>(new object[3] { expression, lookahead, expression2 });
			Expression expression3;
			while ((num = binaryPrecedence(_lookahead, _state.AllowIn)) > 0)
			{
				while (list.Count > 2 && num <= ((Token)list[list.Count - 2]).Precedence)
				{
					expression2 = (Expression)list.Pop();
					string op = (string)((Token)list.Pop()).Value;
					expression = (Expression)list.Pop();
					expression3 = CreateBinaryExpression(op, expression, expression2);
					stack.Pop();
					locationMarker = stack.Pop();
					if (locationMarker != null)
					{
						locationMarker.End(_index, _lineNumber, _lineStart);
						locationMarker.Apply(expression3, _extra, PostProcess);
					}
					list.Push(expression3);
					stack.Push(locationMarker);
				}
				lookahead = Lex();
				lookahead.Precedence = num;
				list.Push(lookahead);
				stack.Push(CreateLocationMarker());
				expression3 = ParseUnaryExpression();
				list.Push(expression3);
			}
			int num2 = list.Count - 1;
			expression3 = (Expression)list[num2];
			stack.Pop();
			while (num2 > 1)
			{
				expression3 = CreateBinaryExpression((string)((Token)list[num2 - 1]).Value, (Expression)list[num2 - 2], expression3);
				num2 -= 2;
				locationMarker = stack.Pop();
				if (locationMarker != null)
				{
					locationMarker.End(_index, _lineNumber, _lineStart);
					locationMarker.Apply(expression3, _extra, PostProcess);
				}
			}
			return expression3;
		}

		private Expression ParseConditionalExpression()
		{
			MarkStart();
			Expression expression = ParseBinaryExpression();
			if (Match("?"))
			{
				Lex();
				bool allowIn = _state.AllowIn;
				_state.AllowIn = true;
				Expression consequent = ParseAssignmentExpression();
				_state.AllowIn = allowIn;
				Expect(":");
				Expression alternate = ParseAssignmentExpression();
				expression = MarkEnd(CreateConditionalExpression(expression, consequent, alternate));
			}
			else
			{
				MarkEnd(new SyntaxNode());
			}
			return expression;
		}

		private Expression ParseAssignmentExpression()
		{
			Token lookahead = _lookahead;
			MarkStart();
			Expression expression;
			Expression node = (expression = ParseConditionalExpression());
			if (MatchAssign())
			{
				if (_strict && expression.Type == SyntaxNodes.Identifier && IsRestrictedWord(((Identifier)expression).Name))
				{
					ThrowErrorTolerant(lookahead, Messages.StrictLHSAssignment);
				}
				lookahead = Lex();
				Expression right = ParseAssignmentExpression();
				node = CreateAssignmentExpression((string)lookahead.Value, expression, right);
			}
			return MarkEndIf(node);
		}

		private Expression ParseExpression()
		{
			MarkStart();
			Expression expression = ParseAssignmentExpression();
			if (Match(","))
			{
				expression = CreateSequenceExpression(new List<Expression> { expression });
				while (_index < _length && Match(","))
				{
					Lex();
					((SequenceExpression)expression).Expressions.Add(ParseAssignmentExpression());
				}
			}
			return MarkEndIf(expression);
		}

		private IEnumerable<Statement> ParseStatementList()
		{
			List<Statement> list = new List<Statement>();
			while (_index < _length && !Match("}"))
			{
				Statement statement = ParseSourceElement();
				if (statement == null)
				{
					break;
				}
				list.Add(statement);
			}
			return list;
		}

		private BlockStatement ParseBlock()
		{
			MarkStart();
			Expect("{");
			IEnumerable<Statement> body = ParseStatementList();
			Expect("}");
			return MarkEnd(CreateBlockStatement(body));
		}

		private Identifier ParseVariableIdentifier()
		{
			MarkStart();
			Token token = Lex();
			if (token.Type != Tokens.Identifier)
			{
				ThrowUnexpected(token);
			}
			return MarkEnd(CreateIdentifier((string)token.Value));
		}

		private VariableDeclarator ParseVariableDeclaration(string kind)
		{
			Expression init = null;
			MarkStart();
			Identifier identifier = ParseVariableIdentifier();
			if (_strict && IsRestrictedWord(identifier.Name))
			{
				ThrowErrorTolerant(Token.Empty, Messages.StrictVarName);
			}
			if ("const".Equals(kind))
			{
				Expect("=");
				init = ParseAssignmentExpression();
			}
			else if (Match("="))
			{
				Lex();
				init = ParseAssignmentExpression();
			}
			return MarkEnd(CreateVariableDeclarator(identifier, init));
		}

		private IEnumerable<VariableDeclarator> ParseVariableDeclarationList(string kind)
		{
			List<VariableDeclarator> list = new List<VariableDeclarator>();
			do
			{
				list.Add(ParseVariableDeclaration(kind));
				if (!Match(","))
				{
					break;
				}
				Lex();
			}
			while (_index < _length);
			return list;
		}

		private VariableDeclaration ParseVariableStatement()
		{
			ExpectKeyword("var");
			IEnumerable<VariableDeclarator> declarations = ParseVariableDeclarationList(null);
			ConsumeSemicolon();
			return CreateVariableDeclaration(declarations, "var");
		}

		private VariableDeclaration ParseConstLetDeclaration(string kind)
		{
			MarkStart();
			ExpectKeyword(kind);
			IEnumerable<VariableDeclarator> declarations = ParseVariableDeclarationList(kind);
			ConsumeSemicolon();
			return MarkEnd(CreateVariableDeclaration(declarations, kind));
		}

		private EmptyStatement ParseEmptyStatement()
		{
			Expect(";");
			return CreateEmptyStatement();
		}

		private ExpressionStatement ParseExpressionStatement()
		{
			Expression expression = ParseExpression();
			ConsumeSemicolon();
			return CreateExpressionStatement(expression);
		}

		private IfStatement ParseIfStatement()
		{
			ExpectKeyword("if");
			Expect("(");
			Expression test = ParseExpression();
			Expect(")");
			Statement consequent = ParseStatement();
			Statement alternate;
			if (MatchKeyword("else"))
			{
				Lex();
				alternate = ParseStatement();
			}
			else
			{
				alternate = null;
			}
			return CreateIfStatement(test, consequent, alternate);
		}

		private DoWhileStatement ParseDoWhileStatement()
		{
			ExpectKeyword("do");
			bool inIteration = _state.InIteration;
			_state.InIteration = true;
			Statement body = ParseStatement();
			_state.InIteration = inIteration;
			ExpectKeyword("while");
			Expect("(");
			Expression test = ParseExpression();
			Expect(")");
			if (Match(";"))
			{
				Lex();
			}
			return CreateDoWhileStatement(body, test);
		}

		private WhileStatement ParseWhileStatement()
		{
			ExpectKeyword("while");
			Expect("(");
			Expression test = ParseExpression();
			Expect(")");
			bool inIteration = _state.InIteration;
			_state.InIteration = true;
			Statement body = ParseStatement();
			_state.InIteration = inIteration;
			return CreateWhileStatement(test, body);
		}

		private VariableDeclaration ParseForVariableDeclaration()
		{
			MarkStart();
			Token token = Lex();
			IEnumerable<VariableDeclarator> declarations = ParseVariableDeclarationList(null);
			return MarkEnd(CreateVariableDeclaration(declarations, (string)token.Value));
		}

		private Statement ParseForStatement()
		{
			SyntaxNode syntaxNode = null;
			SyntaxNode syntaxNode2 = null;
			Expression right = null;
			Expression test = null;
			Expression update = null;
			ExpectKeyword("for");
			Expect("(");
			if (Match(";"))
			{
				Lex();
			}
			else
			{
				if (MatchKeyword("var") || MatchKeyword("let"))
				{
					_state.AllowIn = false;
					syntaxNode = ParseForVariableDeclaration();
					_state.AllowIn = true;
					if (syntaxNode.As<VariableDeclaration>().Declarations.Count() == 1 && MatchKeyword("in"))
					{
						Lex();
						syntaxNode2 = syntaxNode;
						right = ParseExpression();
						syntaxNode = null;
					}
				}
				else
				{
					_state.AllowIn = false;
					syntaxNode = ParseExpression();
					_state.AllowIn = true;
					if (MatchKeyword("in"))
					{
						if (!isLeftHandSide((Expression)syntaxNode))
						{
							ThrowErrorTolerant(Token.Empty, Messages.InvalidLHSInForIn);
						}
						Lex();
						syntaxNode2 = syntaxNode;
						right = ParseExpression();
						syntaxNode = null;
					}
				}
				if (syntaxNode2 == null)
				{
					Expect(";");
				}
			}
			if (syntaxNode2 == null)
			{
				if (!Match(";"))
				{
					test = ParseExpression();
				}
				Expect(";");
				if (!Match(")"))
				{
					update = ParseExpression();
				}
			}
			Expect(")");
			bool inIteration = _state.InIteration;
			_state.InIteration = true;
			Statement body = ParseStatement();
			_state.InIteration = inIteration;
			if (syntaxNode2 != null)
			{
				return CreateForInStatement(syntaxNode2, right, body);
			}
			return CreateForStatement(syntaxNode, test, update, body);
		}

		private Statement ParseContinueStatement()
		{
			Identifier identifier = null;
			ExpectKeyword("continue");
			if (_source.CharCodeAt(_index) == ';')
			{
				Lex();
				if (!_state.InIteration)
				{
					ThrowError(Token.Empty, Messages.IllegalContinue);
				}
				return CreateContinueStatement(null);
			}
			if (PeekLineTerminator())
			{
				if (!_state.InIteration)
				{
					ThrowError(Token.Empty, Messages.IllegalContinue);
				}
				return CreateContinueStatement(null);
			}
			if (_lookahead.Type == Tokens.Identifier)
			{
				identifier = ParseVariableIdentifier();
				string item = "$" + identifier.Name;
				if (!_state.LabelSet.Contains(item))
				{
					ThrowError(Token.Empty, Messages.UnknownLabel, identifier.Name);
				}
			}
			ConsumeSemicolon();
			if (identifier == null && !_state.InIteration)
			{
				ThrowError(Token.Empty, Messages.IllegalContinue);
			}
			return CreateContinueStatement(identifier);
		}

		private BreakStatement ParseBreakStatement()
		{
			Identifier identifier = null;
			ExpectKeyword("break");
			if (_source.CharCodeAt(_index) == ';')
			{
				Lex();
				if (!_state.InIteration && !_state.InSwitch)
				{
					ThrowError(Token.Empty, Messages.IllegalBreak);
				}
				return CreateBreakStatement(null);
			}
			if (PeekLineTerminator())
			{
				if (!_state.InIteration && !_state.InSwitch)
				{
					ThrowError(Token.Empty, Messages.IllegalBreak);
				}
				return CreateBreakStatement(null);
			}
			if (_lookahead.Type == Tokens.Identifier)
			{
				identifier = ParseVariableIdentifier();
				string item = "$" + identifier.Name;
				if (!_state.LabelSet.Contains(item))
				{
					ThrowError(Token.Empty, Messages.UnknownLabel, identifier.Name);
				}
			}
			ConsumeSemicolon();
			if (identifier == null && !_state.InIteration && !_state.InSwitch)
			{
				ThrowError(Token.Empty, Messages.IllegalBreak);
			}
			return CreateBreakStatement(identifier);
		}

		private ReturnStatement ParseReturnStatement()
		{
			Expression argument = null;
			ExpectKeyword("return");
			if (!_state.InFunctionBody)
			{
				ThrowErrorTolerant(Token.Empty, Messages.IllegalReturn);
			}
			if (_source.CharCodeAt(_index) == ' ' && IsIdentifierStart(_source.CharCodeAt(_index + 1)))
			{
				argument = ParseExpression();
				ConsumeSemicolon();
				return CreateReturnStatement(argument);
			}
			if (PeekLineTerminator())
			{
				return CreateReturnStatement(null);
			}
			if (!Match(";") && !Match("}") && _lookahead.Type != Tokens.EOF)
			{
				argument = ParseExpression();
			}
			ConsumeSemicolon();
			return CreateReturnStatement(argument);
		}

		private WithStatement ParseWithStatement()
		{
			if (_strict)
			{
				ThrowErrorTolerant(Token.Empty, Messages.StrictModeWith);
			}
			ExpectKeyword("with");
			Expect("(");
			Expression obj = ParseExpression();
			Expect(")");
			Statement body = ParseStatement();
			return CreateWithStatement(obj, body);
		}

		private SwitchCase ParseSwitchCase()
		{
			List<Statement> list = new List<Statement>();
			MarkStart();
			Expression test;
			if (MatchKeyword("default"))
			{
				Lex();
				test = null;
			}
			else
			{
				ExpectKeyword("case");
				test = ParseExpression();
			}
			Expect(":");
			while (_index < _length && !Match("}") && !MatchKeyword("default") && !MatchKeyword("case"))
			{
				Statement item = ParseStatement();
				list.Add(item);
			}
			return MarkEnd(CreateSwitchCase(test, list));
		}

		private SwitchStatement ParseSwitchStatement()
		{
			ExpectKeyword("switch");
			Expect("(");
			Expression discriminant = ParseExpression();
			Expect(")");
			Expect("{");
			List<SwitchCase> list = new List<SwitchCase>();
			if (Match("}"))
			{
				Lex();
				return CreateSwitchStatement(discriminant, list);
			}
			bool inSwitch = _state.InSwitch;
			_state.InSwitch = true;
			bool flag = false;
			while (_index < _length && !Match("}"))
			{
				SwitchCase switchCase = ParseSwitchCase();
				if (switchCase.Test == null)
				{
					if (flag)
					{
						ThrowError(Token.Empty, Messages.MultipleDefaultsInSwitch);
					}
					flag = true;
				}
				list.Add(switchCase);
			}
			_state.InSwitch = inSwitch;
			Expect("}");
			return CreateSwitchStatement(discriminant, list);
		}

		private ThrowStatement ParseThrowStatement()
		{
			ExpectKeyword("throw");
			if (PeekLineTerminator())
			{
				ThrowError(Token.Empty, Messages.NewlineAfterThrow);
			}
			Expression argument = ParseExpression();
			ConsumeSemicolon();
			return CreateThrowStatement(argument);
		}

		private CatchClause ParseCatchClause()
		{
			MarkStart();
			ExpectKeyword("catch");
			Expect("(");
			if (Match(")"))
			{
				ThrowUnexpected(_lookahead);
			}
			Identifier identifier = ParseVariableIdentifier();
			if (_strict && IsRestrictedWord(identifier.Name))
			{
				ThrowErrorTolerant(Token.Empty, Messages.StrictCatchVariable);
			}
			Expect(")");
			BlockStatement body = ParseBlock();
			return MarkEnd(CreateCatchClause(identifier, body));
		}

		private TryStatement ParseTryStatement()
		{
			List<CatchClause> list = new List<CatchClause>();
			Statement statement = null;
			ExpectKeyword("try");
			BlockStatement block = ParseBlock();
			if (MatchKeyword("catch"))
			{
				list.Add(ParseCatchClause());
			}
			if (MatchKeyword("finally"))
			{
				Lex();
				statement = ParseBlock();
			}
			if (list.Count == 0 && statement == null)
			{
				ThrowError(Token.Empty, Messages.NoCatchOrFinally);
			}
			return CreateTryStatement(block, new Statement[0], list, statement);
		}

		private DebuggerStatement ParseDebuggerStatement()
		{
			ExpectKeyword("debugger");
			ConsumeSemicolon();
			return CreateDebuggerStatement();
		}

		private Statement ParseStatement()
		{
			Tokens type = _lookahead.Type;
			if (type == Tokens.EOF)
			{
				ThrowUnexpected(_lookahead);
			}
			MarkStart();
			if (type == Tokens.Punctuator)
			{
				switch ((string)_lookahead.Value)
				{
				case ";":
					return MarkEnd(ParseEmptyStatement());
				case "{":
					return MarkEnd(ParseBlock());
				case "(":
					return MarkEnd(ParseExpressionStatement());
				}
			}
			if (type == Tokens.Keyword)
			{
				switch ((string)_lookahead.Value)
				{
				case "break":
					return MarkEnd(ParseBreakStatement());
				case "continue":
					return MarkEnd(ParseContinueStatement());
				case "debugger":
					return MarkEnd(ParseDebuggerStatement());
				case "do":
					return MarkEnd(ParseDoWhileStatement());
				case "for":
					return MarkEnd(ParseForStatement());
				case "function":
					return MarkEnd(ParseFunctionDeclaration());
				case "if":
					return MarkEnd(ParseIfStatement());
				case "return":
					return MarkEnd(ParseReturnStatement());
				case "switch":
					return MarkEnd(ParseSwitchStatement());
				case "throw":
					return MarkEnd(ParseThrowStatement());
				case "try":
					return MarkEnd(ParseTryStatement());
				case "var":
					return MarkEnd(ParseVariableStatement());
				case "while":
					return MarkEnd(ParseWhileStatement());
				case "with":
					return MarkEnd(ParseWithStatement());
				}
			}
			Expression expression = ParseExpression();
			if (expression.Type == SyntaxNodes.Identifier && Match(":"))
			{
				Lex();
				string item = "$" + ((Identifier)expression).Name;
				if (_state.LabelSet.Contains(item))
				{
					ThrowError(Token.Empty, Messages.Redeclaration, "Label", ((Identifier)expression).Name);
				}
				_state.LabelSet.Add(item);
				Statement body = ParseStatement();
				_state.LabelSet.Remove(item);
				return MarkEnd(CreateLabeledStatement((Identifier)expression, body));
			}
			ConsumeSemicolon();
			return MarkEnd(CreateExpressionStatement(expression));
		}

		private Statement ParseFunctionSourceElements()
		{
			Token token = Token.Empty;
			List<Statement> list = new List<Statement>();
			MarkStart();
			Expect("{");
			while (_index < _length && _lookahead.Type == Tokens.StringLiteral)
			{
				Token lookahead = _lookahead;
				Statement statement = ParseSourceElement();
				list.Add(statement);
				if (((ExpressionStatement)statement).Expression.Type != SyntaxNodes.Literal)
				{
					break;
				}
				string text = _source.Slice(lookahead.Range[0] + 1, lookahead.Range[1] - 1);
				if (text == "use strict")
				{
					_strict = true;
					if (token != Token.Empty)
					{
						ThrowErrorTolerant(token, Messages.StrictOctalLiteral);
					}
				}
				else if (token == Token.Empty && lookahead.Octal)
				{
					token = lookahead;
				}
			}
			HashSet<string> labelSet = _state.LabelSet;
			bool inIteration = _state.InIteration;
			bool inSwitch = _state.InSwitch;
			bool inFunctionBody = _state.InFunctionBody;
			_state.LabelSet = new HashSet<string>();
			_state.InIteration = false;
			_state.InSwitch = false;
			_state.InFunctionBody = true;
			while (_index < _length && !Match("}"))
			{
				Statement statement2 = ParseSourceElement();
				if (statement2 == null)
				{
					break;
				}
				list.Add(statement2);
			}
			Expect("}");
			_state.LabelSet = labelSet;
			_state.InIteration = inIteration;
			_state.InSwitch = inSwitch;
			_state.InFunctionBody = inFunctionBody;
			return MarkEnd(CreateBlockStatement(list));
		}

		private ParsedParameters ParseParams(Token firstRestricted)
		{
			string message = null;
			Token stricted = Token.Empty;
			List<Identifier> list = new List<Identifier>();
			Expect("(");
			if (!Match(")"))
			{
				HashSet<string> hashSet = new HashSet<string>();
				while (_index < _length)
				{
					Token lookahead = _lookahead;
					Identifier item = ParseVariableIdentifier();
					string item2 = "$" + (string)lookahead.Value;
					if (_strict)
					{
						if (IsRestrictedWord((string)lookahead.Value))
						{
							stricted = lookahead;
							message = Messages.StrictParamName;
						}
						if (hashSet.Contains(item2))
						{
							stricted = lookahead;
							message = Messages.StrictParamDupe;
						}
					}
					else if (firstRestricted == Token.Empty)
					{
						if (IsRestrictedWord((string)lookahead.Value))
						{
							firstRestricted = lookahead;
							message = Messages.StrictParamName;
						}
						else if (IsStrictModeReservedWord((string)lookahead.Value))
						{
							firstRestricted = lookahead;
							message = Messages.StrictReservedWord;
						}
						else if (hashSet.Contains(item2))
						{
							firstRestricted = lookahead;
							message = Messages.StrictParamDupe;
						}
					}
					list.Add(item);
					hashSet.Add(item2);
					if (Match(")"))
					{
						break;
					}
					Expect(",");
				}
			}
			Expect(")");
			ParsedParameters result = default(ParsedParameters);
			result.Parameters = list;
			result.Stricted = stricted;
			result.FirstRestricted = firstRestricted;
			result.Message = message;
			return result;
		}

		private Statement ParseFunctionDeclaration()
		{
			EnterVariableScope();
			EnterFunctionScope();
			Token firstRestricted = Token.Empty;
			string messageFormat = null;
			MarkStart();
			ExpectKeyword("function");
			Token lookahead = _lookahead;
			Identifier id = ParseVariableIdentifier();
			if (_strict)
			{
				if (IsRestrictedWord((string)lookahead.Value))
				{
					ThrowErrorTolerant(lookahead, Messages.StrictFunctionName);
				}
			}
			else if (IsRestrictedWord((string)lookahead.Value))
			{
				firstRestricted = lookahead;
				messageFormat = Messages.StrictFunctionName;
			}
			else if (IsStrictModeReservedWord((string)lookahead.Value))
			{
				firstRestricted = lookahead;
				messageFormat = Messages.StrictReservedWord;
			}
			ParsedParameters parsedParameters = ParseParams(firstRestricted);
			IEnumerable<Identifier> parameters = parsedParameters.Parameters;
			Token stricted = parsedParameters.Stricted;
			firstRestricted = parsedParameters.FirstRestricted;
			if (parsedParameters.Message != null)
			{
				messageFormat = parsedParameters.Message;
			}
			bool strict = _strict;
			Statement body = ParseFunctionSourceElements();
			if (_strict && firstRestricted != Token.Empty)
			{
				ThrowError(firstRestricted, messageFormat);
			}
			if (_strict && stricted != Token.Empty)
			{
				ThrowErrorTolerant(stricted, messageFormat);
			}
			bool strict2 = _strict;
			_strict = strict;
			return MarkEnd(CreateFunctionDeclaration(id, parameters, new Expression[0], body, strict2));
		}

		private void EnterVariableScope()
		{
			_variableScopes.Push(new VariableScope());
		}

		private IList<VariableDeclaration> LeaveVariableScope()
		{
			return _variableScopes.Pop().VariableDeclarations;
		}

		private void EnterFunctionScope()
		{
			_functionScopes.Push(new FunctionScope());
		}

		private IList<FunctionDeclaration> LeaveFunctionScope()
		{
			return _functionScopes.Pop().FunctionDeclarations;
		}

		private FunctionExpression ParseFunctionExpression()
		{
			EnterVariableScope();
			EnterFunctionScope();
			Token firstRestricted = Token.Empty;
			string messageFormat = null;
			Identifier id = null;
			MarkStart();
			ExpectKeyword("function");
			if (!Match("("))
			{
				Token lookahead = _lookahead;
				id = ParseVariableIdentifier();
				if (_strict)
				{
					if (IsRestrictedWord((string)lookahead.Value))
					{
						ThrowErrorTolerant(lookahead, Messages.StrictFunctionName);
					}
				}
				else if (IsRestrictedWord((string)lookahead.Value))
				{
					firstRestricted = lookahead;
					messageFormat = Messages.StrictFunctionName;
				}
				else if (IsStrictModeReservedWord((string)lookahead.Value))
				{
					firstRestricted = lookahead;
					messageFormat = Messages.StrictReservedWord;
				}
			}
			ParsedParameters parsedParameters = ParseParams(firstRestricted);
			IEnumerable<Identifier> parameters = parsedParameters.Parameters;
			Token stricted = parsedParameters.Stricted;
			firstRestricted = parsedParameters.FirstRestricted;
			if (parsedParameters.Message != null)
			{
				messageFormat = parsedParameters.Message;
			}
			bool strict = _strict;
			Statement body = ParseFunctionSourceElements();
			if (_strict && firstRestricted != Token.Empty)
			{
				ThrowError(firstRestricted, messageFormat);
			}
			if (_strict && stricted != Token.Empty)
			{
				ThrowErrorTolerant(stricted, messageFormat);
			}
			bool strict2 = _strict;
			_strict = strict;
			return MarkEnd(CreateFunctionExpression(id, parameters, new Expression[0], body, strict2));
		}

		private Statement ParseSourceElement()
		{
			if (_lookahead.Type == Tokens.Keyword)
			{
				switch ((string)_lookahead.Value)
				{
				case "const":
				case "let":
					return ParseConstLetDeclaration((string)_lookahead.Value);
				case "function":
					return ParseFunctionDeclaration();
				default:
					return ParseStatement();
				}
			}
			if (_lookahead.Type != Tokens.EOF)
			{
				return ParseStatement();
			}
			return null;
		}

		private ICollection<Statement> ParseSourceElements()
		{
			List<Statement> list = new List<Statement>();
			Token token = Token.Empty;
			while (_index < _length)
			{
				Token lookahead = _lookahead;
				if (lookahead.Type != Tokens.StringLiteral)
				{
					break;
				}
				Statement statement = ParseSourceElement();
				list.Add(statement);
				if (((ExpressionStatement)statement).Expression.Type != SyntaxNodes.Literal)
				{
					break;
				}
				string text = _source.Slice(lookahead.Range[0] + 1, lookahead.Range[1] - 1);
				if (text == "use strict")
				{
					_strict = true;
					if (token != Token.Empty)
					{
						ThrowErrorTolerant(token, Messages.StrictOctalLiteral);
					}
				}
				else if (token == Token.Empty && lookahead.Octal)
				{
					token = lookahead;
				}
			}
			while (_index < _length)
			{
				Statement statement2 = ParseSourceElement();
				if (statement2 == null)
				{
					break;
				}
				list.Add(statement2);
			}
			return list;
		}

		private Program ParseProgram()
		{
			EnterVariableScope();
			EnterFunctionScope();
			MarkStart();
			Peek();
			ICollection<Statement> body = ParseSourceElements();
			return MarkEnd(CreateProgram(body, _strict));
		}

		private LocationMarker CreateLocationMarker()
		{
			if (!_extra.Loc.HasValue && _extra.Range.Length == 0)
			{
				return null;
			}
			SkipComment();
			return new LocationMarker(_index, _lineNumber, _lineStart);
		}

		public Program Parse(string code)
		{
			return Parse(code, null);
		}

		public Program Parse(string code, ParserOptions options)
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
				if (options.Comment)
				{
					_extra.Comments = new List<Comment>();
				}
				if (options.Tolerant)
				{
					_extra.Errors = new List<ParserException>();
				}
			}
			try
			{
				Program program = ParseProgram();
				if (_extra.Comments != null)
				{
					program.Comments = _extra.Comments;
				}
				if (_extra.Tokens != null)
				{
					program.Tokens = _extra.Tokens;
				}
				if (_extra.Errors != null)
				{
					program.Errors = _extra.Errors;
					return program;
				}
				return program;
			}
			finally
			{
				_extra = new Extra();
			}
		}

		public FunctionExpression ParseFunctionExpression(string functionExpression)
		{
			_source = functionExpression;
			_index = 0;
			_lineNumber = ((_source.Length > 0) ? 1 : 0);
			_lineStart = 0;
			_length = _source.Length;
			_lookahead = null;
			_state = new State
			{
				AllowIn = true,
				LabelSet = new HashSet<string>(),
				InFunctionBody = true,
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
			_strict = false;
			Peek();
			return ParseFunctionExpression();
		}
	}
}
