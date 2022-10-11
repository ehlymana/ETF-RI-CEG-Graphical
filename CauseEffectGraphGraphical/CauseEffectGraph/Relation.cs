using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CauseEffectGraph
{
    public class Relation
    {
        #region Properties

        public List<Node> Causes { get; set; }
        public List<Node> Effects { get; set; }

        public bool Result { get; set; }
        public string Type { get; set; }
        public bool LogicalRelation { get; set; }

        #endregion

        #region Constructor

        public Relation(string type)
        {
            Causes = new List<Node>();
            Effects = new List<Node>();
            Type = type;

            List<string> logical = new List<string>()
            { "DIR", "NOT", "AND", "OR", "NAND", "NOR" };

            LogicalRelation = logical.Contains(type);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Calculate the effect values if the relation is logical
        /// </summary>
        public void CalculateResult()
        {
            if (!LogicalRelation)
                return;

            if (Type == "DIR")
            {
                Effects[0].Result = Causes[0].Result;
                Result = Causes[0].Result;
            }

            else if (Type == "NOT")
            {
                Effects[0].Result = !Causes[0].Result;
                Result = !Causes[0].Result;
            }

            else if (Type == "AND")
            {
                Effects[0].Result = Causes.All(x => x.Result == true);
                Result = Causes.All(x => x.Result == true);
            }

            else if (Type == "OR")
            {
                Effects[0].Result = Causes.Any(x => x.Result == true);
                Result = Causes.Any(x => x.Result == true);
            }

            else if (Type == "NAND")
            {
                Effects[0].Result = !Causes.All(x => x.Result == true);
                Result = !Causes.All(x => x.Result == true);
            }

            else if (Type == "NOR")
            {
                Effects[0].Result = !Causes.Any(x => x.Result == true);
                Result = !Causes.Any(x => x.Result == true);
            }
        }

        #endregion
    }
}
