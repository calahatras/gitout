using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GitOut.Features.Git.Log
{
    public class GitTreeEvent
    {
        private static readonly List<AvailableColor> colors;
        private readonly List<GitTreeNode> nodes;

        static GitTreeEvent() => colors = new List<AvailableColor>
            {
                new AvailableColor(Color.FromArgb(255, 255, 255, 0)),
                new AvailableColor(Color.FromArgb(255, 255, 200, 100)),
                new AvailableColor(Color.FromArgb(255, 0, 255, 255)),
                new AvailableColor(Color.FromArgb(255, 100, 255, 100)),
                new AvailableColor(Color.FromArgb(255, 255, 255, 255)),
                new AvailableColor(Color.FromArgb(255, 200, 200, 200)),
                new AvailableColor(Color.FromArgb(255, 100, 100, 100)),
                new AvailableColor(Color.FromArgb(255, 50, 50, 50))
            };

        public GitTreeEvent(GitHistoryEvent historyEvent)
        {
            Event = historyEvent;
            CommitIndex = -1;
            nodes = new List<GitTreeNode>();
        }

        public int CommitIndex { get; set; }

        public GitHistoryEvent Event { get; }
        public IReadOnlyCollection<GitTreeNode> Nodes => nodes;

        public IEnumerable<TreeBuildingLeaf> Process(IEnumerable<TreeBuildingLeaf> leafs) => ProcessBottom(ProcessTop(leafs));

        private IEnumerable<TreeBuildingLeaf> ProcessBottom(IEnumerable<TreeBuildingLeaf> leafs)
        {
            int from = 0;
            int to = 0;
            var bottomLeafs = new List<TreeBuildingLeaf>();
            bool processedCommit = false;
            foreach (TreeBuildingLeaf leaf in leafs)
            {
                if (from == CommitIndex)
                {
                    processedCommit = true;

                    if (Event.Parent != null)
                    {
                        leaf.Current.Bottom = new Line(from, to++);
                        bottomLeafs.Add(new TreeBuildingLeaf(Event.Parent, leaf.Current));
                        if (Event.MergedParent != null)
                        {
                            var node = new GitTreeNode(null, new Line(from, to++), GetNextAvailableColor(), true);
                            nodes.Add(node);
                            bottomLeafs.Add(new TreeBuildingLeaf(Event.MergedParent, node));
                        }
                    }
                    ++from;
                }
                else
                {
                    leaf.Current.Bottom = new Line(from++, to++);
                    bottomLeafs.Add(new TreeBuildingLeaf(leaf.LookingFor, leaf.Current));
                }
            }
            if (!processedCommit)
            {
                var node = new GitTreeNode(null, new Line(from, to), GetNextAvailableColor(), true);
                nodes.Add(node);
                bottomLeafs.Add(new TreeBuildingLeaf(Event.Parent, node));
                if (Event.MergedParent != null)
                {
                    var mergedNode = new GitTreeNode(null, new Line(from, to++), GetNextAvailableColor(), true);
                    nodes.Add(mergedNode);
                    bottomLeafs.Add(new TreeBuildingLeaf(Event.MergedParent, mergedNode));
                }
            }
            return bottomLeafs;
        }

        private Color GetNextAvailableColor()
        {
            AvailableColor nextColor = colors.FirstOrDefault(ac => ac.Available);
            if (nextColor is null)
            {
                colors.ForEach(ac => ac.Available = true);
                nextColor = colors[0];
            }
            nextColor.Available = false;
            return nextColor.Color;
        }

        private void SetColorAvailable(Color color)
        {
            AvailableColor availableColor = colors.FirstOrDefault(ac => ac.Color == color);
            if (availableColor != null)
            {
                availableColor.Available = true;
            }
        }

        private IEnumerable<TreeBuildingLeaf> ProcessTop(IEnumerable<TreeBuildingLeaf> leafs)
        {
            int from = 0;
            int to = 0;
            var topLeafs = new List<TreeBuildingLeaf>();
            foreach (TreeBuildingLeaf leaf in leafs)
            {
                if (leaf.LookingFor == Event)
                {
                    GitTreeNode node;
                    if (CommitIndex == -1)
                    {
                        CommitIndex = to++;
                        node = new GitTreeNode(new Line(from++, CommitIndex), null, leaf.Current.Color, true);

                        // add node to topleafs, so that it's processed in ProcessBottom
                        // done only for the first branch that finds the commit, the other would be duplicates
                        // the LookingFor is unused later - it is the parents of the Event that will be used as LookingFor
                        topLeafs.Add(new TreeBuildingLeaf(null, node));
                    }
                    else
                    {
                        node = new GitTreeNode(new Line(from++, CommitIndex), null, leaf.Current.Color, true);
                        SetColorAvailable(leaf.Current.Color);
                    }
                    nodes.Add(node);
                }
                else
                {
                    var newNode = new GitTreeNode(new Line(from++, to++), null, leaf.Current.Color, false);
                    nodes.Add(newNode);
                    topLeafs.Add(new TreeBuildingLeaf(leaf.LookingFor, newNode));
                }
            }
            if (CommitIndex == -1)
            {
                CommitIndex = to;
            }

            return topLeafs;
        }
        private class AvailableColor
        {
            public AvailableColor(Color color)
            {
                Available = true;
                Color = color;
            }

            public bool Available { get; set; }
            public Color Color { get; }
        }
    }
}
