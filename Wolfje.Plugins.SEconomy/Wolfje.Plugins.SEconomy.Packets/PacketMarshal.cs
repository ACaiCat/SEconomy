using System;
using System.Runtime.InteropServices;

namespace Wolfje.Plugins.SEconomy.Packets
{
	internal static class PacketMarshal
	{
		public static T MarshalFromBuffer<T>(byte[] buffer) where T : struct
		{
			int num = Marshal.SizeOf((object)new T());
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			try
			{
				Marshal.Copy(buffer, 0, intPtr, num);
				return (T)Marshal.PtrToStructure(intPtr, typeof(T));
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}
	}
}
