using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassBot.Models
{
    public class UserProfileWithPoints
    {
        public string DiscordId { get; set; }
        public string DiscordUsername { get; set; }
        public string Email { get; set; }
        public string WalletAddress { get; set; }
        public long Points { get; set; }
    }
}

//Token = "MTI5NDA2MDgyNzkxNzE1NjM5Mw.GyE9kL.kIODQAJR1h74KNdUV0lcNt9M1Jvvkhax2KSQ3U"
