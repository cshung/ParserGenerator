namespace Andrew.ParserGenerator
{
    using System.Collections.Generic;
    using System.Text;

    public class Dfa
    {
        // TODO: Index the edges by node
        public DfaNode StartNode { get; set; }
        public List<DfaNode> Nodes { get; set; }
        public List<DfaEdge> Edges { get; set; }

        public string ToGraphViz()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"digraph finite_state_machine {
    rankdir=LR;
    size=""8,5""

    node[shape = doublecircle]; ");
            foreach (var node in Nodes)
            {
                if (node.IsFinal)
                {
                    sb.Append("S");
                    sb.Append(node.Id);
                    sb.Append(" ");
                }
            }
            sb.AppendLine(";");
            sb.AppendLine("    node[shape = circle];");
            foreach (var edge in this.Edges)
            {
                sb.Append("    ");
                sb.Append("S");
                sb.Append(edge.SourceNode.Id);
                sb.Append(" -> ");
                sb.Append("S");
                sb.Append(edge.TargetNode.Id);
                sb.Append(@" [ label = """);
                sb.Append(edge.Symbol);
                sb.AppendLine(@""" ];");
            }
            sb.Append("}");

            return sb.ToString();
        }
    }
}