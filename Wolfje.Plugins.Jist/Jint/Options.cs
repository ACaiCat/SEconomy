using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Jint.Runtime.Interop;

namespace Jint
{
	public class Options
	{
		private bool _discardGlobal;

		private bool _strict;

		private bool _allowDebuggerStatement;

		private bool _debugMode;

		private bool _allowClr;

		private readonly List<IObjectConverter> _objectConverters = new List<IObjectConverter>();

		private int _maxStatements;

		private int _maxRecursionDepth = -1;

		private TimeSpan _timeoutInterval;

		private CultureInfo _culture = CultureInfo.CurrentCulture;

		private TimeZoneInfo _localTimeZone = TimeZoneInfo.Local;

		private List<Assembly> _lookupAssemblies = new List<Assembly>();

		internal bool _IsGlobalDiscarded => _discardGlobal;

		internal bool _IsStrict => _strict;

		internal bool _IsDebuggerStatementAllowed => _allowDebuggerStatement;

		internal bool _IsDebugMode => _debugMode;

		internal bool _IsClrAllowed => _allowClr;

		internal IList<Assembly> _LookupAssemblies => _lookupAssemblies;

		internal IEnumerable<IObjectConverter> _ObjectConverters => _objectConverters;

		internal int _MaxStatements => _maxStatements;

		internal int _MaxRecursionDepth => _maxRecursionDepth;

		internal TimeSpan _TimeoutInterval => _timeoutInterval;

		internal CultureInfo _Culture => _culture;

		internal TimeZoneInfo _LocalTimeZone => _localTimeZone;

		public Options DiscardGlobal(bool discard = true)
		{
			_discardGlobal = discard;
			return this;
		}

		public Options Strict(bool strict = true)
		{
			_strict = strict;
			return this;
		}

		public Options AllowDebuggerStatement(bool allowDebuggerStatement = true)
		{
			_allowDebuggerStatement = allowDebuggerStatement;
			return this;
		}

		public Options DebugMode(bool debugMode = true)
		{
			_debugMode = debugMode;
			return this;
		}

		public Options AddObjectConverter(IObjectConverter objectConverter)
		{
			_objectConverters.Add(objectConverter);
			return this;
		}

		public Options AllowClr(params Assembly[] assemblies)
		{
			_allowClr = true;
			_lookupAssemblies.AddRange(assemblies);
			_lookupAssemblies = _lookupAssemblies.Distinct().ToList();
			return this;
		}

		public Options MaxStatements(int maxStatements = 0)
		{
			_maxStatements = maxStatements;
			return this;
		}

		public Options TimeoutInterval(TimeSpan timeoutInterval)
		{
			_timeoutInterval = timeoutInterval;
			return this;
		}

		public Options LimitRecursion(int maxRecursionDepth = 0)
		{
			_maxRecursionDepth = maxRecursionDepth;
			return this;
		}

		public Options Culture(CultureInfo cultureInfo)
		{
			_culture = cultureInfo;
			return this;
		}

		public Options LocalTimeZone(TimeZoneInfo timeZoneInfo)
		{
			_localTimeZone = timeZoneInfo;
			return this;
		}
	}
}
