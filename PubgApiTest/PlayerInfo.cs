using PubgApiTest.PubgApi.ApiModels;
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
        public int TeamId { get; set; }
        public List<PlayerInfo> Revivees { get; set; } = new List<PlayerInfo>();
        public List<PlayerInfo> DbnodBy { get; set; } = new List<PlayerInfo>();
        public List<PlayerInfo> Dbnos { get; set; } = new List<PlayerInfo>();
        public List<PlayerInfo> Kills { get; set; } = new List<PlayerInfo>();
        public List<PlayerInfo> Assists { get; set; } = new List<PlayerInfo>();
        public PlayerInfo Killer { get; set; }
        public bool Dies { get; set; }
        public bool IsWinner { get; set; }
        public string EquippedWeapon { get; set; }

        public Dictionary<string, double> DamageDealtByAccountId { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> DamageReceivedByAccountId { get; set; } = new Dictionary<string, double>();
        public List<PlayerEvent> Events { get; set; } = new List<PlayerEvent>();
        public bool IsDbno { get; set; }
        public double TotalDamageTaken { get; set; }
        public double TotalDamageDealt { get; set; }
        public DateTime? DiesAt { get; set; }
        public LocationModel Location { get; set; } = new LocationModel();

        public void RecordDamageDealt(PlayerInfo victim, double damage)
        {
            if (!DamageDealtByAccountId.ContainsKey(victim.AccountId))
            {
                DamageDealtByAccountId[victim.AccountId] = 0;
            }

            DamageDealtByAccountId[victim.AccountId] += damage;

            if (!victim.DamageReceivedByAccountId.ContainsKey(AccountId))
            {
                victim.DamageReceivedByAccountId[AccountId] = 0;
            }

            victim.DamageReceivedByAccountId[AccountId] += damage;
        }

        public double GetDamageDealtTo(string victimAccountId)
        {
            if (DamageDealtByAccountId.TryGetValue(victimAccountId, out var damage))
            {
                return Math.Round(damage, 1);
            }

            return 0;
        }

        public void AddEvent(PlayerEvent playerEvent)
        {
            Events.Add(playerEvent);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
