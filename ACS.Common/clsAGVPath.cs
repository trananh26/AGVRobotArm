using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Common
{
    public class clsAGVPath
    {
    }
    public class PredictLine
    {
        public List<string> GetPath(string source, string dest)
        {
            Logic g = new Logic();
            List<string> result = g.Dijkstra(source, dest);
            return result;
        }
    }

    class Edge
    {
        public string u { get; set; }
        public string v { get; set; }
        public int w { get; set; }

    }

    public class Logic
    {
        List<string> lstNodes = new List<string>();
        List<Edge> lstEdges = new List<Edge>();

        public Logic()
        {
            string[] dataNodes = System.IO.File.ReadAllLines("Nodes.txt");
            foreach (string dataNode in dataNodes)
            {
                string[] s = dataNode.Split('\t');
                lstNodes.Add(s[0]);
            }
            string[] dataEdges = System.IO.File.ReadAllLines("Edges.txt");
            foreach (string dataEdge in dataEdges)
            {
                string[] s = dataEdge.Split('\t');
                lstEdges.Add(new Edge { u = s[0], v = s[1], w = int.Parse(s[2]) });
                lstEdges.Add(new Edge { u = s[1], v = s[0], w = int.Parse("2000") });
            }
            // Edit Edges Speacial
            foreach (var e in lstEdges)
            {
                if (e.u == "2106" && e.v == "3106") e.w = 2000;
                if (e.u == "3106" && e.v == "2106") e.w = 2000;
                //if (e.u == "3013" && e.v == "3014") e.w = 2000;
                //if (e.u == "3003" && e.v == "3004") e.w = 2000;

                //if (e.u == "2027" && e.v == "2028") e.w = 5000;
                //if (e.u == "2028" && e.v == "2027") e.w = 5000;

                //if (e.u == "3037" && e.v == "3038") e.w = 5000;
                //if (e.u == "3038" && e.v == "3037") e.w = 5000;

                //if (e.u == "3017" && e.v == "3018") e.w = 5000;
                //if (e.u == "3018" && e.v == "3017") e.w = 5000;

                //if (e.u == "3045" && e.v == "3046") e.w = 2000;
                //if (e.u == "3047" && e.v == "3048") e.w = 2000;
                //if (e.u == "2046" && e.v == "2045") e.w = 2000;
                //if (e.u == "2044" && e.v == "2043") e.w = 2000;
            }
        }
        public List<string> Dijkstra(string source, string dest)
        {
            List<string> pathResult = new List<string>();
            Dictionary<string, int> dictDistance = new Dictionary<string, int>();
            Dictionary<string, string> dictNodeNode = new Dictionary<string, string>();
            foreach (var node in lstNodes)
            {
                dictDistance[node] = int.MaxValue;
                dictNodeNode[node] = null;
            }
            dictDistance[source] = 0;
            string nodeCheck = string.Empty;
            while (lstNodes.Count > 0)
            {
                Dictionary<string, int> dictDisTemp = new Dictionary<string, int>();
                foreach (var node in lstNodes)
                {
                    dictDisTemp[node] = dictDistance[node];
                }
                nodeCheck = dictDisTemp.Where(k => k.Value == dictDisTemp.Values.Min()).First().Key;
                if (nodeCheck == dest)
                {
                    break;
                }
                List<Edge> lstEdgeCheck = new List<Edge>();
                foreach (var edge in lstEdges)
                {
                    if (nodeCheck == edge.u)
                    {
                        lstEdgeCheck.Add(edge);
                    }
                }
                foreach (var edge in lstEdgeCheck)
                {
                    if (dictDistance[edge.v] > dictDistance[edge.u] + edge.w)
                    {
                        dictDistance[edge.v] = dictDistance[edge.u] + edge.w;
                        dictNodeNode[edge.v] = edge.u;
                    }
                }
                lstNodes.Remove(nodeCheck);
            }
            string NodeTemp = dest;
            while (NodeTemp != null)
            {
                pathResult.Insert(0, NodeTemp);
                NodeTemp = dictNodeNode[NodeTemp];
            }
            return pathResult;
        }
    }
}
