using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassBot.Models
{
    public class UserProfile
    {
        public string DiscordId { get; set; }
        public string DiscordUsername { get; set; }
        public string Email { get; set; }
        public string WalletAddress { get; set; }
    }
}
