using FluentAssertions;
using PagueVeloz.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Tests.Domain
{
    public class AccountTests
    {
        private readonly Guid _clientId = Guid.NewGuid();

        private Account CreateAccount(double available, double creditLimit = 0, double reserved = 0)
        {
            Account account = new Account(_clientId, available, creditLimit);
            typeof(Account)
                .GetProperty(nameof(Account.ReservedBalance))!
                .SetValue(account, reserved);
            return account;
        }

        // Operações não podem deixar o saldo disponível negativo
        [Fact(DisplayName = "Não deve permitir que o saldo disponível fique negativo")]
        public void Debit_ShouldThrow_WhenBalanceWouldBeNegative()
        {
            Account account = CreateAccount(100, creditLimit: 0);

            Action act = () => account.Debit(200);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Saldo insuficiente para débito.");

            account.AvailableBalance.Should().Be(100);
        }

        // Limite de crédito deve ser respeitado (success)
        [Fact(DisplayName = "Deve permitir débito dentro do limite de crédito")]
        public void Debit_ShouldAllow_WhenWithinCreditLimit()
        {
            Account account = CreateAccount(100, creditLimit: 200);

            account.Debit(250);

            account.AvailableBalance.Should().Be(-150);
        }

        // Limite de crédito deve ser respeitado (fail)
        [Fact(DisplayName = "Deve falhar débito quando exceder o limite de crédito")]
        public void Debit_ShouldThrow_WhenExceedsCreditLimit()
        {
            Account account = CreateAccount(100, creditLimit: 200);

            Action act = () => account.Debit(400);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Saldo insuficiente para débito.");

            account.AvailableBalance.Should().Be(100);
        }

        // Operações de débito consideram saldo disponível + limite de crédito
        [Fact(DisplayName = "Deve permitir débito usando saldo + limite de crédito")]
        public void Debit_ShouldUseAvailableBalanceAndCreditLimit()
        {
            Account account = CreateAccount(100, creditLimit: 100);

            account.Debit(180);

            account.AvailableBalance.Should().Be(-80);
        }

        // Reservas só podem ser feitas com saldo disponível (fail)
        [Fact(DisplayName = "Deve falhar reserva quando saldo disponível for insuficiente")]
        public void Reserve_ShouldThrow_WhenInsufficientAvailableBalance()
        {
            Account account = CreateAccount(50);

            Action act = () => account.Reserve(100);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Saldo disponível insuficiente para reserva.");

            account.AvailableBalance.Should().Be(50);
            account.ReservedBalance.Should().Be(0);
        }

        // Reservas só podem ser feitas com saldo disponível (succes)
        [Fact(DisplayName = "Deve reservar valor do saldo disponível corretamente")]
        public void Reserve_ShouldMoveFundsToReservedBalance()
        {
            Account account = CreateAccount(200);

            account.Reserve(100);

            account.AvailableBalance.Should().Be(100);
            account.ReservedBalance.Should().Be(100);
        }

        // Capturas só podem ser feitas com saldo reservado suficiente (fail)
        [Fact(DisplayName = "Deve falhar captura quando saldo reservado for insuficiente")]
        public void Capture_ShouldThrow_WhenInsufficientReservedBalance()
        {
            Account account = CreateAccount(100, reserved: 50);

            Action act = () => account.Capture(100);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Saldo reservado insuficiente para captura.");

            account.ReservedBalance.Should().Be(50);
        }

        // Capturas só podem ser feitas com saldo reservado suficiente (success)
        [Fact(DisplayName = "Deve capturar valor reservado corretamente")]
        public void Capture_ShouldReduceReservedBalance_WhenSufficient()
        {
            Account account = CreateAccount(100, reserved: 100);

            account.Capture(50);

            account.ReservedBalance.Should().Be(50);
        }

        // Conta bloqueada não deve permitir operações
        [Fact(DisplayName = "Não deve permitir operações em conta bloqueada")]
        public void ShouldThrow_WhenAccountIsBlocked()
        {
            Account account = CreateAccount(100);
            account.Block();

            Action act = () => account.Debit(50);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Conta inativa ou bloqueada.");
        }
    }
}
