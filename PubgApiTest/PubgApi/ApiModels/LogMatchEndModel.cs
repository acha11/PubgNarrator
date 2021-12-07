namespace PubgApiTest.PubgApi.ApiModels
{
    public class LogMatchEndModel
    {
        public class GameResultOnFinishedModel
        {
            public ResultModel[] Results { get; set; }

            public class ResultModel
            {
                public int Rank { get; set; }
                public string AccountId { get; set; }
            }
        }

        public GameResultOnFinishedModel GameResultOnFinished { get; set; }
    }
}
