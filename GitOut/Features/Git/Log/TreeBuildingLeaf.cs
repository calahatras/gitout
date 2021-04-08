namespace GitOut.Features.Git.Log
{
    public class TreeBuildingLeaf
    {
        private TreeBuildingLeaf(GitHistoryEvent? lookingFor, GitTreeNode currentNode)
        {
            LookingFor = lookingFor;
            Current = currentNode;
        }

        public GitHistoryEvent? LookingFor { get; }
        public GitTreeNode Current { get; }
        public static TreeBuildingLeaf WithoutParent(GitTreeNode node) => new(null, node);
        public static TreeBuildingLeaf WithParent(GitHistoryEvent parent, GitTreeNode node) => new(parent, node);
    }
}
