namespace PubgApiTest
{
    public class LogPlayerKillV2Model
    {
        public class PlayerModel
        {
            public string Name { get; set; }
            public string AccountId { get; set; }

            public bool IsBot { get { return AccountId.StartsWith("ai."); } }
        }

        public PlayerModel Victim { get; set; }
        public PlayerModel Killer { get; set; }
        public string[] Assists_AccountId { get; set; }
    }
}
