using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.Domain.Entities
{
    public class OutboxEvent
    {
        public Guid OutboxEventId { get; private set; } = Guid.NewGuid();
        public string Type { get; private set; } = null!;
        public string Payload { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; private set; }

        public OutboxEvent(string type, string payload)
        {
            Type = type;
            Payload = payload;
        }

        public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;
    }

}
