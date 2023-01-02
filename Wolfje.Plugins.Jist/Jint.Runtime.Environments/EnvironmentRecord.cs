using Jint.Native;
using Jint.Native.Object;

namespace Jint.Runtime.Environments
{
	public abstract class EnvironmentRecord : ObjectInstance
	{
		protected EnvironmentRecord(Engine engine)
			: base(engine)
		{
		}

		public abstract bool HasBinding(string name);

		public abstract void CreateMutableBinding(string name, bool canBeDeleted = false);

		public abstract void SetMutableBinding(string name, JsValue value, bool strict);

		public abstract JsValue GetBindingValue(string name, bool strict);

		public abstract bool DeleteBinding(string name);

		public abstract JsValue ImplicitThisValue();

		public abstract string[] GetAllBindingNames();
	}
}
