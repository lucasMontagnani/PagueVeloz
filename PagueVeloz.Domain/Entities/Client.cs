using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PagueVeloz.Domain.Entities
{
    public class Client
    {
        public Guid ClientId { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        [JsonIgnore]
        public virtual ICollection<Account>? Accounts { get; set; }

        protected Client() { }

        public Client(string name, string email)
        {
            Name = name;
            Email = email;
        }
    }
}
