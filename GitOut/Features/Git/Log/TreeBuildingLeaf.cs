namespace GitOut.Features.Git.Log
{
    public class TreeBuildingLeaf
    {
        private TreeBuildingLeaf(GitHistoryEvent? lookingFor, GitTreeNode currentNode, LineType lineType)
        {
            LookingFor = lookingFor;
            Current = currentNode;
            LineType = lineType;
        }

        public GitHistoryEvent? LookingFor { get; }
        public GitTreeNode Current { get; }
        public LineType LineType { get; }
        public static TreeBuildingLeaf WithoutParent(GitTreeNode node) => new(null, node, LineType.Solid);
        public static TreeBuildingLeaf WithParent(GitHistoryEvent parent, GitTreeNode node, LineType lineType) => new(parent, node, lineType);
    }
}
