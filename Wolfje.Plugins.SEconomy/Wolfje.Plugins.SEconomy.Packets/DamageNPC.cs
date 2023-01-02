using System.Runtime.InteropServices;

namespace Wolfje.Plugins.SEconomy.Packets
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DamageNPC
	{
		[MarshalAs(UnmanagedType.I2)]
		public short NPCID;

		[MarshalAs(UnmanagedType.I2)]
		public short Damage;

		[MarshalAs(UnmanagedType.R4)]
		public float Knockback;

		[MarshalAs(UnmanagedType.I1)]
		public byte Direction;

		[MarshalAs(UnmanagedType.I1)]
		public byte CrititcalHit;
	}
}
