using System.Collections.Generic;

namespace PubgApiTest.PubgApi.ApiModels
{
    public class LogPlayerMakeGroggyModel : BaseTelemetryEventModel
    {
        public class PlayerModel
        {
            public string Name { get; set; }
            public string AccountId { get; set; }
        }

        public PlayerModel Victim { get; set; }
        public PlayerModel Attacker { get; set; }
        public string DamageTypeCategory { get; set; }
        public double Damage { get; set; }
    }
}
