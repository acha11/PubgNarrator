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
        public void Generate(StreamWriter sw, Dictionary<string, PlayerInfo> players, string title)
        {
            GraphVizWriter writer = new GraphVizWriter(sw);

            writer.RawWrite("digraph G {");

            writer.WriteKvp("rankdir", "LR");
            writer.WriteKvp("labelloc", "tl");
            writer.WriteKvp("label", title);
            writer.WriteKvp("fontsize", "30");
            writer.WriteKvp("fontname", "Verdana");

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

                sw.WriteLine("   \"" + p.Name + "\" [fillcolor=\"" + fillcolor + "\", style=\"filled\"];");
            }

            WriteCluster(writer, "cluster_0", "Winners", players.Values.Where(x => x.IsWinner).Select(x => x.Name));
            WriteCluster(writer, "cluster_1", "Probably logged out", players.Values.Where(x => !x.Dies && !x.IsWinner).Select(x => x.Name));
            WriteCluster(writer, "cluster_2", "Died to environment", players.Values.Where(x => x.Killer == null && x.Dies).Select(x => x.Name));

            foreach (var p in players.Values)
            {
                DumpKillGraph(writer, p);
                DumpAssistGraph(writer, p);
                //DumpDamageGraph(p, players);
            }

            writer.RawWrite("}");
        }

        void WriteCluster(GraphVizWriter gvw, string clusterKey, string clusterLabel, IEnumerable<string> clusterNodeKeys)
        {
            // Put the DiedToEnvironment-ers in a box
            gvw.RawWrite("subgraph " + clusterKey + " {");
            gvw.WriteKvp("label", clusterLabel);
            gvw.WriteKvp("style", "filled");
            gvw.WriteKvp("color", "lightgrey");

            foreach (var nodeKey in clusterNodeKeys)
            {
                gvw.RawWrite("   \"" + nodeKey + "\";");
            }

            gvw.RawWrite("}");
        }

        void DumpKillGraph(GraphVizWriter gvw, PlayerInfo p)
        {
            foreach (var victim in p.Kills)
            {
                gvw.WriteEdge(p.Name, victim.Name, "[color=red label=" + p.GetDamageDealtTo(victim.AccountId) + "]");
            }
        }

        void DumpAssistGraph(GraphVizWriter gvw, PlayerInfo p)
        {
            foreach (var victim in p.Assists)
            {
                gvw.WriteEdge(p.Name, victim.Name, "[color=black   style=dashed  label=" + p.GetDamageDealtTo(victim.AccountId) + "]");
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
