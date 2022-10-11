using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CauseEffectGraph
{
    public partial class Form1 : Form
    {
        #region Attributes

        // attribute used to refresh the panel
        Graphics g;

        // attributes for working with nodes
        int radius = 50, distance = 16, penWeight = 2;
        List<Node> causes = new List<Node>();
        List<Node> intermediates = new List<Node>();
        List<Node> effects = new List<Node>();
        Node nodeToBeMoved;
        int dragDropOption = 0;

        // attributes for working with relations
        List<Relation> relations = new List<Relation>();
        Relation temporaryRelation = null;
        bool allRelationsActive = true;

        #endregion

        #region Constructor

        public Form1()
        {
            InitializeComponent();
            g = this.panel1.CreateGraphics();
            toolStripStatusLabel1.Text = "";
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

        #endregion

        #region Drag-And-Dropping Nodes

        /// <summary>
        /// Marking that the drag-and-drop operation for causes is initiated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_MouseDown(object sender, MouseEventArgs e)
        {
            dragDropOption = 1;
            button1.DoDragDrop(button1.Text, DragDropEffects.Copy | DragDropEffects.Move);
        }

        /// <summary>
        /// Marking that the drag-and-drop operation for causes is initiated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_MouseDown(object sender, MouseEventArgs e)
        {
            dragDropOption = 2;
            button2.DoDragDrop(button2.Text, DragDropEffects.Copy | DragDropEffects.Move);
        }

        /// <summary>
        /// Marking that the drag-and-drop operation for intermediates is initiated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button18_MouseDown(object sender, MouseEventArgs e)
        {
            dragDropOption = 3;
            button18.DoDragDrop(button18.Text, DragDropEffects.Copy | DragDropEffects.Move);
        }


        /// <summary>
        /// Allowing the nodes to be dropped onto the panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// Performing the drag-and-drop operation (add new or move existing node on the panel)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            toolStripStatusLabel1.Text = "";

            // calculating the relative cursor coordinates
            int x = panel1.PointToClient(Cursor.Position).X, y = panel1.PointToClient(Cursor.Position).Y;

            // add new node onto the panel (option 1 - cause, option 2 - effect, option 3 - intermediate)
            if (dragDropOption > 0 && dragDropOption < 4)
            {
                
                // drawing and adding the new node into the collection of corresponding nodes
                if (dragDropOption == 1)
                {
                    int index = DetermineNextNodeNumber(1);
                    causes.Add(new Node(x, y, index, "C"));
                }
                else if (dragDropOption == 2)
                {
                    int index = DetermineNextNodeNumber(2);
                    effects.Add(new Node(x, y, index, "E"));
                }
                else
                {
                    int index = DetermineNextNodeNumber(3);
                    intermediates.Add(new Node(x, y, index, "I"));
                }

                // reset drag-and-drop
                dragDropOption = 0;

                // refresh the panel (draw new nodes)
                Refresh();
                g = this.panel1.CreateGraphics();
                Redraw();

                // refresh the listBox which contains all nodes
                RefreshNodeList();
            }
            // move existing node on the panel (option 4)
            else if (dragDropOption == 4)
            {
                // node has not been properly selected
                // this code block solves a bug related to nodes positioned too close to each other
                if (nodeToBeMoved == null)
                {
                    toolStripStatusLabel1.Text = "You have selected nodes too close to each other - try again with a bigger distance or delete and re-add the nodes!";
                    dragDropOption = 0;
                    nodeToBeMoved = null;
                    return;
                }

                toolStripStatusLabel1.Text = "";
                Node newNode = null;
                int index = nodeToBeMoved.Index;

                // remove the node with old and add the node with new coordinates
                if (causes.Contains(nodeToBeMoved))
                {
                    newNode = new Node(x, y, index, "C");
                    causes.Add(newNode);
                    causes.Remove(nodeToBeMoved);
                }
                else if (effects.Contains(nodeToBeMoved))
                {
                    newNode = new Node(x, y, index, "E");
                    effects.Add(newNode);
                    effects.Remove(nodeToBeMoved);
                }
                else if (intermediates.Contains(nodeToBeMoved))
                {
                    newNode = new Node(x, y, index, "I");
                    intermediates.Add(newNode);
                    intermediates.Remove(nodeToBeMoved);
                }

                // update all relations to contain the new coordinates
                UpdateAllRelations(nodeToBeMoved, newNode);

                // reload the form (draw all nodes again in order to erase the old node on panel)
                Refresh();
                g = this.panel1.CreateGraphics();
                Redraw();

                // reset drag-and-drop
                dragDropOption = 0;
                nodeToBeMoved = null;
            }
        }

        /// <summary>
        /// Helper function to determine the next node number
        /// When nodes are deleted, their numbers are empty and should be reused
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private int DetermineNextNodeNumber(int type)
        {
            // the lowest node number is 1
            int i = 1;

            // determine whether to search for the next cause or effect node number
            List<Node> list;
            if (type == 1)
                list = causes;
            else if (type == 2)
                list = effects;
            else
                list = intermediates;

            // order the list by numbers for easier detection
            list = list.OrderBy(n => n.Index).ToList();
            foreach (var node in list)
            {
                if (node.Index == i)
                    i++;
                else
                    break;
            }
            return i;
        }

        /// <summary>
        /// Update all node coordinates in all existing relations
        /// when a node is moved and its X and Y coordinates change
        /// </summary>
        /// <param name="oldNode"></param>
        /// <param name="newNode"></param>
        public void UpdateAllRelations(Node oldNode, Node newNode)
        {
            foreach (var relation in relations)
            {
                var node = relation.Causes.Find(n => n.X == oldNode.X && n.Y == oldNode.Y);

                if (node != null)
                {
                    node.X = newNode.X;
                    node.Y = newNode.Y;
                }

                node = relation.Effects.Find(n => n.X == oldNode.X && n.Y == oldNode.Y);

                if (node != null)
                {
                    node.X = newNode.X;
                    node.Y = newNode.Y;
                }
            }
        }

        /// <summary>
        /// Start drag-and-drop operation of node that is already on the panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            // do drag-and-drop only if the coordinates on which the user has clicked
            // belong to a node
            int x = panel1.PointToClient(Cursor.Position).X, y = panel1.PointToClient(Cursor.Position).Y;
            nodeToBeMoved = FindNode(x, y);

            // begin the move operation if a relation addition has not been started
            if (nodeToBeMoved != null && temporaryRelation == null)
            {
                dragDropOption = 4;
                panel1.DoDragDrop(panel1.Text, DragDropEffects.Copy | DragDropEffects.Move);
            }

            // if a relation addition has been started, add the selected node to the new relation
            else if (nodeToBeMoved != null && temporaryRelation != null)
            {
                AddNodeToRelation(ref nodeToBeMoved);
                nodeToBeMoved = null;
            }
        }

        /// <summary>
        /// A helper method which determines whether the coordinates where the user has clicked
        /// belong to any of the existing nodes or not
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        Node FindNode(int x, int y)
        {
            List<Node> nodes = new List<Node>();
            foreach (var cause in causes)
            {
                int neighborhoodXDown = cause.X - distance, neighborhoodXUp = cause.X + (radius - distance),
                    neighborhoodYDown = cause.Y - distance, neighborhoodYUp = cause.Y + (radius - distance);

                if (x >= neighborhoodXDown && x <= neighborhoodXUp &&
                    y >= neighborhoodYDown && y <= neighborhoodYUp)
                    nodes.Add(cause);
            }

            foreach (var effect in effects)
            {
                int neighborhoodXDown = effect.X - distance, neighborhoodXUp = effect.X + (radius - distance),
                    neighborhoodYDown = effect.Y - distance, neighborhoodYUp = effect.Y + (radius - distance);

                if (x >= neighborhoodXDown && x <= neighborhoodXUp &&
                    y >= neighborhoodYDown && y <= neighborhoodYUp)
                    nodes.Add(effect);
            }

            foreach (var intermediate in intermediates)
            {
                int neighborhoodXDown = intermediate.X - distance, neighborhoodXUp = intermediate.X + (radius - distance),
                    neighborhoodYDown = intermediate.Y - distance, neighborhoodYUp = intermediate.Y + (radius - distance);

                if (x >= neighborhoodXDown && x <= neighborhoodXUp &&
                    y >= neighborhoodYDown && y <= neighborhoodYUp)
                    nodes.Add(intermediate);
            }

            // if multiple nodes are in the neighborhood, do not allow one to be selected
            // doing so would create a graphical bug
            if (nodes.Count != 1)
                return null;
            else
                return nodes.ElementAt(0);
        }

        /// <summary>
        /// Draw a new node on the panel
        /// </summary>
        /// <param name="g"></param>
        /// <param name="option"></param>
        /// <param name="nodeNumber"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void DrawNode(Graphics g, int option, int nodeNumber, int x, int y)
        {
            Pen pen = new Pen(Color.LightSlateGray, penWeight);
            SolidBrush brush, brush2 = new SolidBrush(Color.Black);
            string name;

            // different appearance of nodes based on option (1 - cause, 2 - effect)
            if (option == 1)
            {
                brush = new SolidBrush(Color.Lime);
                name = "C" + nodeNumber.ToString();
            }
            else if (option == 2)
            {
                brush = new SolidBrush(Color.Magenta);
                name = "E" + nodeNumber.ToString();
            }
            else
            {
                brush = new SolidBrush(Color.CornflowerBlue);
                name = "I" + nodeNumber.ToString();
            }

            // draw the node on the panel
            g.DrawEllipse(pen, x - distance, y - distance, radius, radius);
            g.FillEllipse(brush, x - distance, y - distance, radius, radius);
            g.DrawString(name, this.Font, brush2, x, y);
        }

        #endregion

        #region Relation Addition

        #region Selecting Relation Type

        /// <summary>
        /// DIR relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = true;
            buttonDrop.Visible = true;
            temporaryRelation = new Relation("DIR");
        }

        /// <summary>
        /// NOT relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = true;
            buttonDrop.Visible = true;
            temporaryRelation = new Relation("NOT");
        }

        /// <summary>
        /// AND relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = true;
            buttonDrop.Visible = true;
            temporaryRelation = new Relation("AND");
        }

        /// <summary>
        /// OR relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = true;
            buttonDrop.Visible = true;
            temporaryRelation = new Relation("OR");
        }

        /// <summary>
        /// NAND relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = true;
            buttonDrop.Visible = true;
            temporaryRelation = new Relation("NAND");
        }

        /// <summary>
        /// NOR relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = true;
            buttonDrop.Visible = true;
            temporaryRelation = new Relation("NOR");
        }

        /// <summary>
        /// EXC relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button9_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = true;
            buttonDrop.Visible = true;
            temporaryRelation = new Relation("EXC");
        }

        /// <summary>
        /// INC relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = true;
            buttonDrop.Visible = true;
            temporaryRelation = new Relation("INC");
        }

        /// <summary>
        /// REQ relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button11_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = true;
            buttonDrop.Visible = true;
            temporaryRelation = new Relation("REQ");
        }

        /// <summary>
        /// MSK relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button12_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = true;
            buttonDrop.Visible = true;
            temporaryRelation = new Relation("MSK");
        }

        /// <summary>
        /// EXC Δ INC relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button13_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = true;
            buttonDrop.Visible = true;
            temporaryRelation = new Relation("EXCINC");
        }

        #endregion
        
        /// <summary>
        /// Adding new relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonFinish_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = false;
            buttonDrop.Visible = false;

            string text = "";
            List<string> logicRelations = new List<string>()
            { "AND", "OR", "NAND", "NOR" };
            List<string> causalConstraints = new List<string>()
            { "EXC", "INC", "EXCINC" };

            // no causes or effects selected - relation rejected
            if (temporaryRelation.Causes.Count == 0 && temporaryRelation.Effects.Count == 0)
                text = "No nodes are selected!";

            // NOT and DIR are unary relations - exactly one cause and one relation must be selected
            else if ((temporaryRelation.Type == "NOT" || temporaryRelation.Type == "DIR") && 
                (temporaryRelation.Causes.Count != 1 || temporaryRelation.Effects.Count != 1))
                text = "DIR and NOT relations must have exactly one cause and effect node!";

            // logic relations must have at least two causes and exactly one effect
            else if (logicRelations.Contains(temporaryRelation.Type) &&
                (temporaryRelation.Causes.Count < 2 || temporaryRelation.Effects.Count != 1))
                text = "Logical relations must have at least two causes and exactly one effect node!";

            // causal constraints can be applied only to causes and to at least two causes
            else if (causalConstraints.Contains(temporaryRelation.Type) && 
                (temporaryRelation.Effects.Count > 0 || temporaryRelation.Causes.Count < 2))
                text = "Causal constraints must be applied to at least two causes and causes only!";

            // REQ constraint is unary (exactly two causes) and can be applied only to causes
            else if (temporaryRelation.Type == "REQ" && 
                (temporaryRelation.Causes.Count != 2 || temporaryRelation.Effects.Count > 0))
                text = "REQ constraint must be applied to exactly to causes and causes only!";

            // MSK constraint is unary (exactly two effects) and can be applied only to effects
            else if (temporaryRelation.Type == "MSK" && 
                (temporaryRelation.Effects.Count != 2 || temporaryRelation.Causes.Count > 0))
            {
                text = "MSK constraint must be applied to exactly to effect and effects only!";
            }

            toolStripStatusLabel1.Text = text;

            // relation passed all tests - it is valid
            if (text.Length < 1)
                relations.Add(temporaryRelation);
                

            // reset relation - erase temporary nodes
            temporaryRelation = null;

            // refresh form in order to draw the new relation
            Refresh();
            g = this.panel1.CreateGraphics();
            Redraw();

            // add the new relation to the listBox containing relations which can be deleted
            RefreshRelationList();
        }

        /// <summary>
        /// Choosing not to add the current relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonDrop_Click(object sender, EventArgs e)
        {
            buttonFinish.Visible = false;
            buttonDrop.Visible = false;

            temporaryRelation = null;

            Refresh();
            Redraw();
        }

        /// <summary>
        /// Draw a new logical relation on the panel
        /// </summary>
        /// <param name="relation"></param>
        public void DrawLogicalRelation(Graphics g, Relation relation)
        {
            List<Tuple<int, int>> beginningCoordinates = new List<Tuple<int, int>>(),
                      endingCoordinates = new List<Tuple<int, int>>();

            // calculating the place for the beginning of the relation lines
            foreach (var cause in relation.Causes)
            {
                int x = cause.X + (radius - distance),
                    y = cause.Y + (radius / 2 - distance);
                beginningCoordinates.Add(new Tuple<int, int>(x, y));
            }

            // calculating the place for the end of the relation lines
            foreach (var effect in relation.Effects)
            {
                int x = effect.X - distance,
                    y = effect.Y + (radius / 2 - distance);
                endingCoordinates.Add(new Tuple<int, int>(x, y));
            }

            Pen pen = new Pen(Color.Black, penWeight);

            // draw the lines
            for (int i = 0; i < beginningCoordinates.Count; i++)
            {
                for (int j = 0; j < endingCoordinates.Count; j++)
                {
                    g.DrawLine(pen, beginningCoordinates[i].Item1, beginningCoordinates[i].Item2,
                               endingCoordinates[j].Item1, endingCoordinates[j].Item2);
                }
            }

            List<string> logicalRelations = new List<string>()
            { "AND", "OR", "NAND", "NOR" };

            // drawing additional symbols if necessary
            // DIR - no additional symbols
            // NOT
            if (relation.Type == "NOT")
            {
                // add wave line at the center of the relation line

                // finding the middle of the relation line
                int xLength = endingCoordinates[0].Item1 - beginningCoordinates[0].Item1,
                    yLength = endingCoordinates[0].Item2 - beginningCoordinates[0].Item2,
                    xMiddle = beginningCoordinates[0].Item1 + xLength / 2,
                    yMiddle = beginningCoordinates[0].Item2 + yLength / 2;

                // defining wave width and height
                int lineLength = 20,
                    offset = 2,
                    lineHeight = 5;

                // the wave line is composed four points:
                // POINT 1 - start of the wave, on the relation line
                Point p1 = new Point(xMiddle - lineLength, yMiddle);
                // POINT 2 - middle of the line, above the relation line
                Point p2 = new Point(xMiddle - offset, yMiddle + lineHeight);
                // POINT 3 - middle of the line, below the relation line
                Point p3 = new Point(xMiddle + offset, yMiddle - lineHeight);
                // POINT 4 - end of the wave, on the relation line
                Point p4 = new Point(xMiddle + lineLength, yMiddle);

                // drawing the wave line defined by four points
                Point[] curvePoints = { p1, p2, p3, p4 };
                float tension = 1.0F;
                g.DrawCurve(pen, curvePoints, tension);
            }
            
            // all other relations require an arch to be drawn
            else if (logicalRelations.Contains(relation.Type))
            {                
                // the highest, middle and lowest relation line must be found
                beginningCoordinates = beginningCoordinates.OrderBy(element => element.Item2).ToList();
                
                int noOfElements = beginningCoordinates.Count,
                    offset = 20;

                // the arch is composed of three points:
                // POINT 1 - beginning of the arch, near the end of the highest relation line
                var d1 = FindPointOnRelationLine(beginningCoordinates[0], endingCoordinates[0], offset);
                // POINT 3 - end of the arch, near the end of the lowest relation line
                var d3 = FindPointOnRelationLine(beginningCoordinates[noOfElements - 1], endingCoordinates[0], offset);
                // POINT 2 - middle of the arch, at the center of the middle relation line
                Tuple<int, int> d2;
                // if the logical relation is composed of more than two causes, the middle line will be at one of the relation lines
                if (noOfElements > 2)
                    d2 = FindPointOnRelationLine(beginningCoordinates[noOfElements / 2], endingCoordinates[0], offset * 2);
                // if there are just two causes, there is no middle line - it must be calculated manually
                else
                {
                    d2 = new Tuple<int, int>(d1.Item1 - offset * 2,
                                                 d1.Item2 + (d3.Item2 - d1.Item2) / 2);
                }

                Point p1 = new Point(d1.Item1, d1.Item2);
                Point p2 = new Point(d2.Item1, d2.Item2);
                Point p3 = new Point(d3.Item1, d3.Item2);

                // drawing the defined arch
                Point[] curvePoints = { p1, p2, p3 };
                float tension = 1.0F;
                g.DrawCurve(pen, curvePoints, tension);

                // it is also necessary to draw the corresponding logical relation sign
                // the location of the sign should be closeby the middle arch point
                // but placed with an offset to the left for higher visibility

                string relationSign = "";
                float fontSize = this.Font.Size;

                if (relation.Type == "AND")
                    relationSign = "∧";
                else if (relation.Type == "OR")
                    relationSign = "∨";
                else if (relation.Type == "NAND")
                {
                    relationSign = "⊼";
                    fontSize += 8;
                }
                else if (relation.Type == "NOR")
                {
                    relationSign = "⊽";
                    fontSize += 8;
                }

                // drawing the defined sign on the form
                SolidBrush brush = new SolidBrush(Color.Black);
                Font font = new Font(this.Font.FontFamily, fontSize, this.Font.Style);
                g.DrawString(relationSign, font, brush, d2.Item1 - offset, d2.Item2 - offset / 2);
            }
        }

        /// <summary>
        /// Helper function for finding the desired point on the relation line
        /// (the offset represent the distance starting from the end of the line towards the beginning)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        Tuple<int, int> FindPointOnRelationLine(Tuple<int, int> p1, Tuple<int, int> p2, int offset)
        {
            double k = (p2.Item2 - p1.Item2) * 1.0 / (p2.Item1 - p1.Item1),
                   n = p1.Item2 - k * p1.Item1,
                   x = p2.Item1 - offset,
                   y = k * x + n;

            return new Tuple<int, int>((int)x, (int)y);
        }

        /// <summary>
        /// Draw a new constraint on the panel
        /// </summary>
        /// <param name="relation"></param>
        public void DrawConstraint(Graphics g, Relation relation)
        {
            List<Tuple<int, int>> beginningCoordinates = new List<Tuple<int, int>>(),
                                  endCoordinates = new List<Tuple<int, int>>();

            // calculating the place for the end of the constraint lines
            // since causal constraints begin to the left of the causes,
            // and the beginning of cause nodes are the ends of the lines
            foreach (var cause in relation.Causes)
            {
                int x = cause.X - distance,
                    y = cause.Y + (radius / 2 - distance);
                endCoordinates.Add(new Tuple<int, int>(x, y));
            }

            // calculating the place for the beginning of the relation lines
            // since effect constraints begin at the end of effect nodes
            foreach (var effect in relation.Effects)
            {
                int x = effect.X + (radius - distance),
                    y = effect.Y + (radius / 2 - distance);
                beginningCoordinates.Add(new Tuple<int, int>(x, y));
            }

            Pen pen = new Pen(Color.Black, penWeight);

            // all constraints are defined with dashed lines
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

            List<string> causalRelations = new List<string>()
            { "EXC", "INC", "EXCINC" };

            // all causal relations except for REQ do not require an arrowhead
            if (causalRelations.Contains(relation.Type))
            {
                // end coordinates should be sorted by coordinate height
                endCoordinates = endCoordinates.OrderBy(element => element.Item2).ToList();

                int noOfEelements = endCoordinates.Count,
                    offset = 30;

                // the beginning of the line is a point at the middle between the lowest and
                // highest point of all causes in the constraint
                int xMiddle = endCoordinates[0].Item1 - offset,
                    yMiddle = endCoordinates[0].Item2 + (endCoordinates[noOfEelements - 1].Item2 - endCoordinates[0].Item2) / 2;

                // specifying the calculated middle point as the beginning point
                beginningCoordinates.Clear();
                beginningCoordinates.Add(new Tuple<int, int>(xMiddle, yMiddle));

                // drawing the lines
                for (int i = 0; i < beginningCoordinates.Count; i++)
                {
                    for (int j = 0; j < endCoordinates.Count; j++)
                    {
                        g.DrawLine(pen, beginningCoordinates[i].Item1, beginningCoordinates[i].Item2,
                                   endCoordinates[j].Item1, endCoordinates[j].Item2);
                    }
                }

                // different causal constraints require different signs
                string drawSign = "";

                if (relation.Type == "EXC")
                    drawSign = "E";
                else if (relation.Type == "INC")
                    drawSign = "I";
                else if (relation.Type == "EXCINC")
                    drawSign = "O";

                // drawing the specified sign
                SolidBrush brush = new SolidBrush(Color.Black);
                g.DrawString(drawSign, this.Font, brush, xMiddle - offset, yMiddle);
            }

            // REQ and MSK constraints (unary) require an arch and an arrowhead
            else
            {
                Point p1 = new Point(0, 0), p2 = new Point(0, 0), p3 = new Point(0, 0);
                string drawSign = "";
                int offset = 20;

                // REQ is a causal constraint so it is placed to the left of the causes
                if (relation.Type == "REQ")
                {
                    int noOfElements = endCoordinates.Count;

                    // calculating the middle point
                    int xMiddle = endCoordinates[0].Item1 - offset,
                        yMiddle = endCoordinates[0].Item2 + (endCoordinates[noOfElements - 1].Item2 - endCoordinates[0].Item2) / 2;

                    // specifying the calculated middle point as the beginning point
                    beginningCoordinates.Clear();
                    beginningCoordinates.Add(new Tuple<int, int>(xMiddle, yMiddle));

                    // arch definition
                    p1 = new Point(endCoordinates[0].Item1, endCoordinates[0].Item2);
                    p2 = new Point(beginningCoordinates[0].Item1, beginningCoordinates[0].Item2);
                    p3 = new Point(endCoordinates[1].Item1, endCoordinates[1].Item2);

                    // specifying and drawing the draw sign
                    drawSign = "R";
                    SolidBrush brush = new SolidBrush(Color.Black);
                    g.DrawString(drawSign, this.Font, brush, p2.X - offset, p2.Y);

                    // drawing the arrowhead
                    DrawArrowhead(pen, p3, -1);
                }

                // REQ is an effect constraint so it is placed to the right of the effects
                else if (relation.Type == "MSK")
                {
                    int noOfElements = beginningCoordinates.Count;

                    // calculating the middle point
                    int xMiddle = beginningCoordinates[0].Item1 + (radius - offset) + offset,
                        yMiddle = beginningCoordinates[0].Item2 + (beginningCoordinates[noOfElements - 1].Item2 - beginningCoordinates[0].Item2) / 2;

                    // specifying the calculated middle point as the ending point
                    endCoordinates.Clear();
                    endCoordinates.Add(new Tuple<int, int>(xMiddle, yMiddle));

                    // arch definition
                    p1 = new Point(beginningCoordinates[0].Item1, beginningCoordinates[0].Item2);
                    p2 = new Point(endCoordinates[0].Item1, endCoordinates[0].Item2);
                    p3 = new Point(beginningCoordinates[1].Item1, beginningCoordinates[1].Item2);

                    // specifying and drawing the draw sign
                    drawSign = "M";
                    SolidBrush brush = new SolidBrush(Color.Black);
                    g.DrawString(drawSign, this.Font, brush, p2.X + offset, p2.Y);

                    // drawing the arrowhead
                    DrawArrowhead(pen, p3, 1);
                }

                // drawing the arch
                Point[] curvePoints = { p1, p2, p3 };
                float tension = 1.0F;
                g.DrawCurve(pen, curvePoints, tension);
            }

        }

        /// <summary>
        /// Helper method to draw the arrowhead
        /// </summary>
        /// <param name="pen"></param>
        /// <param name="p"></param>
        /// <param name="nx"></param>
        /// <param name="ny"></param>
        /// <param name="length"></param>
        private void DrawArrowhead(Pen pen, PointF p, int factor, float nx = 2, float ny = 2, float length = 3)
        {
            float ax = length * (ny + nx); // 18
            float ay = length * (nx + ny); // 18
            PointF[] points =
            {
                new PointF(p.X + (factor * ax) , p.Y + (factor * ay)),
                p,
                new PointF(p.X + (factor * ax), p.Y - (factor * ay))
            };
            g.DrawLines(pen, points);
        }

        /// <summary>
        /// Activate and deactivate constraint display
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            allRelationsActive = !allRelationsActive;
            if (allRelationsActive)
            {
                checkBox1.Text = "Yes";
                checkBox1.BackColor = Color.LimeGreen;
            }
            else
            {
                checkBox1.Text = "No";
                checkBox1.BackColor = Color.Tomato;
            }

            // redraw all nodes and relations
            Refresh();
            g = this.panel1.CreateGraphics();
            Redraw();
        }

        /// <summary>
        /// Select the desired node to be added to the new relation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddNodeToRelation(ref Node node)
        {
            // ignore the click if relation addition has not been initiated
            if (temporaryRelation == null)
                return;

            // ignore the click if no node is selected
            if (node == null)
                return;

            // add the node to the temporary relation
            if (node.Type == "C")
                temporaryRelation.Causes.Add(node);
            else if (node.Type == "E")
            {
                // when there is an effect, all intermediates are causes
                if (temporaryRelation.Effects.Find(n => n.Type == "I") != null)
                {
                    foreach (var n in temporaryRelation.Effects)
                        if (n.Type == "I")
                            temporaryRelation.Causes.Add(n);

                    temporaryRelation.Effects.Clear();
                }
                temporaryRelation.Effects.Add(node);
            }
            // intermediate nodes can be both causes and effects
            // no cause node - intermediate node is the cause
            else if (node.Type == "I" && temporaryRelation.Causes.Count == 0)
                temporaryRelation.Causes.Add(node);
            // no effect node - intermediate node is the cause
            else if (node.Type == "I" && temporaryRelation.Effects.Count == 0)
                temporaryRelation.Effects.Add(node);
            // cause and effect nodes already exist (but only I effects) - the last node to be added is the effect
            else if (node.Type == "I" && temporaryRelation.Effects.Count > 0 && temporaryRelation.Effects.Find(n => n.Type == "E") == null)
            {
                temporaryRelation.Causes.Add(temporaryRelation.Effects[0]);
                temporaryRelation.Effects.Clear();
                temporaryRelation.Effects.Add(node);
            }
            // effect node (E) exists - intermediate is the cause
            else if (node.Type == "I" && temporaryRelation.Effects.Count > 0)
                temporaryRelation.Causes.Add(node);

            // draw a rectangle around the node
            Pen pen = new Pen(Color.Indigo, 4);
            g.DrawRectangle(pen, node.X - distance, node.Y - distance, radius, radius);
        }

        #endregion

        #region Refresh Panel
        
        /// <summary>
        /// Show general info about the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("This application was created at the Department of Computing and Informatics at the Faculty of Electrical Engineering of the University of Sarajevo. For more information, contact the application creator via email: ekrupalija1@etf.unsa.ba. All rights reserved.", "General Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Method that draws all nodes and relations defined in the collections
        /// </summary>
        private void Redraw()
        {
            // draw all cause nodes
            foreach (var cause in causes)
            {
                DrawNode(g, 1, cause.Index, cause.X, cause.Y);
            }

            // draw all effect nodes
            foreach (var effect in effects)
            {
                DrawNode(g, 2, effect.Index, effect.X, effect.Y);
            }

            // draw all intermediate nodes
            foreach (var intermediate in intermediates)
            {
                DrawNode(g, 3, intermediate.Index, intermediate.X, intermediate.Y);
            }

            // draw all relations
            foreach (var relation in relations)
            {
                List<string> logicalRelations = new List<string>()
                { "DIR", "NOT", "AND", "OR", "NAND", "NOR" };

                // logical relations are always drawn
                if (logicalRelations.Contains(relation.Type))
                    DrawLogicalRelation(g, relation);

                // constraints are drawn only if the user chooses the option
                else if (allRelationsActive)
                {
                    DrawConstraint(g, relation);
                }
            }

        }

        /// <summary>
        /// Refresh panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            
        }

        /// <summary>
        /// Redraw everything when the form size changes (maximize-minimize)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Resize(object sender, EventArgs e)
        {
            g = this.panel1.CreateGraphics();
            if (WindowState == FormWindowState.Maximized)
            {
                Redraw();
            }
            else
            {
                Refresh();
                Redraw();
            }
        }

        #endregion

        #region Erase

        /// <summary>
        /// Delete everything from the listBox containing nodes and reload it
        /// </summary>
        private void RefreshNodeList()
        {
            // erase everything from the list
            checkedListBox1.Items.Clear();

            // sort the list by name order
            causes = causes.OrderBy(node => node.Index).ToList();
            effects = effects.OrderBy(node => node.Index).ToList();
            intermediates = intermediates.OrderBy(node => node.Index).ToList();

            // add all causes named Cx to the list
            foreach (var node in causes)
            {
                checkedListBox1.Items.Add("C" + node.Index);
            }

            // add all effects named Ex to the list
            foreach (var node in effects)
            {
                checkedListBox1.Items.Add("E" + node.Index);
            }

            // add all intermediates named Ix to the list
            foreach (var node in intermediates)
            {
                checkedListBox1.Items.Add("I" + node.Index);
            }
        }

        /// <summary>
        /// Delete everything from the listBoxes containing relations and reload them
        /// </summary>
        private void RefreshRelationList()
        {
            // erase everything from the lists
            checkedListBox2.Items.Clear();
            checkedListBox3.Items.Clear();

            List<string> logicalRelations = new List<string>()
            { "DIR", "NOT", "AND", "OR", "NAND", "NOR" };

            // sort the lists by name order
            relations = relations.OrderBy(connection => connection.Type).ToList();

            // add all relations into one of the two lists
            foreach (var relation in relations)
            {
                // relation name format: TYPE - Cx/Ix, ... - Ex/Ix, ...
                string name = relation.Type + " - ";

                // for constraints which do not contain effects
                if (relation.Causes.Count < 1)
                    name += "- ";

                foreach (var cause in relation.Causes)
                    name += cause.Type + cause.Index + ", ";

                // erasing the extra comma
                name = name.Substring(0, name.Length - 2);
                name += " - ";

                // for constraints which do not contain effects
                if (relation.Effects.Count < 1)
                    name += "- ";

                foreach (var effect in relation.Effects)
                    name += effect.Type + effect.Index + ", ";

                // erasing the extra comma
                name = name.Substring(0, name.Length - 2);

                // adding the name into the appropriate list (logical relation vs. constraint)
                if (logicalRelations.Contains(relation.Type))
                    checkedListBox2.Items.Add(name);
                else
                    checkedListBox3.Items.Add(name);
            }
        }

        /// <summary>
        /// Erasing nodes selected in the listBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button14_Click(object sender, EventArgs e)
        {
            // if no nodes are selected, deleting is not allowed
            if (checkedListBox1.CheckedItems.Count < 1)
            {
                toolStripStatusLabel1.Text = "You need to select at least one node!";
                return;
            }

            toolStripStatusLabel1.Text = "";

            // beginning the search for nodes to be erased and all relations which contain them
            List<Node> nodesForErasing = new List<Node>();
            List<Relation> relationsForErasing = new List<Relation>();

            foreach (var node in checkedListBox1.CheckedItems)
            {
                string nodeName = Convert.ToString(node);
                int nodeIndex = Int32.Parse(nodeName.Substring(1));

                // the selected node is a cause - it is in the list of causes
                if (nodeName.StartsWith("C"))
                {
                    Node cause = causes.Find(c => c.Index == nodeIndex);
                    nodesForErasing.Add(cause);
                    // finding all relations which contain the node-to-be-erased
                    foreach (var relation in relations)
                    {
                        if (relation.Causes.Find(u => u.Index == cause.Index) != null)
                            relationsForErasing.Add(relation);
                    }
                }

                // the selected node is an effect - it is in the list of effects
                else if (nodeName.StartsWith("E"))
                {
                    Node effect = effects.Find(n => n.Index == nodeIndex);
                    nodesForErasing.Add(effect);
                    // finding all relations which contain the node-to-be-erased
                    foreach (var relation in relations)
                    {
                        if (relation.Effects.Find(p => p.Index == effect.Index) != null)
                            relationsForErasing.Add(relation);
                    }
                }

                // the selected node is an intermediate - it is in the list of intermediates
                else
                {
                    Node intermediate = intermediates.Find(n => n.Index == nodeIndex);
                    nodesForErasing.Add(intermediate);
                    // finding all relations which contain the node-to-be-erased
                    foreach (var relation in relations)
                    {
                        if (relation.Causes.Find(p => p.Index == intermediate.Index) != null)
                            relationsForErasing.Add(relation);

                        else if (relation.Effects.Find(p => p.Index == intermediate.Index) != null)
                            relationsForErasing.Add(relation);
                    }
                }

            }

            // erasing all the selected relations from the list
            relations = relations.Except(relationsForErasing).ToList();

            // erasing all the selected nodes from the list
            causes = causes.Except(nodesForErasing).ToList();
            effects = effects.Except(nodesForErasing).ToList();
            intermediates = intermediates.Except(nodesForErasing).ToList();

            // redrawing the panel
            Refresh();
            Redraw();

            // refreshing all lists
            RefreshNodeList();
            RefreshRelationList();
        }

        /// <summary>
        /// Erasing relations selected in the listBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button15_Click(object sender, EventArgs e)
        {
            // if no relations are selected, deleting is not allowed
            if (checkedListBox2.CheckedItems.Count < 1)
            {
                toolStripStatusLabel1.Text = "You need to select at least one relation!";
                return;
            }

            toolStripStatusLabel1.Text = "";

            // finding all relations to be erased
            List<Relation> relationsForErasing = new List<Relation>();

            foreach (var relationString in checkedListBox2.CheckedItems)
            {
                string description = Convert.ToString(relationString);
                string[] parts = description.Split(" - ");
                string type = parts[0];
                string[] causesString = parts[1].Split(", ");
                string[] effectsString = parts[2].Split(", ");
                List<Node> nodesC = new List<Node>();
                List<Node> nodesE = new List<Node>();

                // finding all relation causes
                foreach (var cause in causesString)
                {
                    int nodeIndex = Int32.Parse(cause.Substring(1));
                    Node node = causes.Find(n => n.Index == nodeIndex);
                    if (cause.StartsWith("I"))
                        node = intermediates.Find(n => n.Index == nodeIndex);
                    nodesC.Add(node);
                }

                // finding all relation effects
                foreach (var effect in effectsString)
                {
                    int nodeIndex = Int32.Parse(effect.Substring(1));
                    Node node = effects.Find(n => n.Index == nodeIndex);
                    if (effect.StartsWith("I"))
                        node = intermediates.Find(n => n.Index == nodeIndex);

                    nodesE.Add(node);
                }

                // finding the relation which contains all specified relation parts
                Relation relation = relations.Find(r => r.Type == type
                                    && nodesC.All(cn => r.Causes.Find(n => n.Index == cn.Index) != null
                                    && nodesE.All(en => r.Effects.Find(n => n.Index == en.Index) != null)));
                relationsForErasing.Add(relation);
            }

            // erasing all selected relations
            relations = relations.Except(relationsForErasing).ToList();

            // redrawing the panel
            Refresh();
            Redraw();

            // refreshing all lists
            RefreshRelationList();
        }

        /// <summary>
        /// Erasing constraints selected in the listBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button16_Click(object sender, EventArgs e)
        {
            // if no relations are selected, deleting is not allowed
            if (checkedListBox3.CheckedItems.Count < 1)
            {
                toolStripStatusLabel1.Text = "You need to select at least one constraint!";
                return;
            }

            toolStripStatusLabel1.Text = "";

            // finding all constraints to be erased
            List<Relation> constraintsForErasing = new List<Relation>();

            foreach (var relationString in checkedListBox3.CheckedItems)
            {
                string description = Convert.ToString(relationString);
                string[] parts = description.Split(" - ");
                string type = parts[0];

                // constraints do not contain both causes and effects
                string[] causesString = new string[1];
                if (parts[1].Length > 0)
                    causesString = parts[1].Split(", ");

                string[] effectsString = new string[1];
                if (parts.Length > 1 && parts[2].Length > 0)
                    effectsString = parts[2].Split(", ");

                List<Node> nodesC = new List<Node>();
                List<Node> nodesE = new List<Node>();

                // finding all constraint causes (if any)
                if (parts[1].Length > 0)
                    foreach (var cause in causesString)
                    {
                        int nodeIndex = Int32.Parse(cause.Substring(1));
                        Node node = causes.Find(n => n.Index == nodeIndex);
                        if (cause.StartsWith("I"))
                            node = intermediates.Find(n => n.Index == nodeIndex);
                        nodesC.Add(node);
                    }

                // finding all constraint effects (if any)
                if (parts[2].Length > 0)
                    foreach (var effect in effectsString)
                    {
                        int nodeIndex = Int32.Parse(effect.Substring(1));
                        Node node = effects.Find(n => n.Index == nodeIndex);
                        if (effect.StartsWith("I"))
                            node = intermediates.Find(n => n.Index == nodeIndex);
                        nodesE.Add(node);
                    }

                // finding the relation which contains all specified relation parts
                Relation relation = relations.Find(r => r.Type == type
                                    && nodesC.All(nc => r.Causes.Find(n => n.Index == nc.Index) != null
                                    && nodesE.All(ne => r.Effects.Find(n => n.Index == ne.Index) != null)));
                constraintsForErasing.Add(relation);
            }

            // erasing all selected relations
            relations = relations.Except(constraintsForErasing).ToList();

            // redrawing the panel
            Refresh();
            Redraw();

            // refreshing all lists
            RefreshRelationList();
        }

        /// <summary>
        /// Resetting the entire panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button20_Click(object sender, EventArgs e)
        {
            causes.Clear();
            effects.Clear();
            intermediates.Clear();
            relations.Clear();

            RefreshNodeList();
            RefreshRelationList();

            Refresh();
        }

        #endregion

        #region Import/Export

        /// <summary>
        /// Importing an existing graph
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                // opening the existing file
                using (var fbd = new OpenFileDialog())
                {
                    DialogResult result = fbd.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.FileName))
                    {
                        string IMPORT = File.ReadAllText(fbd.FileName);
                        string[] rows = IMPORT.Split('\n');

                        // refreshing the existing CEG objects
                        causes.Clear();
                        effects.Clear();
                        intermediates.Clear();
                        relations.Clear();

                        for (int i = 0; i < rows.Length; i++)
                        {
                            // importing all causes
                            if (rows[i] == "CAUSES:")
                            {
                                i++;
                                while (rows[i] != "EFFECTS:")
                                {
                                    string[] attributes = rows[i].Split(',');
                                    causes.Add(new Node(Int32.Parse(attributes[2]), Int32.Parse(attributes[3]), Int32.Parse(attributes[1]), attributes[0]));
                                    i++;
                                }
                                i--;
                            }

                            // importing all effects
                            else if (rows[i] == "EFFECTS:")
                            {
                                i++;
                                while (rows[i] != "INTERMEDIATES:")
                                {
                                    string[] attributes = rows[i].Split(',');
                                    effects.Add(new Node(Int32.Parse(attributes[2]), Int32.Parse(attributes[3]), Int32.Parse(attributes[1]), attributes[0]));
                                    i++;
                                }
                                i--;
                            }

                            // importing all effects
                            else if (rows[i] == "INTERMEDIATES:")
                            {
                                i++;
                                while (rows[i] != "RELATION:")
                                {
                                    string[] attributes = rows[i].Split(',');
                                    intermediates.Add(new Node(Int32.Parse(attributes[2]), Int32.Parse(attributes[3]), Int32.Parse(attributes[1]), attributes[0]));
                                    i++;
                                }
                                i--;
                            }

                            // importing all relations
                            else if (rows[i] == "RELATION:")
                            {
                                i++;
                                Relation relation = new Relation(rows[i]);
                                i++;
                                while (i < rows.Length && rows[i] != "RELATION:")
                                {
                                    if (rows[i] == "CAUSE:")
                                    {
                                        i++;
                                        string[] attributes = rows[i].Split(',');
                                        var node = causes.Find(n => n.X == Int32.Parse(attributes[2]) && n.Y == Int32.Parse(attributes[3]) && n.Index == Int32.Parse(attributes[1]) && n.Type == attributes[0]);
                                        if (attributes[0] == "I")
                                        node = intermediates.Find(n => n.X == Int32.Parse(attributes[2]) && n.Y == Int32.Parse(attributes[3]) && n.Index == Int32.Parse(attributes[1]) && n.Type == attributes[0]);
                                        relation.Causes.Add(node);
                                    }
                                    else if (rows[i] == "EFFECT:")
                                    {
                                        i++;
                                        string[] attributes = rows[i].Split(',');
                                        var node = effects.Find(n => n.X == Int32.Parse(attributes[2]) && n.Y == Int32.Parse(attributes[3]) && n.Index == Int32.Parse(attributes[1]) && n.Type == attributes[0]);
                                        if (attributes[0] == "I")
                                            node = intermediates.Find(n => n.X == Int32.Parse(attributes[2]) && n.Y == Int32.Parse(attributes[3]) && n.Index == Int32.Parse(attributes[1]) && n.Type == attributes[0]);
                                        relation.Effects.Add(node);
                                    }
                                    i++;
                                }
                                i--;
                                relations.Add(relation);
                            }
                        }

                        // refreshing everything
                        Refresh();
                        Redraw();
                        RefreshNodeList();
                        RefreshRelationList();
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("The import could not be completed successfully. Error message: " + exception.Message, "Error information", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Export the current graph to a file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            try
            {
                string EXPORT = "";

                // exporting all causes
                EXPORT += "CAUSES:\n";
                foreach (var cause in causes)
                    EXPORT += cause.Type + "," + cause.Index + "," + cause.X + "," + cause.Y + "\n";

                // exporting all effects
                EXPORT += "EFFECTS:\n";
                foreach (var effect in effects)
                    EXPORT += effect.Type + "," + effect.Index + "," + effect.X + "," + effect.Y + "\n";

                // exporting all effects
                EXPORT += "INTERMEDIATES:\n";
                foreach (var intermediate in intermediates)
                    EXPORT += intermediate.Type + "," + intermediate.Index + "," + intermediate.X + "," + intermediate.Y + "\n";


                // exporting all relations
                foreach (var relation in relations)
                {
                    EXPORT += "RELATION:\n";
                    EXPORT += relation.Type + "\n";
                    foreach (var cause in relation.Causes)
                    {
                        EXPORT += "CAUSE:\n";
                        EXPORT += cause.Type + "," + cause.Index + "," + cause.X + "," + cause.Y + "\n";
                    }
                    foreach (var effect in relation.Effects)
                    {
                        EXPORT += "EFFECT:\n";
                        EXPORT += effect.Type + "," + effect.Index + "," + effect.X + "," + effect.Y + "\n";
                    }
                }

                // creating a new file
                using (var fbd = new FolderBrowserDialog())
                {
                    DialogResult result = fbd.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        File.WriteAllText(fbd.SelectedPath + "\\exportedData.txt", EXPORT);
                        MessageBox.Show(this, "Export succeffully completed. File path: " + fbd.SelectedPath + "\\exportedData.txt!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("The export could not be completed successfully. Error message: " + exception.Message, "Error information", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
