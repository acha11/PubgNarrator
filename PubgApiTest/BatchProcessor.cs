using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PubgApiTest.PubgApi;
using PubgApiTest.PubgApi.ApiModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubgApiTest
{
    public class BatchProcessor
    {
        PubgApiClient _pubgApi;
        Dictionary<string, PlayerInfo> _playersByAccountId;

        public BatchProcessor(PubgApiClient pubgApi)
        {
            _pubgApi = pubgApi;
        }

        public void ProcessRecentMatchesForPlayer(string playerName, int numberOfMatches = 5)
        {
            string rawResponse = _pubgApi.ExecuteGetRequest("https://api.pubg.com/shards/steam/players?filter[playerNames]=" + playerName, false);

            var playerInfo = JObject.Parse(rawResponse);

            var matchDatas = playerInfo.SelectToken("data[0].relationships.matches.data");

            var playerAccountId = playerInfo.SelectToken("data[0].id").ToString();

            var machesProcessed = 0;

            foreach (var matchData in matchDatas)
            {
                var matchId = matchData.SelectToken("id").ToString();

                var obj = JObject.Parse(_pubgApi.ExecuteGetRequest("https://api.pubg.com/shards/steam/matches/" + matchId, true));

                var matchLocalTime = obj.SelectToken("data.attributes.createdAt").ToObject<DateTime>().ToLocalTime();

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
                    ProcessTelemetry(telemetryUrl, matchLocalTime, playerAccountId);
                }

                machesProcessed++;
                if (machesProcessed == numberOfMatches)
                {
                    break;
                }
            }
        }

        void ProcessTelemetry(string telemetryUrl, DateTime matchLocalTime, string playerAccountId)
        {
            _playersByAccountId = new Dictionary<string, PlayerInfo>();

            string telemetryJson = _pubgApi.ExecuteGetRequest(telemetryUrl, true);

            var telemetry = JArray.Parse(telemetryJson);

            File.WriteAllText("telemetry.json", telemetry.ToString(Formatting.Indented));

            HashSet<string> nodeTypes = new HashSet<string>();

            foreach (var node in telemetry.Children())
            {
                var t = node.SelectToken("_T").Value<string>();

                nodeTypes.Add(t);

                if (t == "LogPlayerCreate")
                {
                    var msg = JsonConvert.DeserializeObject<LogPlayerCreateModel>(node.ToString());

                    EnsurePlayerInfo(msg.Character.AccountId, msg.Character.Name, msg.Character.AccountId.StartsWith("ai."), msg.Character.TeamId);
                }

                if (t == "LogMatchEnd")
                {
                    var msg = JsonConvert.DeserializeObject<LogMatchEndModel>(node.ToString());

                    foreach (var resultEntry in msg.GameResultOnFinished.Results)
                    {
                        _playersByAccountId[resultEntry.AccountId].IsWinner = true;
                    }
                }

                if (t == "LogPlayerTakeDamage")
                {
                    var msg = JsonConvert.DeserializeObject<LogPlayerTakeDamageModel>(node.ToString());

                    if (msg.Attacker != null)
                    {
                        var attacker = _playersByAccountId[msg.Attacker.AccountId];
                        var victim = _playersByAccountId[msg.Victim.AccountId];

                        attacker.RecordDamageDealt(victim, msg.Damage);
                    }
                }

                if (t == "LogPlayerKillV2")
                {
                    var msg = JsonConvert.DeserializeObject<LogPlayerKillV2Model>(node.ToString());

                    var damageReason = node.SelectToken("finishDamageInfo.damageReason")?.Value<string>() ?? "";
                    var damageTypeCategory = node.SelectToken("finishDamageInfo.damageTypeCategory")?.Value<string>() ?? "";

                    File.WriteAllText("playerKill_" + msg.Victim.Name + ".json", node.ToString(Formatting.Indented));

                    var victim = _playersByAccountId[msg.Victim.AccountId];

                    victim.Dies = true;

                    if (msg.Killer != null)
                    {
                        var killer = _playersByAccountId[msg.Killer.AccountId];

                        killer.Kills.Add(victim);

                        victim.Killer = killer;
                    }

                    foreach (var assistAccountId in msg.Assists_AccountId)
                    {
                        var assister = _playersByAccountId[assistAccountId];

                        assister.Assists.Add(victim);
                    }
                }
            }

            if (!Directory.Exists("outputs"))
            {
                Directory.CreateDirectory("outputs");
            }

            string filenameBase = "outputs\\match-" + matchLocalTime.ToString("yyyyMMddHHmmssfff");

            string dotFilename = filenameBase + ".dot";

            using (var sw = new StreamWriter(dotFilename))
            {
                new KillsOverviewGraphGenerator().Generate(sw, _playersByAccountId, "Match at " + matchLocalTime.ToString(), playerAccountId);
            }

            var p =
                new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = @"C:\Program Files\Graphviz\bin\dot.exe",
                        Arguments = "-Tsvg " + dotFilename,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

            p.Start();

            string svgFilename = filenameBase + ".svg";

            using (var sw = new StreamWriter(svgFilename))
            {
                while (!p.StandardOutput.EndOfStream)
                {
                    string line = p.StandardOutput.ReadLine();

                    sw.WriteLine(line);
                }
            }

            Process.Start(new ProcessStartInfo() { FileName = svgFilename, UseShellExecute = true });
        }

        PlayerInfo EnsurePlayerInfo(string accountId, string playerName, bool playerIsBot, int? teamId)
        {
            if (!_playersByAccountId.TryGetValue(accountId, out var pi))
            {
                pi =
                    new PlayerInfo()
                    {
                        AccountId = accountId,
                        Name = playerName,
                        IsBot = playerIsBot,
                        TeamId = teamId,
                    };

                _playersByAccountId[accountId] = pi;
            }

            return pi;
        }
    }
}
