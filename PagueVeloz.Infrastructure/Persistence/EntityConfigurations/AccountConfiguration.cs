using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PagueVeloz.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Infrastructure.Persistence.EntityConfigurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");

            builder.HasKey(a => a.AccountId);

            builder.Property(a => a.ClientId)
                  .IsRequired();

            builder.Property(a => a.AvailableBalance)
                  .IsRequired();

            builder.Property(a => a.ReservedBalance)
                  .IsRequired();

            builder.Property(a => a.CreditLimit)
                  .IsRequired();

            builder.Property(a => a.Status)
                  .HasConversion<int>()
                  .IsRequired();

            builder.HasOne(a => a.Client)
                   .WithMany(c => c.Accounts)
                   .HasForeignKey(a => a.ClientId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Transações enviadas (origem)
            builder.HasMany(a => a.SentTransactions)
                   .WithOne(t => t.Account)
                   .HasForeignKey(t => t.AccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Transações recebidas (destino)
            builder.HasMany(a => a.ReceivedTransactions)
                   .WithOne(t => t.DestinationAccount)
                   .HasForeignKey(t => t.DestinationAccountId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
