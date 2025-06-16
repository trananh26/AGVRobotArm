using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Common
{
    public class MapData
    {
    }
    public class Node
    {
        public string ID { get; set; }
        public string TYPE { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Direction { get; set; }
        public bool show { get; set; }
    }

    public class PathNode
    {
        public string Node { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class Link
    {
        public string ID { get; set; }
        public string Source { get; set; }
        public string Dest { get; set; }
        public string Distance { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
    }

    public class AGV
    {
        public string ID { get; set; }
        public string NODE { get; set; }
        public string CrNode { get; set; }
        public string PreNode { get; set; }
        public string NEXTNODE { get; set; }
        public int NEXT_X { get; set; }
        public int NEXT_Y { get; set; }
        public string BAYID { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public string ALARM { get; set; }
        public int BATTERY { get; set; }
        public string STATE { get; set; }
        public string STATUS { get; set; }
        public string RUNSTATE { get; set; }
        public string CONNECTSTATE { get; set; }
        public string TransportCommand { get; set; }
        public string Path { get; set; }
        public string Direction { get; set; }
        public string Dest { get; set; }
    }

    public class PathFollowTurnNode
    {
        public Link TurnPath { get; set; }
        public List<Link> HorPath { get; set; }
    }
}
