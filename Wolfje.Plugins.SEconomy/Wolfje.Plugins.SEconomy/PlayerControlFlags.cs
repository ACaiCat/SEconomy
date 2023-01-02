using System;

namespace Wolfje.Plugins.SEconomy
{
	[Flags]
	public enum PlayerControlFlags : byte
	{
		Idle = 0,
		DownPressed = 1,
		LeftPressed = 2,
		RightPressed = 4,
		JumpPressed = 8,
		UseItemPressed = 0x10,
		DirectionFacingRight = 0x20
	}
}
