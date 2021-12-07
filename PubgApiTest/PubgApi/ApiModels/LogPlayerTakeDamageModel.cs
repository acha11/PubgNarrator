namespace PubgApiTest.PubgApi.ApiModels
{
    public class LogPlayerTakeDamageModel
    {
        public class PlayerModel
        {
            public string Name { get; set; }
            public string AccountId { get; set; }
        }

        public PlayerModel Victim { get; set; }
        public PlayerModel Attacker { get; set; }

        public string DamageTypeCategory { get; set; }
        public string DamageReason { get; set; }
        public double Damage { get; set; }
    }
}
