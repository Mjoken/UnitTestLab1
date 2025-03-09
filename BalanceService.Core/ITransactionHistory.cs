namespace BalanceService.Core;

public interface ITransactionHistory
{
    void AddTransaction(int userId, string type, decimal amount);
    List<string> GetTransactionLog(int userId);
}