namespace PubgApiTest.PubgApi.ApiModels
{
    public class LogItemEquipModel : BaseTelemetryEventModel
    {
        public CharacterModel Character { get; set; }
        public ItemModel Item { get; set; }

        public class CharacterModel
        {
            public string Name { get; set; }
            public string AccountId { get; set; }
        }

        public class ItemModel
        {
            public string Category { get; set; }
            public string SubCategory { get; set; }
            public string ItemId { get; set; }
        }            
    }
}
