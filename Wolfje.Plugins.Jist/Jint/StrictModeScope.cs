using System;

namespace Jint
{
	public class StrictModeScope : IDisposable
	{
		private readonly bool _strict;

		private readonly bool _force;

		private readonly int _forcedRefCount;

		[ThreadStatic]
		private static int _refCount;

		public static bool IsStrictModeCode => _refCount > 0;

		public static int RefCount
		{
			get
			{
				return _refCount;
			}
			set
			{
				_refCount = value;
			}
		}

		public StrictModeScope(bool strict = true, bool force = false)
		{
			_strict = strict;
			_force = force;
			if (_force)
			{
				_forcedRefCount = _refCount;
				_refCount = 0;
			}
			if (_strict)
			{
				_refCount++;
			}
		}

		public void Dispose()
		{
			if (_strict)
			{
				_refCount--;
			}
			if (_force)
			{
				_refCount = _forcedRefCount;
			}
		}
	}
}
