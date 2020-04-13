using System.Collections.Generic;
using System.Windows.Media;

namespace GitOut.Features.Git.Log
{
    public class GitTreeEvent
    {
        private static readonly List<Color> colors;
        private readonly List<GitTreeNode> nodes;
        private int colorIndex;

        static GitTreeEvent() => colors = new List<Color>
            {
                Color.FromArgb(255, 255, 255, 0),
                Color.FromArgb(255, 255, 200, 100),
                Color.FromArgb(255, 0, 255, 255),
                Color.FromArgb(255, 100, 255, 100),
                Color.FromArgb(255, 255, 255, 255),
                Color.FromArgb(255, 200, 200, 200),
                Color.FromArgb(255, 100, 100, 100),
                Color.FromArgb(255, 50, 50, 50),
            };

        public GitTreeEvent(GitHistoryEvent historyEvent, int colorIndex)
        {
            this.colorIndex = colorIndex;
            Event = historyEvent;
            CommitIndex = -1;
            nodes = new List<GitTreeNode>();
        }

        public int CommitIndex { get; set; }

        public GitHistoryEvent Event { get; }
        public IReadOnlyCollection<GitTreeNode> Nodes => nodes;

        public int ColorIndex => colorIndex;

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
                            var node = new GitTreeNode(null, new Line(from, to++), NextColor(), true);
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
                var node = new GitTreeNode(null, new Line(from, to), NextColor(), true);
                nodes.Add(node);
                bottomLeafs.Add(new TreeBuildingLeaf(Event.Parent, node));
                if (Event.MergedParent != null)
                {
                    var mergedNode = new GitTreeNode(null, new Line(from, to++), NextColor(), true);
                    nodes.Add(mergedNode);
                    bottomLeafs.Add(new TreeBuildingLeaf(Event.MergedParent, mergedNode));
                }
            }
            return bottomLeafs;
        }

        private Color NextColor() => colors[colorIndex++ % (colors.Count - 1)];

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
                        --colorIndex;
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
    }
}
