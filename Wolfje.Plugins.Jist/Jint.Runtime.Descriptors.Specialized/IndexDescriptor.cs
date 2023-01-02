using System;
using System.Globalization;
using System.Reflection;
using Jint.Native;

namespace Jint.Runtime.Descriptors.Specialized
{
	public sealed class IndexDescriptor : PropertyDescriptor
	{
		private readonly Engine _engine;

		private readonly object _key;

		private readonly object _item;

		private readonly PropertyInfo _indexer;

		private readonly MethodInfo _containsKey;

		public override JsValue? Value
		{
			get
			{
				MethodInfo getMethod = _indexer.GetGetMethod();
				if (getMethod == null)
				{
					throw new InvalidOperationException("Indexer has no public getter.");
				}
				object[] parameters = new object[1] { _key };
				if (_containsKey != null && _containsKey.Invoke(_item, parameters) as bool? != true)
				{
					return JsValue.Undefined;
				}
				try
				{
					return JsValue.FromObject(_engine, getMethod.Invoke(_item, parameters));
				}
				catch
				{
					return JsValue.Undefined;
				}
			}
			set
			{
				MethodInfo setMethod = _indexer.GetSetMethod();
				if (setMethod == null)
				{
					throw new InvalidOperationException("Indexer has no public setter.");
				}
				object[] parameters = new object[2]
				{
					_key,
					value.HasValue ? value.Value.ToObject() : null
				};
				setMethod.Invoke(_item, parameters);
			}
		}

		public IndexDescriptor(Engine engine, Type targetType, string key, object item)
		{
			_engine = engine;
			_item = item;
			PropertyInfo[] properties = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			PropertyInfo[] array = properties;
			PropertyInfo[] array2 = array;
			foreach (PropertyInfo propertyInfo in array2)
			{
				if (propertyInfo.GetIndexParameters().Length == 1 && (propertyInfo.GetGetMethod() != null || propertyInfo.GetSetMethod() != null))
				{
					Type parameterType = propertyInfo.GetIndexParameters()[0].ParameterType;
					if (_engine.ClrTypeConverter.TryConvert(key, parameterType, CultureInfo.InvariantCulture, out _key))
					{
						_indexer = propertyInfo;
						_containsKey = targetType.GetMethod("ContainsKey", new Type[1] { parameterType });
						break;
					}
				}
			}
			if (_indexer == null)
			{
				throw new InvalidOperationException("No matching indexer found.");
			}
			base.Writable = true;
		}

		public IndexDescriptor(Engine engine, string key, object item)
			: this(engine, item.GetType(), key, item)
		{
		}
	}
}
