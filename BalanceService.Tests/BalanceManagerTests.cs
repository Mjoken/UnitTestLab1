using Xunit;
using Moq;
using BalanceService.Core;
using System;
using System.Collections.Generic;

namespace BalanceService.Tests;

public class BalanceManagerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ITransactionHistory> _txnHistoryMock = new();
    private readonly Mock<ICurrencyConverter> _currencyConverterMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly BalanceManager _balanceManager;

    public BalanceManagerTests()
    {
        _balanceManager = new BalanceManager(
            _userRepoMock.Object,
            _txnHistoryMock.Object,
            _currencyConverterMock.Object,
            _notificationMock.Object
        );
    }

    // 1. Тест на корректное списание
    [Fact]
    public void ProcessTransaction_DebitValidAmount_UpdatesBalance()
    {
        // Arrange
        const int userId = 1;
        _userRepoMock.Setup(r => r.GetUserBalance(userId)).Returns(500m);

        // Act
        _balanceManager.ProcessTransaction(userId, "DEBIT", 300m);

        // Assert
        _userRepoMock.Verify(r => r.UpdateUserBalance(userId, 200m), Times.Once);
    }

    // 2. Тест на конвертацию валюты
    [Theory]
    [InlineData("USD", 75, 7500)]
    [InlineData("EUR", 100, 10000)]
    public void ProcessTransaction_ConvertsCurrency(string currency, decimal rate, decimal expectedRub)
    {
        // Arrange
        const int userId = 2;
        _userRepoMock.Setup(r => r.GetUserBalance(userId)).Returns(20000m);
        _currencyConverterMock.Setup(c => c.Convert(100, currency, "RUB")).Returns(rate * 100);

        // Act
        _balanceManager.ProcessTransaction(userId, "CREDIT", 100, currency);

        // Assert
        _txnHistoryMock.Verify(h => h.AddTransaction(userId, "CREDIT", expectedRub), Times.Once);
    }

    // 3. Тест на уведомление при низком балансе
    [Fact]
    public void ProcessTransaction_LowBalance_SendsNotification()
    {
        // Arrange
        const int userId = 3;
        _userRepoMock.Setup(r => r.GetUserBalance(userId)).Returns(150m);

        // Act
        _balanceManager.ProcessTransaction(userId, "DEBIT", 100m);

        // Assert
        _notificationMock.Verify(n => n.SendLowBalanceNotification(userId, 50m), Times.Once);
    }

    // 4. Тест на некорректный ввод данных
    [Theory]
    [InlineData(0, 100)]   // userId = 0
    [InlineData(1, -50)]   // amount < 0
    [InlineData(-5, 100)]  // userId < 0
    public void ProcessTransaction_InvalidInput_ThrowsException(int userId, decimal amount)
    {
        Assert.Throws<ArgumentException>(() =>
            _balanceManager.ProcessTransaction(userId, "CREDIT", amount)
        );
    }

    // 5. Тест на несколько транзакций
    [Fact]
    public void ProcessTransaction_MultipleTransactions_UpdatesBalanceCorrectly()
    {
        // Arrange
        const int userId = 4;
        _userRepoMock.SetupSequence(r => r.GetUserBalance(userId))
            .Returns(1000m)
            .Returns(1200m)
            .Returns(900m);

        // Act
        _balanceManager.ProcessTransaction(userId, "CREDIT", 200m);
        _balanceManager.ProcessTransaction(userId, "DEBIT", 300m);

        // Assert
        _userRepoMock.Verify(r => r.UpdateUserBalance(userId, 1200m), Times.Once);
        _userRepoMock.Verify(r => r.UpdateUserBalance(userId, 900m), Times.Once);
    }

    // 6. Тест на списание всего баланса
    [Fact]
    public void ProcessTransaction_DebitFullBalance_Succeeds()
    {
        // Arrange
        const int userId = 5;
        _userRepoMock.Setup(r => r.GetUserBalance(userId)).Returns(500m);

        // Act
        _balanceManager.ProcessTransaction(userId, "DEBIT", 500m);

        // Assert
        _userRepoMock.Verify(r => r.UpdateUserBalance(userId, 0m), Times.Once);
    }

    // 7. Тест на неподдерживаемую валюту
    [Fact]
    public void ProcessTransaction_UnsupportedCurrency_ThrowsException()
    {
        // Arrange
        _currencyConverterMock
            .Setup(c => c.Convert(100, "XYZ", "RUB"))
            .Throws(new NotSupportedException());

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            _balanceManager.ProcessTransaction(1, "CREDIT", 100, "XYZ")
        );
    }

    // 8. Тест на историю транзакций
    [Fact]
    public void GetTransactionReport_ReturnsFormattedLog()
    {
        // Arrange
        const int userId = 6;
        var transactions = new List<string> { "CREDIT 1000 RUB", "DEBIT 500 RUB" };
        _txnHistoryMock.Setup(h => h.GetTransactionLog(userId)).Returns(transactions);

        // Act
        var report = _balanceManager.GetTransactionReport(userId);

        // Assert
        Assert.Contains("CREDIT 1000 RUB", report);
        Assert.Contains("DEBIT 500 RUB", report);
    }

    // 9. Тест на повторное уведомление
    [Fact]
    public void ProcessTransaction_AlreadyLowBalance_NoDuplicateNotification()
    {
        // Arrange
        const int userId = 7;
        _userRepoMock.Setup(r => r.GetUserBalance(userId)).Returns(50m);

        // Act
        _balanceManager.ProcessTransaction(userId, "DEBIT", 10m); // Новый баланс: 40

        // Assert
        _notificationMock.Verify(n => n.SendLowBalanceNotification(userId, 40m), Times.Once);
    }

    // 10. Тест на несуществующего пользователя
    [Fact]
    public void GetTransactionReport_NonExistentUser_ReturnsEmptyLog()
    {
        // Arrange
        const int userId = 999;
        _txnHistoryMock.Setup(h => h.GetTransactionLog(userId)).Returns(new List<string>());

        // Act
        var report = _balanceManager.GetTransactionReport(userId);

        // Assert
        Assert.Equal($"Transaction report for user {userId}:\n", report);
    }
}