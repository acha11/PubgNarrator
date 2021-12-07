using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubgApiTest.GraphViz
{
    public class GraphVizWriter
    {
        StreamWriter _sw;

        public GraphVizWriter(StreamWriter sw)
        {
            _sw = sw;
        }

        public void RawWrite(string s)
        {
            _sw.WriteLine(s);
        }

        public void WriteKvp(string key, string value)
        {
            _sw.WriteLine("   " + key + "=\"" + value.Replace("\"", "\"\"") + "\";");
        }

        public void WriteEdge(string source, string sink, string attributes)
        {
            _sw.WriteLine("   \"" + source + "\"->\"" + sink + "\" " + attributes + ";");
        }
    }
}
