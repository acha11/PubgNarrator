using System.Collections.Generic;

namespace PubgApiTest.PubgApi.ApiModels
{
    public class LogWheelDestroyModel : BaseTelemetryEventModel
    {
        public class PlayerModel
        {
            public string Name { get; set; }
            public string AccountId { get; set; }
        }

        public PlayerModel Attacker { get; set; }
    }
}
