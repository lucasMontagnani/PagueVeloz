using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PagueVeloz.Domain.Entities
{
    public class Account
    {
        public Guid AccountId { get; private set; } = Guid.NewGuid();
        public double AvailableBalance { get; private set; }
        public double ReservedBalance { get; private set; }
        public double CreditLimit { get; private set; }
        public AccountStatus Status { get; private set; } = AccountStatus.Active;
        //public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

        [JsonIgnore]
        public virtual Client? Client { get; set; }
        [ForeignKey("Client")]
        public Guid ClientId { get; set; }

        [JsonIgnore]
        public virtual ICollection<Transaction>? SentTransactions { get; set; }
        public virtual ICollection<Transaction>? ReceivedTransactions { get; set; }

        protected Account() { }

        public Account(Guid clientId, double initialBalance, double creditLimit)
        {
            ClientId = clientId;
            AvailableBalance = initialBalance;
            CreditLimit = creditLimit;
        }

        public void Credit(double amount)
        {
            EnsureActive();
            AvailableBalance += amount;
        }

        public void Debit(double amount)
        {
            EnsureActive();
            if (AvailableBalance + CreditLimit < amount)
                throw new InvalidOperationException("Saldo insuficiente para débito.");
            AvailableBalance -= amount;
        }

        public void Reserve(double amount)
        {
            EnsureActive();
            if (AvailableBalance < amount)
                throw new InvalidOperationException("Saldo disponível insuficiente para reserva.");
            AvailableBalance -= amount;
            ReservedBalance += amount;
        }

        public void Capture(double amount)
        {
            EnsureActive();
            if (ReservedBalance < amount)
                throw new InvalidOperationException("Saldo reservado insuficiente para captura.");
            ReservedBalance -= amount;
        }

        public void RevertReserve(double amount)
        {
            EnsureActive();
            if (ReservedBalance < amount)
                throw new InvalidOperationException("Saldo reservado insuficiente para reversão.");
            ReservedBalance -= amount;
            AvailableBalance += amount;
        }

        public void Block() => Status = AccountStatus.Blocked;

        private void EnsureActive()
        {
            if (Status != AccountStatus.Active)
                throw new InvalidOperationException("Conta inativa ou bloqueada.");
        }
    }

    public enum AccountStatus
    {
        Active = 1,
        Inactive = 2,
        Blocked = 3
    }
}
