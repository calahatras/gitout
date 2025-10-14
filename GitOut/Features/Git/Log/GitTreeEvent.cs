using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace GitOut.Features.Git.Log
{
    public class GitTreeEvent : INotifyPropertyChanged
    {
        private static readonly List<AvailableColor> colors = new()
        {
            new AvailableColor(Color.FromRgb(255, 255, 0)),
            new AvailableColor(Color.FromRgb(255, 200, 100)),
            new AvailableColor(Color.FromRgb(0, 255, 255)),
            new AvailableColor(Color.FromRgb(100, 255, 100)),
            new AvailableColor(Color.FromRgb(255, 255, 255)),
            new AvailableColor(Color.FromRgb(200, 200, 200)),
            new AvailableColor(Color.FromRgb(100, 100, 100)),
            new AvailableColor(Color.FromRgb(50, 50, 50)),
        };

        private readonly List<GitTreeNode> nodes = new();
        private int commitIndex = -1;
        private int colorIndex;
        private bool isSelected;

        public GitTreeEvent(GitHistoryEvent historyEvent) => Event = historyEvent;

        public event PropertyChangedEventHandler? PropertyChanged;

        public GitHistoryEvent Event { get; }
        public SolidColorBrush CommitBrush
        {
            get
            {
                var commitBrush = new SolidColorBrush(colors[colorIndex % colors.Count].Color);
                commitBrush.Freeze();
                return commitBrush;
            }
        }

        public IReadOnlyCollection<GitTreeNode> Nodes => nodes;

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public int StashIndex => Event is GitStash stash ? stash.StashIndex : -1;

        public IEnumerable<TreeBuildingLeaf> Process(IEnumerable<TreeBuildingLeaf> leafs) =>
            ProcessBottom(ProcessTop(leafs));

        public static void ResetColors() => colors.ForEach(c => c.Available = true);

        private LineType EventLineType() =>
            Event switch
            {
                GitStash _ => LineType.Dashed,
                GitHistoryEvent _ => LineType.Solid,
                _ => LineType.None,
            };

        private IEnumerable<TreeBuildingLeaf> ProcessBottom(IEnumerable<TreeBuildingLeaf> leafs)
        {
            int from = 0;
            int to = 0;
            var bottomLeafs = new List<TreeBuildingLeaf>();
            bool processedCommit = false;
            foreach (TreeBuildingLeaf leaf in leafs)
            {
                if (from == commitIndex)
                {
                    processedCommit = true;
                    colorIndex = colors.FindIndex(ac => ac.Color == leaf.Current.Color);
                    if (Event.Parent is not null)
                    {
                        leaf.Current.AttachBottom(new Line(from, to++), EventLineType());
                        bottomLeafs.Add(
                            TreeBuildingLeaf.WithParent(Event.Parent, leaf.Current, EventLineType())
                        );
                        if (Event.MergedParent is not null)
                        {
                            var node = GitTreeNode.WithBottomLine(
                                new Line(from, to++),
                                GetNextAvailableColor(),
                                true,
                                LineType.Solid
                            );
                            nodes.Add(node);
                            bottomLeafs.Add(
                                TreeBuildingLeaf.WithParent(
                                    Event.MergedParent,
                                    node,
                                    EventLineType()
                                )
                            );
                        }
                    }
                    ++from;
                }
                else
                {
                    leaf.Current.AttachBottom(new Line(from++, to++), leaf.LineType);
                    bottomLeafs.Add(
                        TreeBuildingLeaf.WithParent(leaf.LookingFor!, leaf.Current, leaf.LineType)
                    );
                }
            }
            if (!processedCommit)
            {
                Color color = GetNextAvailableColor();
                var node = GitTreeNode.WithBottomLine(
                    new Line(from, to++),
                    color,
                    true,
                    EventLineType()
                );
                colorIndex = colors.FindIndex(ac => ac.Color == color);

                nodes.Add(node);
                if (Event.Parent is not null)
                {
                    bottomLeafs.Add(
                        TreeBuildingLeaf.WithParent(Event.Parent, node, EventLineType())
                    );
                    if (Event.MergedParent is not null)
                    {
                        var mergedNode = GitTreeNode.WithBottomLine(
                            new Line(from, to++),
                            GetNextAvailableColor(),
                            true,
                            EventLineType()
                        );
                        nodes.Add(mergedNode);
                        bottomLeafs.Add(
                            TreeBuildingLeaf.WithParent(
                                Event.MergedParent,
                                mergedNode,
                                EventLineType()
                            )
                        );
                    }
                }
            }
            return bottomLeafs;
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
                    if (commitIndex == -1)
                    {
                        commitIndex = to++;
                        node = GitTreeNode.WithTopLine(
                            new Line(from++, commitIndex),
                            leaf.Current.Color,
                            true,
                            leaf.LineType
                        );

                        // add node to topleafs, so that it's processed in ProcessBottom
                        // done only for the first branch that finds the commit, the other would be duplicates
                        // the LookingFor is unused later - it is the parents of the Event that will be used as LookingFor
                        topLeafs.Add(TreeBuildingLeaf.WithoutParent(node));
                    }
                    else
                    {
                        node = GitTreeNode.WithTopLine(
                            new Line(from++, commitIndex),
                            leaf.Current.Color,
                            true,
                            leaf.LineType
                        );
                        SetColorAvailable(leaf.Current.Color);
                    }
                    nodes.Add(node);
                }
                else
                {
                    var newNode = GitTreeNode.WithTopLine(
                        new Line(from++, to++),
                        leaf.Current.Color,
                        false,
                        leaf.LineType
                    );
                    nodes.Add(newNode);
                    if (leaf.LookingFor is null)
                    {
                        throw new InvalidOperationException(
                            "leaf should have something to look for"
                        );
                    }

                    topLeafs.Add(
                        TreeBuildingLeaf.WithParent(leaf.LookingFor, newNode, leaf.LineType)
                    );
                }
            }
            if (commitIndex == -1)
            {
                commitIndex = to;
            }

            return topLeafs;
        }

        private static Color GetNextAvailableColor()
        {
            AvailableColor? nextColor = colors.FirstOrDefault(ac => ac.Available);
            if (nextColor is null)
            {
                colors.ForEach(ac => ac.Available = true);
                nextColor = colors[0];
            }
            nextColor.Available = false;
            return nextColor.Color;
        }

        private static void SetColorAvailable(Color color)
        {
            AvailableColor? availableColor = colors.FirstOrDefault(ac => ac.Color == color);
            if (availableColor is not null)
            {
                availableColor.Available = true;
            }
        }

        private void SetProperty<T>(
            ref T prop,
            T value,
            [CallerMemberName] string? propertyName = null
        )
        {
            if (!ReferenceEquals(prop, value))
            {
                prop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private class AvailableColor
        {
            public AvailableColor(Color color) => Color = color;

            public bool Available { get; set; } = true;
            public Color Color { get; }
        }
    }
}
