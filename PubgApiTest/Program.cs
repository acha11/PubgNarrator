using PubgApiTest.PubgApi;

namespace PubgApiTest
{
    class Program
    {
        static void Main(string[] args)
        {
            PubgApiClient api = new PubgApiClient(args[1]);

            BatchProcessor batchProcessor = new BatchProcessor(api);

            var numberOfMatches = args.Length > 2 ? int.Parse(args[2]) : 5;

            batchProcessor.ProcessRecentMatchesForPlayer(args[0], numberOfMatches);
        }
    }
}
