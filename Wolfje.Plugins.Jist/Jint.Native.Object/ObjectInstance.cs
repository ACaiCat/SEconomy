using System.Collections.Generic;
using Jint.Runtime;
using Jint.Runtime.Descriptors;

namespace Jint.Native.Object
{
	public class ObjectInstance
	{
		public Engine Engine { get; set; }

		protected IDictionary<string, PropertyDescriptor> Properties { get; private set; }

		public ObjectInstance Prototype { get; set; }

		public bool Extensible { get; set; }

		public virtual string Class => "Object";

		public ObjectInstance(Engine engine)
		{
			Engine = engine;
			Properties = new MruPropertyCache2<string, PropertyDescriptor>();
		}

		public virtual IEnumerable<KeyValuePair<string, PropertyDescriptor>> GetOwnProperties()
		{
			EnsureInitialized();
			return Properties;
		}

		public virtual bool HasOwnProperty(string p)
		{
			EnsureInitialized();
			return Properties.ContainsKey(p);
		}

		public virtual void RemoveOwnProperty(string p)
		{
			EnsureInitialized();
			Properties.Remove(p);
		}

		public virtual JsValue Get(string propertyName)
		{
			PropertyDescriptor property = GetProperty(propertyName);
			if (property == PropertyDescriptor.Undefined)
			{
				return JsValue.Undefined;
			}
			if (property.IsDataDescriptor())
			{
				JsValue? value = property.Value;
				if (!value.HasValue)
				{
					return Undefined.Instance;
				}
				return value.Value;
			}
			JsValue jsValue = (property.Get.HasValue ? property.Get.Value : Undefined.Instance);
			if (jsValue.IsUndefined())
			{
				return Undefined.Instance;
			}
			ICallable callable = jsValue.TryCast<ICallable>();
			return callable.Call(this, Arguments.Empty);
		}

		public virtual PropertyDescriptor GetOwnProperty(string propertyName)
		{
			EnsureInitialized();
			if (Properties.TryGetValue(propertyName, out var value))
			{
				return value;
			}
			return PropertyDescriptor.Undefined;
		}

		protected virtual void SetOwnProperty(string propertyName, PropertyDescriptor desc)
		{
			EnsureInitialized();
			Properties[propertyName] = desc;
		}

		public PropertyDescriptor GetProperty(string propertyName)
		{
			PropertyDescriptor ownProperty = GetOwnProperty(propertyName);
			if (ownProperty != PropertyDescriptor.Undefined)
			{
				return ownProperty;
			}
			if (Prototype == null)
			{
				return PropertyDescriptor.Undefined;
			}
			return Prototype.GetProperty(propertyName);
		}

		public virtual void Put(string propertyName, JsValue value, bool throwOnError)
		{
			if (!CanPut(propertyName))
			{
				if (throwOnError)
				{
					throw new JavaScriptException(Engine.TypeError);
				}
				return;
			}
			PropertyDescriptor ownProperty = GetOwnProperty(propertyName);
			if (ownProperty.IsDataDescriptor())
			{
				ownProperty.Value = value;
				return;
			}
			PropertyDescriptor property = GetProperty(propertyName);
			if (property.IsAccessorDescriptor())
			{
				ICallable callable = property.Set.Value.TryCast<ICallable>();
				callable.Call(new JsValue(this), new JsValue[1] { value });
			}
			else
			{
				PropertyDescriptor desc = new PropertyDescriptor(value, true, true, true);
				DefineOwnProperty(propertyName, desc, throwOnError);
			}
		}

		public bool CanPut(string propertyName)
		{
			PropertyDescriptor ownProperty = GetOwnProperty(propertyName);
			if (ownProperty != PropertyDescriptor.Undefined)
			{
				if (ownProperty.IsAccessorDescriptor())
				{
					if (!ownProperty.Set.HasValue || ownProperty.Set.Value.IsUndefined())
					{
						return false;
					}
					return true;
				}
				if (ownProperty.Writable.HasValue)
				{
					return ownProperty.Writable.Value;
				}
				return false;
			}
			if (Prototype == null)
			{
				return Extensible;
			}
			PropertyDescriptor property = Prototype.GetProperty(propertyName);
			if (property == PropertyDescriptor.Undefined)
			{
				return Extensible;
			}
			if (property.IsAccessorDescriptor())
			{
				if (!property.Set.HasValue || property.Set.Value.IsUndefined())
				{
					return false;
				}
				return true;
			}
			if (!Extensible)
			{
				return false;
			}
			if (property.Writable.HasValue)
			{
				return property.Writable.Value;
			}
			return false;
		}

