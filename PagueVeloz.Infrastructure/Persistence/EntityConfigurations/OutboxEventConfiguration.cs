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
    public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
    {
        public void Configure(EntityTypeBuilder<OutboxEvent> builder)
        {
            builder.ToTable("OutboxEvents");

            builder.HasKey(o => o.OutboxEventId);

            builder.Property(o => o.Type)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.Payload)
                .IsRequired();

            builder.Property(o => o.CreatedAt);

            builder.Property(o => o.ProcessedAt);
        }
    }
}
