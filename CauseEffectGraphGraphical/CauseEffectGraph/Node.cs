using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CauseEffectGraph
{
    public class Node
    {
        #region Properties
        public int X { get; set; }
        public int Y { get; set; }
        public int Index { get; set; }
        public string Type { get; set; }
        public bool Result { get; set; }

        #endregion

        #region Constructor

        public Node (int x, int y, int index, string type)
        {
            X = x;
            Y = y;
            Index = index;
            Type = type;
        }

        #endregion
    }
}
