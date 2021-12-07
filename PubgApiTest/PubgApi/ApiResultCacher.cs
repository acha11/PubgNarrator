using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PubgApiTest.PubgApi
{
    public class ApiResultCacher
    {
        public ApiResultCacher()
        {
            if (!Directory.Exists("cache"))
            {
                Directory.CreateDirectory("cache");
            }
        }

        string GetCacheEntryPath(string key)
        {
            return Path.Combine("cache", WebUtility.UrlEncode(key) + ".cache.json");
        }

        public bool HasEntry(string key)
        {
            return File.Exists(GetCacheEntryPath(key));
        }

        public string GetEntry(string key)
        {
            if (File.Exists(GetCacheEntryPath(key)))
            {
                return File.ReadAllText(GetCacheEntryPath(key));
            }

            return null;
        }

        public void WriteEntry(string key, string value)
        {
            File.WriteAllText(GetCacheEntryPath(key), value);
        }
    }
}
