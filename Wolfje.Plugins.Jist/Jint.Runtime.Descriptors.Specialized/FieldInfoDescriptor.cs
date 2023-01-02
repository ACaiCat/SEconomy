using System.Globalization;
using System.Reflection;
using Jint.Native;

namespace Jint.Runtime.Descriptors.Specialized
{
	public sealed class FieldInfoDescriptor : PropertyDescriptor
	{
		private readonly Engine _engine;

		private readonly FieldInfo _fieldInfo;

		private readonly object _item;

		public override JsValue? Value
		{
			get
			{
				return JsValue.FromObject(_engine, _fieldInfo.GetValue(_item));
			}
			set
			{
				JsValue valueOrDefault = value.GetValueOrDefault();
				object obj;
				if (_fieldInfo.FieldType == typeof(JsValue))
				{
					obj = valueOrDefault;
				}
				else
				{
					obj = valueOrDefault.ToObject();
					if (obj.GetType() != _fieldInfo.FieldType)
					{
						obj = _engine.ClrTypeConverter.Convert(obj, _fieldInfo.FieldType, CultureInfo.InvariantCulture);
					}
				}
				_fieldInfo.SetValue(_item, obj);
			}
		}

		public FieldInfoDescriptor(Engine engine, FieldInfo fieldInfo, object item)
		{
			_engine = engine;
			_fieldInfo = fieldInfo;
			_item = item;
			base.Writable = !fieldInfo.Attributes.HasFlag(FieldAttributes.InitOnly);
		}
	}
}
