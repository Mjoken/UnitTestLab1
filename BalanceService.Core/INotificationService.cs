namespace BalanceService.Core;

public interface INotificationService
{
    void SendLowBalanceNotification(int userId, decimal balance);
}