using System;

namespace GitOut.Features.Git.Stage
{
    public class StatusChangeViewModel
    {
        private StatusChangeViewModel(GitStatusChange model, StatusChangeLocation location)
        {
            Model = model;
            Location = location;
            Path = model.Path;
            if (model.Type == GitStatusChangeType.Untracked)
            {
                IconResourceKey = "FilePlus";
            }
            else
            {
                GitModifiedStatusType? status = location == StatusChangeLocation.Workspace
                    ? model.WorkspaceStatus
                    : model.IndexStatus;
                if (status == null)
                {
                    throw new ArgumentNullException(nameof(model), "Got null status for tracked file");
                }
                IconResourceKey = status switch
                {
                    GitModifiedStatusType.Added => "FilePlus",
                    GitModifiedStatusType.Deleted => "FileRemove",
                    GitModifiedStatusType.Modified => "FileEdit",
                    GitModifiedStatusType.Renamed => "FileMove",
                    GitModifiedStatusType.Copied => "FileReplace",
                    _ => "FileHidden"
                };
            }
        }

        public string Path { get; }
        public string IconResourceKey { get; }

        public GitStatusChange Model { get; }
        public StatusChangeLocation Location { get; }

        public static StatusChangeViewModel AsStaged(GitStatusChange change) => new StatusChangeViewModel(change, StatusChangeLocation.Index);

        public static StatusChangeViewModel AsWorkspace(GitStatusChange change) => new StatusChangeViewModel(change, StatusChangeLocation.Workspace);
    }
}