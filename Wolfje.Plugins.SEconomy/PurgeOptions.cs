using System;

[Flags]
public enum PurgeOptions
{
	RemoveOrphanedAccounts = 1,
	RemoveZeroBalanceAccounts = 2
}