		public bool HasProperty(string propertyName)
		{
			return GetProperty(propertyName) != PropertyDescriptor.Undefined;
		}

		public virtual bool Delete(string propertyName, bool throwOnError)
		{
			PropertyDescriptor ownProperty = GetOwnProperty(propertyName);
			if (ownProperty == PropertyDescriptor.Undefined)
			{
				return true;
			}
			if (ownProperty.Configurable.HasValue && ownProperty.Configurable.Value)
			{
				RemoveOwnProperty(propertyName);
				return true;
			}
			if (throwOnError)
			{
				throw new JavaScriptException(Engine.TypeError);
			}
			return false;
		}

		public JsValue DefaultValue(Types hint)
		{
			EnsureInitialized();
			if (hint switch
			{
				Types.None => (Class == "Date") ? true : false, 
				Types.String => true, 
				_ => false, 
			})
			{
				ICallable callable = Get("toString").TryCast<ICallable>();
				if (callable != null)
				{
					JsValue result = callable.Call(new JsValue(this), Arguments.Empty);
					if (result.IsPrimitive())
					{
						return result;
					}
				}
				ICallable callable2 = Get("valueOf").TryCast<ICallable>();
				if (callable2 != null)
				{
					JsValue result2 = callable2.Call(new JsValue(this), Arguments.Empty);
					if (result2.IsPrimitive())
					{
						return result2;
					}
				}
				throw new JavaScriptException(Engine.TypeError);
			}
			if (hint == Types.Number || hint == Types.None)
			{
				ICallable callable3 = Get("valueOf").TryCast<ICallable>();
				if (callable3 != null)
				{
					JsValue result3 = callable3.Call(new JsValue(this), Arguments.Empty);
					if (result3.IsPrimitive())
					{
						return result3;
					}
				}
				ICallable callable4 = Get("toString").TryCast<ICallable>();
				if (callable4 != null)
				{
					JsValue result4 = callable4.Call(new JsValue(this), Arguments.Empty);
					if (result4.IsPrimitive())
					{
						return result4;
					}
				}
				throw new JavaScriptException(Engine.TypeError);
			}
			return ToString();
		}

