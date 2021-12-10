using PubgApiTest.PubgApi.ApiModels;
using System;

namespace PubgApiTest
{
    public class PlayerEvent
    {
        public PlayerEvent(BaseTelemetryEventModel msg)
        {
            Time = msg._D;
        }

        public PlayerEvent()
        {
        }

        public DateTime? Time { get; set; }
        public string Type { get; set; }
        public int? Amount { get; set; }
    }
}