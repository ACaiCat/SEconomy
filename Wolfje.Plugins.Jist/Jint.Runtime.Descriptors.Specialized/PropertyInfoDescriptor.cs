using System.Globalization;
using System.Reflection;
using Jint.Native;

namespace Jint.Runtime.Descriptors.Specialized
{
	public sealed class PropertyInfoDescriptor : PropertyDescriptor
	{
		private readonly Engine _engine;

		private readonly PropertyInfo _propertyInfo;

		private readonly object _item;

		public override JsValue? Value
		{
			get
			{
				return JsValue.FromObject(_engine, _propertyInfo.GetValue(_item, null));
			}
			set
			{
				JsValue valueOrDefault = value.GetValueOrDefault();
				object obj;
				if (_propertyInfo.PropertyType == typeof(JsValue))
				{
					obj = valueOrDefault;
				}
				else
				{
					obj = valueOrDefault.ToObject();
					if (obj != null && obj.GetType() != _propertyInfo.PropertyType)
					{
						obj = _engine.ClrTypeConverter.Convert(obj, _propertyInfo.PropertyType, CultureInfo.InvariantCulture);
					}
				}
				_propertyInfo.SetValue(_item, obj, null);
			}
		}

		public PropertyInfoDescriptor(Engine engine, PropertyInfo propertyInfo, object item)
		{
			_engine = engine;
			_propertyInfo = propertyInfo;
			_item = item;
			base.Writable = propertyInfo.CanWrite;
		}
	}
}
