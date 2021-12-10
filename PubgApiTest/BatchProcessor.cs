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
        DateTime _matchStartTime;

        public BatchProcessor(PubgApiClient pubgApi)
        {
            _pubgApi = pubgApi;
        }

        public void ProcessRecentMatchesForPlayer(string playerName, int numberOfMatches = 5)
        {
            string rawResponse = _pubgApi.ExecuteGetRequest("https://api.pubg.com/shards/steam/players?filter[playerNames]=" + playerName, false);

            var playerInfo = JObject.Parse(rawResponse);

            var matchDatas = playerInfo
                                .SelectToken("data[0].relationships.matches.data")
                                .Take(numberOfMatches);

            var playerAccountId = playerInfo.SelectToken("data[0].id").ToString();

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
            }
        }

        void ProcessTelemetry(string telemetryUrl, DateTime matchLocalTime, string playerAccountId)
        {
            _playersByAccountId = new Dictionary<string, PlayerInfo>();

            string telemetryJson = _pubgApi.ExecuteGetRequest(telemetryUrl, true);

            var telemetry = JArray.Parse(telemetryJson);

            File.WriteAllText("telemetry.json", telemetry.ToString(Formatting.Indented));

            if (File.Exists("Xiaopooo.json")) File.Delete("Xiaopooo.json");

            File.WriteAllText("Xiaopooo.json", "[\r\n");

            HashSet<string> nodeTypes = new HashSet<string>();

            PlayerInfo lastKillInflicter = null;
            PlayerInfo lastToDie = null;

            foreach (var node in telemetry.Children())
            {
                var t = node.SelectToken("_T").Value<string>();

                nodeTypes.Add(t);

                if (t == "LogMatchStart")
                {
                    var msg = JsonConvert.DeserializeObject<BaseTelemetryEventModel>(node.ToString());

                    _matchStartTime = msg._D;
                }

                if (t == "LogPlayerPosition")
                {
                    var msg = JsonConvert.DeserializeObject<LogPlayerPositionModel>(node.ToString());

                    var player = _playersByAccountId[msg.Character.AccountId];

                    player.Location.X = msg.Character.Location.X;
                    player.Location.Y = msg.Character.Location.X;
                    player.Location.Z = msg.Character.Location.X;
                }

                if (t == "LogPlayerRevive")
                {
                    var msg = JsonConvert.DeserializeObject<LogPlayerReviveModel>(node.ToString());

                    var victim = _playersByAccountId[msg.Victim.AccountId];

                    victim.IsDbno = false;

                    victim.AddEvent(new PlayerEvent(msg) { Type = "#was-revived" });

                    var reviver = _playersByAccountId[msg.Reviver.AccountId];

                    reviver.AddEvent(new PlayerEvent(msg) { Type = "#revived-a-teammate" });

                    reviver.Revivees.Add(victim);
                }

                if (t == "LogItemPickupFromCarepackage")
                {
                    var msg = JsonConvert.DeserializeObject<LogItemPickupFromCarepackageModel>(node.ToString());

                    var attacker = _playersByAccountId[msg.Character.AccountId];

                    attacker.AddEvent(new PlayerEvent(msg) { Type = "#looted-carepackage" });
                }

                if (t == "LogPlayerUseFlareGun")
                {
                    var msg = JsonConvert.DeserializeObject<LogPlayerUseFlareGunModel>(node.ToString());

                    var attacker = _playersByAccountId[msg.Attacker.AccountId];

                    attacker.AddEvent(new PlayerEvent(msg) { Type = "#used-flare-gun" });
                }

                if (t == "LogWheelDestroy")
                {
                    var msg = JsonConvert.DeserializeObject<LogWheelDestroyModel>(node.ToString());

                    if (!string.IsNullOrWhiteSpace(msg.Attacker?.AccountId))
                    {
                        var attacker = _playersByAccountId[msg.Attacker.AccountId];

                        attacker.AddEvent(new PlayerEvent(msg) { Type = "#destroyed-wheel" });
                    }
                }

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
                        var winner = _playersByAccountId[resultEntry.AccountId];

                        winner.IsWinner = true;

                        if (winner.Dies)
                        {
                            winner.AddEvent(new PlayerEvent(msg) { Type = "#won-while-dead" });
                        }
                        else
                        {
                            if (winner.IsDbno)
                            {
                                winner.AddEvent(new PlayerEvent(msg) { Type = "#won-while-knocked" });
                            }
                        }
                    }
                }

                if (t == "LogPlayerTakeDamage")
                {
                    var msg = JsonConvert.DeserializeObject<LogPlayerTakeDamageModel>(node.ToString());

                    var victim = _playersByAccountId[msg.Victim.AccountId];

                    victim.TotalDamageTaken += msg.Damage;

                    // Console.Write(MatchElapsed(msg) + ": " + victim.Name.PadRight(20) + " takes " + msg.Damage.ToString("N2").PadLeft(8) + " damage " + msg.DamageTypeCategory + " " + msg.DamageReason);

                    if (msg.Attacker != null)
                    {
                        var attacker = _playersByAccountId[msg.Attacker.AccountId];

                        attacker.TotalDamageDealt += msg.Damage;

                        attacker.RecordDamageDealt(victim, msg.Damage);

                        if (attacker == victim && msg.DamageTypeCategory != "Damage_Groggy")
                        {
                            attacker.AddEvent(new PlayerEvent(msg) { Type = "#damaged-self" });
                            attacker.AddEvent(new PlayerEvent(msg) { Type = "#damaged-self-" + msg.DamageTypeCategory, Amount = (int)Math.Round(msg.Damage) });
                        }

                        // Console.Write(" from " + attacker.Name.PadRight(20));
                    }

                    // Console.WriteLine();
                }

                if (t == "LogItemEquip")
                {
                    var msg = JsonConvert.DeserializeObject<LogItemEquipModel>(node.ToString());

                    if (msg.Item.Category == "Weapon" && msg.Item.SubCategory == "Main")
                    {
                        _playersByAccountId[msg.Character.AccountId].EquippedWeapon = msg.Item.ItemId;
                    }
                }

                if (t == "LogItemUnequip")
                {
                    var msg = JsonConvert.DeserializeObject<LogItemEquipModel>(node.ToString());

                    if (msg.Item.Category == "Weapon" && msg.Item.SubCategory == "Main")
                    {
                        _playersByAccountId[msg.Character.AccountId].EquippedWeapon = null;
                    }
                }

                if (t == "LogPlayerMakeGroggy")
                {
                    var msg = JsonConvert.DeserializeObject<LogPlayerMakeGroggyModel>(node.ToString());
                    var victim = _playersByAccountId[msg.Victim.AccountId];

                    // Console.ForegroundColor = ConsoleColor.Yellow;
                    // Console.Write(MatchElapsed(msg) + ": " + victim.Name.PadRight(20) + " is DBNO");
                    // Console.ForegroundColor = ConsoleColor.DarkGray;

                    if (!String.IsNullOrWhiteSpace(msg.Attacker?.AccountId))
                    {
                        var attacker = _playersByAccountId[msg.Attacker.AccountId];
                        victim.DbnodBy.Add(attacker);
                        attacker.Dbnos.Add(victim);

                        if (victim == attacker)
                        {
                            victim.AddEvent(new PlayerEvent(msg) { Type = "#knocked-self" });
                            victim.AddEvent(new PlayerEvent(msg) { Type = "#knocked-self" + msg.DamageTypeCategory, Amount = (int)Math.Round(msg.Damage) });
                        }
                        else
                        {
                            if (victim.TeamId == attacker.TeamId)
                            {
                                attacker.AddEvent(new PlayerEvent(msg) { Type = "#knocked-teammate" });
                                attacker.AddEvent(new PlayerEvent(msg) { Type = "#knocked-teammate" + msg.DamageTypeCategory});

                                victim.AddEvent(new PlayerEvent(msg) { Type = "#knocked-by-teammate" });
                                victim.AddEvent(new PlayerEvent(msg) { Type = "#knocked-by-teammate" + msg.DamageTypeCategory });
                            }
                        }

                        victim.IsDbno = true;
                    }

                    var livingTeamMates = _playersByAccountId.Values.Where(x => x.TeamId == victim.TeamId && x.AccountId != victim.AccountId && !x.Dies).ToArray();

                    if (livingTeamMates.Any())
                    {
                        // Console.WriteLine("Died with teammates");

                        foreach (var teamMate in livingTeamMates)
                        {
                            // Console.WriteLine("  " + (teamMate.Location.DistanceFrom(victim.Location) / 100) + "cm");
                        }

                        var distanceToClosestLivingTeamMate = livingTeamMates.Min(x => x.Location.DistanceFrom(victim.Location) / 100);

                        if (distanceToClosestLivingTeamMate > 250)
                        {
                            victim.AddEvent(new PlayerEvent(msg) { Type = "#knocked-with-no-teammates-within-250m", Amount = (int)distanceToClosestLivingTeamMate });
                        }
                    }

                    // Console.WriteLine();
                }

                if (t == "LogPlayerKillV2")
                {
                    var msg = JsonConvert.DeserializeObject<LogPlayerKillV2Model>(node.ToString());

                    var damageReason = node.SelectToken("finishDamageInfo.damageReason")?.Value<string>() ?? "";
                    var damageTypeCategory = node.SelectToken("finishDamageInfo.damageTypeCategory")?.Value<string>() ?? "";

                    var victimWeapon = node.SelectToken("victimWeapon")?.Value<string>();

                    var victim = _playersByAccountId[msg.Victim.AccountId];

                    lastToDie = victim;

                    victim.Dies = true;
                    victim.DiesAt = msg._D;

                    //Console.ForegroundColor = ConsoleColor.Red;
                    //Console.Write(MatchElapsed(msg) + ": " + victim.Name.PadRight(20) + " is killed");
                    //Console.ForegroundColor = ConsoleColor.DarkGray;

                    // Can't trust victimWeapon field for bots, unfortunately, so we track equipped weapon based on LogItemEquip
                    // and LogItemUnequip messages.
                    if (victim.EquippedWeapon == null)
                    {
                        victim.AddEvent(new PlayerEvent(msg) { Type = "#died-unarmed" });
                    }

                    var creditedKiller = msg.Killer ?? msg.Finisher;
                    var damageInfo = msg.KillerDamageInfo ?? msg.FinisherDamageInfo;

                    if (creditedKiller != null)
                    {
                        var killer = _playersByAccountId[creditedKiller.AccountId];

                        killer.Kills.Add(victim);

                        victim.Killer = killer;

                        if (victim.TeamId == killer.TeamId && victim.AccountId != killer.AccountId)
                        {
                            killer.AddEvent(new PlayerEvent(msg) { Type = "#killed-teammate" });
                            victim.AddEvent(new PlayerEvent(msg) { Type = "#killed-by-teammate" });
                        }

                        if (damageInfo?.DamageTypeCategory == "Damage_Punch")
                        { 
                            killer.AddEvent(new PlayerEvent(msg) { Type = "#punched-a-player-to-death" });
                        }

                        if (victim == killer)
                        {
                            victim.AddEvent(new PlayerEvent(msg) { Type = "#killed-self" });
                        }
                        else
                        {
                            if (victim.EquippedWeapon == null)
                            {
                                killer.AddEvent(new PlayerEvent(msg) { Type = "#killed-an-unarmed-player" });
                            }

                            if (damageInfo?.DamageTypeCategory == "Damage_Explosion_Grenade")
                            {
                                killer.AddEvent(new PlayerEvent(msg) { Type = "#killed-a-player-with-a-grenade" });
                            }

                            if (creditedKiller.Health > 0 && creditedKiller.Health < 20)
                            {
                                killer.AddEvent(new PlayerEvent(msg) { Type = "#killed-a-player-while-low-on-health", Amount = (int)Math.Round(creditedKiller.Health) });
                            }
                        }

                        lastKillInflicter = killer;
                    }
                    else
                    {
                        lastKillInflicter = null;
                    }

                    foreach (var assistAccountId in msg.Assists_AccountId)
                    {
                        var assister = _playersByAccountId[assistAccountId];

                        assister.Assists.Add(victim);
                    }

                    //Console.WriteLine();
                }
            }

            foreach (var playerInfo in _playersByAccountId.Values)
            {
                var damageReceived = playerInfo.DamageReceivedByAccountId.Values.Sum();

                if (damageReceived > 200)
                {
                    playerInfo.AddEvent(new PlayerEvent() { Type = "#tank", Amount = (int)Math.Round(damageReceived) });
                }

                if (playerInfo.DamageReceivedByAccountId.Count > 5)
                {
                    playerInfo.AddEvent(new PlayerEvent() { Type = "#damaged-by-5-or-more-players", Amount = playerInfo.DamageReceivedByAccountId.Count });
                }
            }

            if (lastKillInflicter != null)
            {
                lastKillInflicter.AddEvent(new PlayerEvent() { Type = "#inflicted-last-kill" });
            }

            if (lastToDie != null)
            {
                lastToDie.AddEvent(new PlayerEvent() { Type = "#last-to-die" });
            }

            foreach (var winner in _playersByAccountId.Values.Where(x => x.IsWinner))
            {
                if (winner.TotalDamageTaken == 0)
                {
                    winner.AddEvent(new PlayerEvent() { Type = "#untouched" });
                }
                else
                {
                    if (winner.TotalDamageTaken < 50)
                    {
                        winner.AddEvent(new PlayerEvent() { Type = "#barely-touched", Amount = (int)Math.Round(winner.TotalDamageTaken) });
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
                new KillsOverviewGraphGenerator(_matchStartTime).Generate(sw, _playersByAccountId, "Match at " + matchLocalTime.ToString(), playerAccountId);
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

        string MatchElapsed(BaseTelemetryEventModel evt)
        {
            var elapsed = evt._D - _matchStartTime;

            return ((int)elapsed.TotalMinutes) + "m" + elapsed.Seconds + "." + elapsed.Milliseconds.ToString().PadRight(3, '0') + "s";
        }

        PlayerInfo EnsurePlayerInfo(string accountId, string playerName, bool playerIsBot, int teamId)
        {
            if (!_playersByAccountId.TryGetValue(accountId, out var pi))
            {
                pi =
                    new PlayerInfo()
                    {
                        AccountId = accountId,
                        Name = playerName,
                        IsBot = playerIsBot,
                        TeamId = teamId
                    };

                _playersByAccountId[accountId] = pi;
            }

            return pi;
        }
    }
}
