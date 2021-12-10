using System.Collections.Generic;

namespace PubgApiTest.PubgApi.ApiModels
{
    public class LogPlayerKillV2Model : BaseTelemetryEventModel
    {
        public class PlayerModel
        {
            public string Name { get; set; }
            public string AccountId { get; set; }

            public bool IsBot { get { return AccountId.StartsWith("ai."); } }

            public double Health { get; set; }
        }

        public class DamageInfo
        {
            public string DamageReason { get; set; }
            public string DamageTypeCategory { get; set; }
        }
  //"killerDamageInfo": {
  //  "damageReason": "None",
  //  "damageTypeCategory": "Damage_Punch",
  //  "damageCauserName": "UltAIPawn_Base_Female_C",
  //  "additionalInfo": [],
  //  "distance": 80.0419692993164,
  //  "isThroughPenetrableWall": false
  //},


        public PlayerModel Victim { get; set; }
        public PlayerModel Killer { get; set; }
        public PlayerModel Finisher { get; set; }
        public DamageInfo KillerDamageInfo { get; set; }
        public DamageInfo FinisherDamageInfo { get; set; }

        public string[] Assists_AccountId { get; set; }
    }
}
