using System.Collections.Generic;
using System.Linq;

namespace Jint.Runtime.CallStack
{
	public class JintCallStack
	{
		private Stack<CallStackElement> _stack = new Stack<CallStackElement>();

		private Dictionary<CallStackElement, int> _statistics = new Dictionary<CallStackElement, int>(new CallStackElementComparer());

		public int Push(CallStackElement item)
		{
			_stack.Push(item);
			if (_statistics.ContainsKey(item))
			{
				return ++_statistics[item];
			}
			_statistics.Add(item, 0);
			return 0;
		}

		public CallStackElement Pop()
		{
			CallStackElement callStackElement = _stack.Pop();
			if (_statistics[callStackElement] == 0)
			{
				_statistics.Remove(callStackElement);
			}
			else
			{
				_statistics[callStackElement]--;
			}
			return callStackElement;
		}

		public void Clear()
		{
			_stack.Clear();
			_statistics.Clear();
		}

		public override string ToString()
		{
			return string.Join("->", _stack.Select((CallStackElement cse) => cse.ToString()).Reverse());
		}
	}
}
