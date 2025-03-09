using BalanceService.Core;
using System;

var userRepo = new FakeUserRepository();
var txnHistory = new SimpleTransactionHistory();
var currencyConverter = new FixedRateCurrencyConverter();
var notificationService = new ConsoleNotificationService();

var balanceManager = new BalanceManager(
    userRepo,
    txnHistory,
    currencyConverter,
    notificationService
);

try
{
    balanceManager.ProcessTransaction(1, "CREDIT", 500, "USD");
    balanceManager.ProcessTransaction(1, "DEBIT", 300, "EUR");

    Console.WriteLine(balanceManager.GetTransactionReport(1));
    Console.WriteLine($"Final balance: {userRepo.GetUserBalance(1)} RUB");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

// Реализации вспомогательных классов
public class SimpleTransactionHistory : ITransactionHistory
{
    private readonly Dictionary<int, List<string>> _transactions = new();

    public void AddTransaction(int userId, string type, decimal amount)
    {
        if (!_transactions.ContainsKey(userId))
            _transactions[userId] = new List<string>();

        _transactions[userId].Add($"{type} {amount} RUB");
    }

    public List<string> GetTransactionLog(int userId) =>
        _transactions.TryGetValue(userId, out var log) ? log : new List<string>();
}

public class FixedRateCurrencyConverter : ICurrencyConverter
{
    public decimal Convert(decimal amount, string fromCurrency, string toCurrency) => fromCurrency switch
    {
        "USD" => amount * 75m,
        "EUR" => amount * 100m,
        _ => throw new NotSupportedException("Unsupported currency")
    };
}

public class ConsoleNotificationService : INotificationService
{
    public void SendLowBalanceNotification(int userId, decimal balance) =>
        Console.WriteLine($"ALERT: User {userId} has low balance ({balance} RUB)");
}