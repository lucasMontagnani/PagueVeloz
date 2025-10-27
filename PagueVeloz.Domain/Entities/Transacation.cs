using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PagueVeloz.Domain.Entities
{
    public class Transaction
    {
        public Guid TransactionId { get; private set; } = Guid.NewGuid();
        public Guid ReferenceId { get; private set; }
        public TransactionOperation Operation { get; private set; }
        public double Amount { get; private set; }
        public string Currency { get; private set; } = "BRL";
        public TransactionStatus Status { get; private set; }
        public string? ErrorMessage { get; private set; }
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
        public string? Description { get; private set; }

        [JsonIgnore]
        public virtual Account? Account { get; set; } // origem
        [ForeignKey(nameof(Account))]
        public Guid AccountId { get; set; }

        [JsonIgnore]
        public virtual Account? DestinationAccount { get; set; } // destino
        [ForeignKey(nameof(DestinationAccount))]
        public Guid? DestinationAccountId { get; set; }

        protected Transaction() { }

        public Transaction(Guid referenceId, Guid accountId, TransactionOperation operation, double amount, string currency, string? description = null, Guid? destinationAccountId = null)
        {
            ReferenceId = referenceId;
            AccountId = accountId;
            Operation = operation;
            Amount = amount;
            Currency = currency;
            Description = description;
            Status = TransactionStatus.Pending;
            DestinationAccountId = destinationAccountId;
        }

        public void MarkSuccess() => Status = TransactionStatus.Success;

        public void MarkFailed(string error)
        {
            Status = TransactionStatus.Failed;
            ErrorMessage = error;
        }
    }

    public enum TransactionOperation
    {
        Credit,
        Debit,
        Reserve,
        Capture,
        Reversal,
        Transfer
    }

    public enum TransactionStatus
    {
        Pending,
        Success,
        Failed
    }
}
