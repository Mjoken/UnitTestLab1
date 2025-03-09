using System;
using System.Collections.Generic;

namespace BalanceService.Core;

public class BalanceManager
{
    private readonly IUserRepository _userRepository;
    private readonly ITransactionHistory _transactionHistory;
    private readonly ICurrencyConverter _currencyConverter;
    private readonly INotificationService _notificationService;
    private const decimal LowBalanceThreshold = 100m;

    public BalanceManager(
        IUserRepository userRepository,
        ITransactionHistory transactionHistory,
        ICurrencyConverter currencyConverter,
        INotificationService notificationService)
    {
        _userRepository = userRepository;
        _transactionHistory = transactionHistory;
        _currencyConverter = currencyConverter;
        _notificationService = notificationService;
    }

    public void ProcessTransaction(int userId, string transactionType, decimal amount, string currency = "RUB")
    {
        ValidateInput(userId, amount);

        decimal convertedAmount = currency == "RUB"
            ? amount
            : _currencyConverter.Convert(amount, currency, "RUB");

        decimal currentBalance = _userRepository.GetUserBalance(userId);

        if (transactionType == "DEBIT" && currentBalance < convertedAmount)
            throw new InvalidOperationException("Insufficient balance");

        decimal newBalance = transactionType == "CREDIT"
            ? currentBalance + convertedAmount
            : currentBalance - convertedAmount;

        _userRepository.UpdateUserBalance(userId, newBalance);
        _transactionHistory.AddTransaction(userId, transactionType, convertedAmount);

        if (newBalance < LowBalanceThreshold)
            _notificationService.SendLowBalanceNotification(userId, newBalance);
    }

    public string GetTransactionReport(int userId)
    {
        var transactions = _transactionHistory.GetTransactionLog(userId);
        return $"Transaction report for user {userId}:\n{string.Join("\n", transactions)}";
    }

    private void ValidateInput(int userId, decimal amount)
    {
        if (userId <= 0 || amount <= 0)
            throw new ArgumentException("Invalid input parameters");
    }
}