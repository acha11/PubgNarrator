using System.Collections.Generic;

namespace PubgApiTest.PubgApi.ApiModels
{
    public class LogPlayerReviveModel : BaseTelemetryEventModel
    {
        public class PlayerModel
        {
            public string Name { get; set; }
            public string AccountId { get; set; }
        }

        public PlayerModel Victim { get; set; }
        public PlayerModel Reviver { get; set; }
    }
}
