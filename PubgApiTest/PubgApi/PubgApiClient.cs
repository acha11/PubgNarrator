using RestSharp;
using System;

namespace PubgApiTest.PubgApi
{
    public class PubgApiClient
    {
        ApiResultCacher _cache = new ApiResultCacher();

        string _bearerToken;

        public PubgApiClient(string bearerToken)
        {
            _bearerToken = bearerToken;
        }

        public string ExecuteGetRequest(string url, bool allowCache)
        {
            string response = null;

            if (allowCache)
            {
                response = _cache.GetEntry(url);
            }

            if (response == null)
            {
                var client = new RestClient();

                var request = new RestRequest(url, DataFormat.Json);

                request.AddHeader("Authorization", "Bearer " + _bearerToken);
                request.AddHeader("Accept", "application/vnd.api+json");

                response = client.Get(request).Content;

                _cache.WriteEntry(url, response);
            }
            else
            {
                Console.WriteLine("Cache hit for " + url);
            }

            return response;
        }
    }
}
