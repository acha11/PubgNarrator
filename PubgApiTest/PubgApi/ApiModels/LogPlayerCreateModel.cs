namespace PubgApiTest.PubgApi.ApiModels
{
    public class LogPlayerCreateModel
    {
        public class CharacterModel
        {
            public string Name { get; set; }
            public string AccountId { get; set; }
            public int? TeamId { get; set; }
        }

        public CharacterModel Character { get; set; }
    }
}
