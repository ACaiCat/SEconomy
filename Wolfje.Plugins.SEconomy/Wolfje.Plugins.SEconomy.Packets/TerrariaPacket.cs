using System.Runtime.InteropServices;

namespace Wolfje.Plugins.SEconomy.Packets
{
	public struct TerrariaPacket
	{
		public int Length;

		private byte MessageType;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 65526)]
		public byte[] MessagePayload;

		public PacketTypes PacketType => (PacketTypes)MessageType;
	}
}
