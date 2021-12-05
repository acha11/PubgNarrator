using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PubgApiTest
{
    class Program
    {
        static void Main(string[] args)
        {
            const string BearerToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJqdGkiOiJlM2M0NzU3MC00MWRmLTAxMzYtZWEyNy0yMWQxYzFmODQ1N2QiLCJpc3MiOiJnYW1lbG9ja2VyIiwiaWF0IjoxNTI3MjA3ODU5LCJwdWIiOiJibHVlaG9sZSIsInRpdGxlIjoicHViZyIsImFwcCI6Im9ic2VydmVyIn0.2ECB5g0pgmzLyXEtYhEOwyvFoeH2ziRiamTDlwhFl8A";
            var client = new RestClient("https://api.pubg.com/shards/steam/");

            //var request = new RestRequest("matches/f6741b26-d63a-4cc3-8934-72df8a97f2c2", DataFormat.Json);

            var request = new RestRequest("matches/7c9813a9-ef6f-4b88-93cd-754f9c455cbe", DataFormat.Json);
            
            request.AddHeader("Authorization", "Bearer " + BearerToken);
            request.AddHeader("Accept", "application/vnd.api+json");

            var response = client.Get(request);

            var obj = JObject.Parse(response.Content);

            //Console.WriteLine(obj.ToString());

            var inclusions = obj.SelectToken("included");

            string telemetryUrl = null;

            foreach (var inclusion in inclusions)
            {
                var typeToken = inclusion.SelectToken("type");

                if (typeToken?.Type == JTokenType.String)
                {
                    var type = typeToken.Value<string>();

                    switch (type)
                    {
                        case "participant":
                            var playerId = inclusion.SelectToken("attributes.stats.playerId").Value<string>();
                            var name = inclusion.SelectToken("attributes.stats.name").Value<string>();
                            var deathType = inclusion.SelectToken("attributes.stats.deathType").Value<string>();
                            var kills = inclusion.SelectToken("attributes.stats.kills").Value<int>();

                            //Console.Write(playerId.PadRight(50));
                            //Console.Write(name.PadRight(20));
                            //Console.Write(deathType.PadRight(20));
                            //Console.Write(kills.ToString().PadRight(4));

                            //Console.WriteLine();

                            break;

                        case "asset":
                            var assetName = inclusion.SelectToken("attributes.name").Value<string>();

                            if (assetName == "telemetry")
                            {
                                var url = inclusion.SelectToken("attributes.URL").Value<string>();

                                telemetryUrl = url;
                            }

                            break;
                    }
                }
            }

            if (telemetryUrl != null)
            {
                ProcessTelemetry(telemetryUrl, BearerToken);
            }
        }

        static void ProcessTelemetry(string telemetryUrl, string bearerToken)
        {
            string telemetryJson = RetrieveTelemetry(telemetryUrl, bearerToken);

            var telemetry = JArray.Parse(telemetryJson);

            Dictionary<string, PlayerInfo> players = new Dictionary<string, PlayerInfo>();

            File.WriteAllText("telemetry.json", telemetry.ToString(Formatting.Indented));

            HashSet<string> nodeTypes = new HashSet<string>();

            Dictionary<string, string> accountIdToPlayerNameMapping = new Dictionary<string, string>();

            foreach (var node in telemetry.Children())
            {
                var t = node.SelectToken("_T").Value<string>();

                nodeTypes.Add(t);

                if (t == "LogPlayerCreate")
                {
                    var msg = JsonConvert.DeserializeObject<LogPlayerCreateModel>(node.ToString());

                    accountIdToPlayerNameMapping[msg.Character.AccountId] = msg.Character.Name;

                    EnsurePlayerInfo(players, msg.Character.Name, msg.Character.AccountId.StartsWith("ai."));
                }

                if (t == "LogMatchEnd")
                {
                    var msg = JsonConvert.DeserializeObject<LogMatchEndModel>(node.ToString());

                    foreach (var resultEntry in msg.GameResultOnFinished.Results)
                    {
                        players[accountIdToPlayerNameMapping[resultEntry.AccountId]].IsWinner = true;
                    }
                }

                if (t == "LogPlayerKillV2")
                {
                    var msg = JsonConvert.DeserializeObject<LogPlayerKillV2Model>(node.ToString());

                    var damageReason = node.SelectToken("finishDamageInfo.damageReason")?.Value<string>() ?? "";
                    var damageTypeCategory = node.SelectToken("finishDamageInfo.damageTypeCategory")?.Value<string>() ?? "";

                    File.WriteAllText("playerKill_" + msg.Victim.Name + ".json", node.ToString(Formatting.Indented));

                    var victim = EnsurePlayerInfo(players, msg.Victim.Name, msg.Victim.IsBot);

                    victim.Dies = true;

                    if (msg.Killer != null)
                    {
                        var killer = EnsurePlayerInfo(players, msg.Killer.Name, msg.Killer.IsBot);

                        killer.Kills.Add(victim);

                        victim.Killer = killer;
                    }

                    foreach (var assistAccountId in msg.Assists_AccountId)
                    {
                        var assisterName = accountIdToPlayerNameMapping[assistAccountId];

                        var assister = players[assisterName];

                        assister.Assists.Add(victim);
                    }
                }
            }

            DumpKillsAndAssistsGraph(players);
        }

        private static void DumpKillsAndAssistsGraph(Dictionary<string, PlayerInfo> players)
        {
            Console.WriteLine("digraph G {");

            Console.WriteLine("rankdir = \"LR\";");

            foreach (var p in players.Values)
            {
                string fillcolor;

                if (!p.IsBot)
                {
                    if (p.Name == "acha11" || p.Name == "ledpup" || p.Name == "RavenMark" || p.Name == "Playcache")
                    {
                        fillcolor = "yellow";
                    }
                    else
                    {
                        fillcolor = "cyan";
                    }
                }
                else
                {
                    fillcolor = "white";
                }

                Console.WriteLine("   \"" + p.Name + "\" [fillcolor=\"" + fillcolor + "\", style=\"filled\"];");
            }

            // Put the winners in a box.
            Console.WriteLine("subgraph cluster_0 {");
            Console.WriteLine("label = \"Winners\";");
            Console.WriteLine("style = filled;");
            Console.WriteLine("color = lightgrey;");
            foreach (var p in players.Values.Where(x => x.IsWinner))
            {
                Console.WriteLine("   \"" + p.Name + "\";");
            }
            Console.WriteLine("}");

            // Put the logout-ers in a box
            Console.WriteLine("subgraph cluster_1 {");
            Console.WriteLine("label = \"Probably logged out\";");
            Console.WriteLine("style = filled;");
            Console.WriteLine("color = lightgrey;");
            foreach (var p in players.Values.Where(x => !x.Dies && !x.IsWinner))
            {
                Console.WriteLine("   \"" + p.Name + "\";");
            }
            Console.WriteLine("}");

            // Put the DiedToEnvironment-ers in a box
            Console.WriteLine("subgraph cluster_2 {");
            Console.WriteLine("label = \"Died to environment\";");
            Console.WriteLine("style = filled;");
            Console.WriteLine("color = lightgrey;");
            foreach (var p in players.Values.Where(x => x.Killer == null && x.Dies))
            {
                Console.WriteLine("   \"" + p.Name + "\";");
            }
            Console.WriteLine("}");

            foreach (var p in players.Values)
            {
                DumpKillGraph(p);
                DumpAssistGraph(p);
            }

            Console.WriteLine("}");
        }

        private static string RetrieveTelemetry(string telemetryUrl, string bearerToken)
        {
            var telemetryClient = new RestClient();

            var telemetryRequest = new RestRequest(telemetryUrl, DataFormat.Json);

            telemetryRequest.AddHeader("Authorization", "Bearer " + bearerToken);
            telemetryRequest.AddHeader("Accept", "application/vnd.api+json");

            return telemetryClient.Get(telemetryRequest).Content;
        }

        static void Dump(PlayerInfo p, int level)
        {
            Console.WriteLine("".PadLeft(level * 3) + p.Name);

            foreach (var victim in p.Kills)
            {
                Dump(victim, level + 1);
            }
        }

        static void DumpKillGraph(PlayerInfo p)
        {
            foreach (var victim in p.Kills)
            {
                Console.WriteLine("   \"" + p.Name + "\"->\"" + victim.Name + "\" [color=red];");
            }
        }

        static void DumpAssistGraph(PlayerInfo p)
        {
            foreach (var victim in p.Assists)
            {
                Console.WriteLine("   \"" + p.Name + "\"->\"" + victim.Name + "\" [color=black   style=dashed  ];");
            }
        }

        static PlayerInfo EnsurePlayerInfo(Dictionary<string, PlayerInfo> pis, string playerName, bool playerIsBot)
        {
            if (!pis.TryGetValue(playerName, out var pi))
            {
                pi = new PlayerInfo()
                {
                    Name = playerName,
                    IsBot = playerIsBot
                };

                pis[playerName] = pi;
            }

            return pi;
        }
    }

    public class PlayerInfo
    {
        public string Name { get; set; }
        public bool IsBot { get; set; }
        public List<PlayerInfo> Kills { get; set; } = new List<PlayerInfo>();
        public List<PlayerInfo> Assists { get; set; } = new List<PlayerInfo>();
        public PlayerInfo Killer { get; set; }
        public bool Dies { get; set; }
        public bool IsWinner { get; set; }
    }
}
