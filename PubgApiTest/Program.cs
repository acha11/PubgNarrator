using PubgApiTest.PubgApi;

namespace PubgApiTest
{
    class Program
    {
        static void Main(string[] args)
        {
            PubgApiClient api = new PubgApiClient(args[1]);

            BatchProcessor batchProcessor = new BatchProcessor(api);

            batchProcessor.ProcessRecentMatchesForPlayer(args[0]);
        }
    }
}
