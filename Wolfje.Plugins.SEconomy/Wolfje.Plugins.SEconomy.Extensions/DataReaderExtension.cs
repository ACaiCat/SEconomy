using System;
using System.Collections.Generic;
using System.Data;

namespace Wolfje.Plugins.SEconomy.Extensions
{
	public static class DataReaderExtension
	{
		public class EnumeratorWrapper<T>
		{
			private readonly Func<bool> moveNext;

			private readonly Func<T> current;

			public T Current => current();

			public EnumeratorWrapper(Func<bool> moveNext, Func<T> current)
			{
				this.moveNext = moveNext;
				this.current = current;
			}

			public EnumeratorWrapper<T> GetEnumerator()
			{
				return this;
			}

			public bool MoveNext()
			{
				return moveNext();
			}
		}

		private static IEnumerable<T> BuildEnumerable<T>(Func<bool> moveNext, Func<T> current)
		{
			EnumeratorWrapper<T> enumeratorWrapper = new EnumeratorWrapper<T>(moveNext, current);
			foreach (T item in enumeratorWrapper)
			{
				yield return item;
			}
		}

		public static IEnumerable<T> AsEnumerable<T>(this T source) where T : IDataReader
		{
			return BuildEnumerable(((IDataReader)source).Read, () => source);
		}
	}
}
