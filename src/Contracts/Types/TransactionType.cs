namespace Calendar.Contracts.Types;
/// <summary>
/// Тип транзакции
/// </summary>
public enum TransactionType
{
    Deposit = 1, // Пополнение
    Withdrawal = 2, // Списание
    Refund = 3, // Возврат
    Bonus = 4 // Бонус
}