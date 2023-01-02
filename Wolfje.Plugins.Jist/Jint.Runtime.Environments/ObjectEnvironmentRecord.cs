using System.Collections.Generic;
using System.Linq;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;

namespace Jint.Runtime.Environments
{
	public sealed class ObjectEnvironmentRecord : EnvironmentRecord
	{
		private readonly Engine _engine;

		private readonly ObjectInstance _bindingObject;

		private readonly bool _provideThis;

		public ObjectEnvironmentRecord(Engine engine, ObjectInstance bindingObject, bool provideThis)
			: base(engine)
		{
			_engine = engine;
			_bindingObject = bindingObject;
			_provideThis = provideThis;
		}

		public override bool HasBinding(string name)
		{
			return _bindingObject.HasProperty(name);
		}

		public override void CreateMutableBinding(string name, bool configurable = true)
		{
			_bindingObject.DefineOwnProperty(name, new PropertyDescriptor(Undefined.Instance, true, true, configurable), throwOnError: true);
		}

		public override void SetMutableBinding(string name, JsValue value, bool strict)
		{
			_bindingObject.Put(name, value, strict);
		}

		public override JsValue GetBindingValue(string name, bool strict)
		{
			if (!_bindingObject.HasProperty(name))
			{
				if (!strict)
				{
					return Undefined.Instance;
				}
				throw new JavaScriptException(_engine.ReferenceError);
			}
			return _bindingObject.Get(name);
		}

		public override bool DeleteBinding(string name)
		{
			return _bindingObject.Delete(name, throwOnError: false);
		}

		public override JsValue ImplicitThisValue()
		{
			if (_provideThis)
			{
				return new JsValue(_bindingObject);
			}
			return Undefined.Instance;
		}

		public override string[] GetAllBindingNames()
		{
			if (_bindingObject != null)
			{
				return _bindingObject.GetOwnProperties().Select(delegate(KeyValuePair<string, PropertyDescriptor> x)
				{
					KeyValuePair<string, PropertyDescriptor> keyValuePair = x;
					return keyValuePair.Key;
				}).ToArray();
			}
			return new string[0];
		}
	}
}
