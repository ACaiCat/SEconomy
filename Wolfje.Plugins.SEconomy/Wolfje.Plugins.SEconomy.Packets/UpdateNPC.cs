using System.Runtime.InteropServices;

namespace Wolfje.Plugins.SEconomy.Packets
{
	public struct UpdateNPC
	{
		public short NPCSlot;

		public float PositionX;

		public float PositionY;

		public float VelocityX;

		public float VelocityY;

		public short TargetPlayerID;

		public FacingDirectionX FacingDirectionX;

		public FacingDirectionY FacingDirectionY;

		public int Life;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		private int[] AI;

		public short Type;
	}
}
