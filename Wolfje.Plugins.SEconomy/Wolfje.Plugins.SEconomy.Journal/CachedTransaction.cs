namespace Wolfje.Plugins.SEconomy.Journal
{
	public class CachedTransaction
	{
		public long SourceBankAccountK { get; set; }

		public long DestinationBankAccountK { get; set; }

		public Money Amount { get; set; }

		public string Message { get; set; }

		public BankAccountTransferOptions Options { get; set; }

		public int Aggregations { get; set; }

		public CachedTransaction()
		{
			Aggregations = 1;
		}
	}
}
