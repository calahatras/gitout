using System.Collections.Generic;
using GitOut.Features.Git.Log;

namespace GitOut.Features.Git.CherryPick;

public record CherryPickPrepareOptions(IEnumerable<GitTreeEvent> Entries);
