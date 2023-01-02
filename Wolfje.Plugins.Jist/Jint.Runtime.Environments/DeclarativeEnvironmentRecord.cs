using System.Collections.Generic;
using System.Linq;
using Jint.Native;

namespace Jint.Runtime.Environments
{
	public sealed class DeclarativeEnvironmentRecord : EnvironmentRecord
	{
		private readonly Engine _engine;

		private readonly IDictionary<string, Binding> _bindings = new Dictionary<string, Binding>();

		public DeclarativeEnvironmentRecord(Engine engine)
			: base(engine)
		{
			_engine = engine;
		}

		public override bool HasBinding(string name)
		{
			return _bindings.ContainsKey(name);
		}

		public override void CreateMutableBinding(string name, bool canBeDeleted = false)
		{
			_bindings.Add(name, new Binding
			{
				Value = Undefined.Instance,
				CanBeDeleted = canBeDeleted,
				Mutable = true
			});
		}

		public override void SetMutableBinding(string name, JsValue value, bool strict)
		{
			Binding binding = _bindings[name];
			if (binding.Mutable)
			{
				binding.Value = value;
			}
			else if (strict)
			{
				throw new JavaScriptException(_engine.TypeError, "Can't update the value of an immutable binding.");
			}
		}

		public override JsValue GetBindingValue(string name, bool strict)
		{
			Binding binding = _bindings[name];
			if (!binding.Mutable && binding.Value == Undefined.Instance)
			{
				if (strict)
				{
					throw new JavaScriptException(_engine.ReferenceError, "Can't access anm uninitiazed immutable binding.");
				}
				return Undefined.Instance;
			}
			return binding.Value;
		}

		public override bool DeleteBinding(string name)
		{
			if (!_bindings.TryGetValue(name, out var value))
			{
				return true;
			}
			if (!value.CanBeDeleted)
			{
				return false;
			}
			_bindings.Remove(name);
			return true;
		}

		public override JsValue ImplicitThisValue()
		{
			return Undefined.Instance;
		}

		public void CreateImmutableBinding(string name)
		{
			_bindings.Add(name, new Binding
			{
				Value = Undefined.Instance,
				Mutable = false,
				CanBeDeleted = false
			});
		}

		public void InitializeImmutableBinding(string name, JsValue value)
		{
			Binding binding = _bindings[name];
			binding.Value = value;
		}

		public override string[] GetAllBindingNames()
		{
			return _bindings.Keys.ToArray();
		}
	}
}
