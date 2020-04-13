namespace GitOut.Features.Git.Log
{
    public class TreeBuildingLeaf
    {
        public TreeBuildingLeaf(GitHistoryEvent? lookingFor, GitTreeNode currentNode)
        {
            LookingFor = lookingFor;
            Current = currentNode;
        }

        public GitHistoryEvent? LookingFor { get; }
        public GitTreeNode Current { get; }
    }
}
