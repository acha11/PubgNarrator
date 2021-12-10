using PubgApiTest.GraphViz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubgApiTest
{
    public class KillsOverviewGraphGenerator
    {
        DateTime _matchStartTime;
        
        public KillsOverviewGraphGenerator(DateTime matchStartTime)
        {
            _matchStartTime = matchStartTime;
        }

        public void Generate(StreamWriter sw, Dictionary<string, PlayerInfo> players, string title, string playerAccountId)
        {
            GraphVizWriter writer = new GraphVizWriter(sw);

            writer.RawWrite("digraph G {");

            writer.WriteKvp("rankdir", "LR");
            writer.WriteKvp("labelloc", "tl");
            writer.WriteKvp("label", title);
            writer.WriteKvp("fontsize", "30");
            writer.WriteKvp("fontname", "Verdana");

            bool ignoreBots = false;

            var playerTeamId = players[playerAccountId].TeamId;

            foreach (var p in players.Values.Where(x => !ignoreBots || !x.IsBot))
            {
                string fillcolor;

                if (!p.IsBot)
                {
                    if (p.TeamId == playerTeamId)
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

                sw.WriteLine("   \"" + p.Name + "\" [fillcolor=\"" + fillcolor + "\", style=\"filled\"];");
            }

            WriteCluster(writer, "cluster_0", "Winners",             players.Values.Where(x => GetNodeCategory(x) == NodeCategory.Winner));
            WriteCluster(writer, "cluster_1", "Probably logged out", players.Values.Where(x => GetNodeCategory(x) == NodeCategory.ProbablyLoggedOut));
            WriteCluster(writer, "cluster_2", "Died to environment", players.Values.Where(x => GetNodeCategory(x) == NodeCategory.DiedToEnvironment && (!ignoreBots || !x.IsBot)));

            foreach (var player in players.Values.Where(x => GetNodeCategory(x) == NodeCategory.Other && (!ignoreBots || !x.IsBot)))
            {
                writer.RawWrite("   \"" + player.Name + "\" [label=\"" + BuildNodeLabel(player).Replace("\"", "\"\"") + "\"];");
            }


            foreach (var p in players.Values)
            {
                DumpDbnoGraph(writer, p, ignoreBots);
                DumpReviveesGraph(writer, p);
                DumpKillGraph(writer, p, ignoreBots);
                DumpAssistGraph(writer, p, ignoreBots);
                //DumpDamageGraph(p, players);
            }

            writer.RawWrite("}");
        }

        NodeCategory GetNodeCategory(PlayerInfo pi)
        {
            if (pi.IsWinner) return NodeCategory.Winner;
            if (!pi.Dies) return NodeCategory.ProbablyLoggedOut;
            if (pi.Killer == null && pi.Dies) return NodeCategory.DiedToEnvironment;
            return NodeCategory.Other;
        }

        enum NodeCategory
        {
            Winner,
            ProbablyLoggedOut,
            DiedToEnvironment,
            Other
        }

        void WriteCluster(GraphVizWriter gvw, string clusterKey, string clusterLabel, IEnumerable<PlayerInfo> playersInCluster)
        {
            // Put the DiedToEnvironment-ers in a box
            gvw.RawWrite("subgraph " + clusterKey + " {");
            gvw.WriteKvp("label", clusterLabel);
            gvw.WriteKvp("style", "filled");
            gvw.WriteKvp("color", "lightgrey");

            foreach (var player in playersInCluster)
            {
                gvw.RawWrite("   \"" + player.Name + "\" [label=\"" + BuildNodeLabel(player).Replace("\"", "\"\"") + "\"];");
            }

            gvw.RawWrite("}");
        }

        string BuildNodeLabel(PlayerInfo player)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(player.Name);

            foreach (var evtGroup in player.Events.GroupBy(x => x.Type))
            {
                sb.Append(evtGroup.Key);

                var times = evtGroup.Where(x => x.Time != null).Select(x => MatchElapsed((DateTime)x.Time)).ToArray();

                if (times.Any())
                {
                    sb.Append(" @ ");
                    sb.Append(String.Join(", ", times));
                }

                var amounts = string.Join(",", evtGroup.Where(x => x.Amount != null).Select(x => x.Amount.ToString()));

                if (amounts.Length > 0)
                {
                    sb.Append("(" + amounts + ")");
                }
                 
                sb.Append("\\n");
            }

            if (player.Dies)
            {
                sb.Append("\\n\\nDied: " + MatchElapsed((DateTime)player.DiesAt));
            }

            return sb.ToString();
        }

        string MatchElapsed(DateTime t)
        {
            var elapsed = t - _matchStartTime;

            return ((int)elapsed.TotalMinutes) + "m" + elapsed.Seconds + "s";
        }

        void DumpDbnoGraph(GraphVizWriter gvw, PlayerInfo p, bool ignoreBots)
        {
            foreach (var victim in p.Dbnos)
            {
                if (ignoreBots && victim.IsBot) continue;

                // If the killer was also the knocker, don't write this edge.
                if (victim.Killer != p)
                {
                    gvw.WriteEdge(p.Name, victim.Name, "[color=blue label=" + p.GetDamageDealtTo(victim.AccountId) + "]");
                }
            }
        }
        void DumpReviveesGraph(GraphVizWriter gvw, PlayerInfo p)
        {
            foreach (var victim in p.Revivees)
            {
                gvw.WriteEdge(p.Name, victim.Name, "[color=green label=Revived]");
            }
        }

        void DumpKillGraph(GraphVizWriter gvw, PlayerInfo p, bool ignoreBots)
        {
            foreach (var victim in p.Kills)
            {
                if (!ignoreBots || !victim.IsBot)
                {
                    gvw.WriteEdge(p.Name, victim.Name, "[color=red label=\"" + p.GetDamageDealtTo(victim.AccountId) + " | " + victim.GetDamageDealtTo(p.AccountId) + "\"]");
                }
            }
        }

        void DumpAssistGraph(GraphVizWriter gvw, PlayerInfo p, bool ignoreBots)
        {
            foreach (var victim in p.Assists)
            {
                if (!ignoreBots || !victim.IsBot)
                {
                    gvw.WriteEdge(p.Name, victim.Name, "[color=black   style=dashed  label=" + p.GetDamageDealtTo(victim.AccountId) + "]");
                }
            }
        }

        void DumpDamageGraph(StreamWriter sw, PlayerInfo p, Dictionary<string, PlayerInfo> players)
        {
            foreach (var victimAccountInfo in p.DamageDealtByAccountId)
            {
                if (p.Kills.Any(x => x.AccountId == victimAccountInfo.Key)) continue;

                var victim = players[victimAccountInfo.Key];

                sw.WriteLine("   \"" + p.Name + "\"->\"" + victim.Name + "\" [color=lightgreen style=dotted label=" + p.GetDamageDealtTo(victim.AccountId) + "];");
            }
        }
    }
}
