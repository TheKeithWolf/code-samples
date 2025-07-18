using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent; 

public class WithdrawalService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPayPalApiClient _paypalApiClient;
    private readonly ILogger<WithdrawalService> _logger;

    public WithdrawalService(IAccountRepository accountRepository, IPayPalApiClient paypalApiClient, ILogger<WithdrawalService> logger)
    {
        _accountRepository = accountRepository;
        _paypalApiClient = paypalApiClient;
        _logger = logger;
    }

    public async Task<bool> Withdraw(string accountId, decimal amount, string paypalEmail)
    {
        if (amount <= 0)
        {
            _logger.LogError($"Withdrawal amount must be positive. Amount: {amount:C}");
            return false;
        }

        _logger.LogInformation($"Initiating withdrawal for account '{accountId}', amount {amount:C} to {paypalEmail}...");

        decimal currentBalance = await _accountRepository.GetBalance(accountId);
        if (currentBalance < amount)
        {
            _logger.LogWarning($"Withdrawal failed for account '{accountId}': Insufficient funds. Current balance: {currentBalance:C}, Requested: {amount:C}");
            return false;
        }

        bool debitSuccess = await _accountRepository.DebitBalance(accountId, amount);
        if (!debitSuccess)
        {
            _logger.LogError($"Failed to debit account '{accountId}' for {amount:C}. Aborting withdrawal.");
            return false;
        }

        bool paypalTransferSuccess = await _paypalApiClient.TransferFunds(paypalEmail, amount);

        if (!paypalTransferSuccess)
        {
            _logger.LogError($"PayPal transfer failed for account '{accountId}', amount {amount:C} to {paypalEmail}. Reverting debit.");
            await _accountRepository.UpdateBalance(accountId, currentBalance); 
            _logger.LogInformation($"Debit reverted for account '{accountId}'. Balance restored to {currentBalance:C}.");
            return false;
        }

        _logger.LogInformation($"Withdrawal completed successfully for account '{accountId}', amount {amount:C} to {paypalEmail}.");
        return true;
    }
}