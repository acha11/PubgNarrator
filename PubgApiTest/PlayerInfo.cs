using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubgApiTest
{
    public class PlayerInfo
    {
        public string AccountId { get; set; }
        public string Name { get; set; }
        public bool IsBot { get; set; }
        public List<PlayerInfo> Kills { get; set; } = new List<PlayerInfo>();
        public List<PlayerInfo> Assists { get; set; } = new List<PlayerInfo>();
        public PlayerInfo Killer { get; set; }
        public bool Dies { get; set; }
        public bool IsWinner { get; set; }

        public Dictionary<string, double> DamageDealtByAccountId { get; set; } = new Dictionary<string, double>();

        public void RecordDamageDealt(PlayerInfo victim, double damage)
        {
            if (!DamageDealtByAccountId.ContainsKey(victim.AccountId))
            {
                DamageDealtByAccountId[victim.AccountId] = 0;
            }

            DamageDealtByAccountId[victim.AccountId] += damage;
        }

        public double GetDamageDealtTo(string victimAccountId)
        {
            return Math.Round(DamageDealtByAccountId[victimAccountId], 1);
        }
    }
}
