namespace BalanceService.Core;

public class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<int, decimal> _balances = new();

    public decimal GetUserBalance(int userId)
    {
        return _balances.TryGetValue(userId, out var balance) ? balance : 0;
    }

    public void UpdateUserBalance(int userId, decimal newBalance)
    {
        _balances[userId] = newBalance;
    }
}