		public virtual bool DefineOwnProperty(string propertyName, PropertyDescriptor desc, bool throwOnError)
		{
			PropertyDescriptor propertyDescriptor = GetOwnProperty(propertyName);
			if (propertyDescriptor == desc)
			{
				return true;
			}
			if (propertyDescriptor == PropertyDescriptor.Undefined)
			{
				if (!Extensible)
				{
					if (throwOnError)
					{
						throw new JavaScriptException(Engine.TypeError);
					}
					return false;
				}
				if (desc.IsGenericDescriptor() || desc.IsDataDescriptor())
				{
					SetOwnProperty(propertyName, new PropertyDescriptor(desc)
					{
						Value = (desc.Value.HasValue ? desc.Value : new JsValue?(JsValue.Undefined)),
						Writable = (desc.Writable.HasValue && desc.Writable.Value),
						Enumerable = (desc.Enumerable.HasValue && desc.Enumerable.Value),
						Configurable = (desc.Configurable.HasValue && desc.Configurable.Value)
					});
				}
				else
				{
					SetOwnProperty(propertyName, new PropertyDescriptor(desc)
					{
						Get = desc.Get,
						Set = desc.Set,
						Enumerable = (desc.Enumerable.HasValue ? desc.Enumerable : new bool?(false)),
						Configurable = (desc.Configurable.HasValue ? desc.Configurable : new bool?(false))
					});
				}
				return true;
			}
			if (!propertyDescriptor.Configurable.HasValue && !propertyDescriptor.Enumerable.HasValue && !propertyDescriptor.Writable.HasValue && !propertyDescriptor.Get.HasValue && !propertyDescriptor.Set.HasValue && !propertyDescriptor.Value.HasValue)
			{
				return true;
			}
			if (propertyDescriptor.Configurable == desc.Configurable && propertyDescriptor.Writable == desc.Writable && propertyDescriptor.Enumerable == desc.Enumerable && ((!propertyDescriptor.Get.HasValue && !desc.Get.HasValue) || (propertyDescriptor.Get.HasValue && desc.Get.HasValue && ExpressionInterpreter.SameValue(propertyDescriptor.Get.Value, desc.Get.Value))) && ((!propertyDescriptor.Set.HasValue && !desc.Set.HasValue) || (propertyDescriptor.Set.HasValue && desc.Set.HasValue && ExpressionInterpreter.SameValue(propertyDescriptor.Set.Value, desc.Set.Value))) && ((!propertyDescriptor.Value.HasValue && !desc.Value.HasValue) || (propertyDescriptor.Value.HasValue && desc.Value.HasValue && ExpressionInterpreter.StrictlyEqual(propertyDescriptor.Value.Value, desc.Value.Value))))
			{
				return true;
			}
			if (!propertyDescriptor.Configurable.HasValue || !propertyDescriptor.Configurable.Value)
			{
				if (desc.Configurable.HasValue && desc.Configurable.Value)
				{
					if (throwOnError)
					{
						throw new JavaScriptException(Engine.TypeError);
					}
					return false;
				}
				if (desc.Enumerable.HasValue && (!propertyDescriptor.Enumerable.HasValue || desc.Enumerable.Value != propertyDescriptor.Enumerable.Value))
				{
					if (throwOnError)
					{
						throw new JavaScriptException(Engine.TypeError);
					}
					return false;
				}
			}
			if (!desc.IsGenericDescriptor())
			{
				if (propertyDescriptor.IsDataDescriptor() != desc.IsDataDescriptor())
				{
					if (!propertyDescriptor.Configurable.HasValue || !propertyDescriptor.Configurable.Value)
					{
						if (throwOnError)
						{
							throw new JavaScriptException(Engine.TypeError);
						}
						return false;
					}
					if (propertyDescriptor.IsDataDescriptor())
					{
						SetOwnProperty(propertyName, propertyDescriptor = new PropertyDescriptor(Undefined.Instance, Undefined.Instance, propertyDescriptor.Enumerable, propertyDescriptor.Configurable));
					}
					else
					{
						SetOwnProperty(propertyName, propertyDescriptor = new PropertyDescriptor(Undefined.Instance, null, propertyDescriptor.Enumerable, propertyDescriptor.Configurable));
					}
				}
				else if (propertyDescriptor.IsDataDescriptor() && desc.IsDataDescriptor())
				{
					if (!propertyDescriptor.Configurable.HasValue || !propertyDescriptor.Configurable.Value)
					{
						if (!propertyDescriptor.Writable.HasValue || (!propertyDescriptor.Writable.Value && desc.Writable.HasValue && desc.Writable.Value))
						{
							if (throwOnError)
							{
								throw new JavaScriptException(Engine.TypeError);
							}
							return false;
						}
						if (!propertyDescriptor.Writable.Value && desc.Value.HasValue && !ExpressionInterpreter.SameValue(desc.Value.Value, propertyDescriptor.Value.Value))
						{
							if (throwOnError)
							{
								throw new JavaScriptException(Engine.TypeError);
							}
							return false;
						}
					}
				}
				else if (propertyDescriptor.IsAccessorDescriptor() && desc.IsAccessorDescriptor() && (!propertyDescriptor.Configurable.HasValue || !propertyDescriptor.Configurable.Value) && ((desc.Set.HasValue && !ExpressionInterpreter.SameValue(desc.Set.Value, propertyDescriptor.Set.HasValue ? propertyDescriptor.Set.Value : Undefined.Instance)) || (desc.Get.HasValue && !ExpressionInterpreter.SameValue(desc.Get.Value, propertyDescriptor.Get.HasValue ? propertyDescriptor.Get.Value : Undefined.Instance))))
				{
					if (throwOnError)
					{
						throw new JavaScriptException(Engine.TypeError);
					}
					return false;
				}
			}
			if (desc.Value.HasValue)
			{
				propertyDescriptor.Value = desc.Value;
			}
			if (desc.Writable.HasValue)
			{
				propertyDescriptor.Writable = desc.Writable;
			}
			if (desc.Enumerable.HasValue)
			{
				propertyDescriptor.Enumerable = desc.Enumerable;
			}
			if (desc.Configurable.HasValue)
			{
				propertyDescriptor.Configurable = desc.Configurable;
			}
			if (desc.Get.HasValue)
			{
				propertyDescriptor.Get = desc.Get;
			}
			if (desc.Set.HasValue)
			{
				propertyDescriptor.Set = desc.Set;
			}
			return true;
		}

		public void FastAddProperty(string name, JsValue value, bool writable, bool enumerable, bool configurable)
		{
			SetOwnProperty(name, new PropertyDescriptor(value, writable, enumerable, configurable));
		}

		public void FastSetProperty(string name, PropertyDescriptor value)
		{
			SetOwnProperty(name, value);
		}

		protected virtual void EnsureInitialized()
		{
		}

		public override string ToString()
		{
			return TypeConverter.ToString(this);
		}
	}
}
