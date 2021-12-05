namespace PubgApiTest
{
    public class LogPlayerCreateModel
    {
        public class CharacterModel
        {
            public string Name { get; set; }
            public string AccountId { get; set; }
        }

        public CharacterModel Character { get; set; }
    }
}
