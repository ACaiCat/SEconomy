using System;
using System.Collections.Generic;

namespace Jint.Parser
{
	public static class ParserExtensions
	{
		public static string Slice(this string source, int start, int end)
		{
			return source.Substring(start, Math.Min(source.Length, end) - start);
		}

		public static char CharCodeAt(this string source, int index)
		{
			if (index < 0 || index > source.Length - 1)
			{
				return '\0';
			}
			return source[index];
		}

		public static T Pop<T>(this List<T> list)
		{
			int index = list.Count - 1;
			T result = list[index];
			list.RemoveAt(index);
			return result;
		}

		public static void Push<T>(this List<T> list, T item)
		{
			list.Add(item);
		}
	}
}
