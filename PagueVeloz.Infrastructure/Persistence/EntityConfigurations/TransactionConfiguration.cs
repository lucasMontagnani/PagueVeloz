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
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");

            builder.HasKey(t => t.TransactionId);

            builder.Property(t => t.ReferenceId)
                  .IsRequired();

            builder.Property(t => t.AccountId)
                  .IsRequired();

            builder.Property(t => t.Operation)
                  .HasConversion<string>()
                  .IsRequired();

            builder.Property(t => t.Status)
                  .HasConversion<string>()
                  .IsRequired();

            builder.Property(t => t.Currency)
                  .HasMaxLength(10);

            builder.Property(t => t.Timestamp);

            builder.Property(t => t.Description)
                  .HasMaxLength(100);

            builder.HasIndex(t => t.ReferenceId)
                  .IsUnique();

            builder.HasOne(t => t.Account)
                   .WithMany(a => a.SentTransactions)
                   .HasForeignKey(t => t.AccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.DestinationAccount)
                   .WithMany(a => a.ReceivedTransactions)
                   .HasForeignKey(t => t.DestinationAccountId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
