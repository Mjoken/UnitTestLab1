namespace BalanceService.Core;

public interface IUserRepository
{
    decimal GetUserBalance(int userId);
    void UpdateUserBalance(int userId, decimal newBalance);
}