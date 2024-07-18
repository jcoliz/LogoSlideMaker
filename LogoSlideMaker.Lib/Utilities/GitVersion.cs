using LibGit2Sharp;

namespace LogoSlideMaker.Utilities;

public static class GitVersion
{
    /// <summary>
    /// Retrieve the git version of the repository which includes <paramref name="path"/>
    /// </summary>
    /// <remarks>
    /// Possilbe results:
    /// * "1.2.3": When the latest commit is a tag
    /// * "1.2.3-10-g0123fff": When the latest commit is (e.g.) 10 commits ahead of latest tag
    /// * "10-g0123fff": When there are no tags, and the latest commit is (e.g.) 10 commits deep
    /// * "empty": When there are no commits
    /// * above plus "-modified": When files have been modified in the repo but not committed
    /// null: When the <paramref name="path"/> is not contained in any repo
    /// </remarks>
    /// <param name="path">Directory path to look in</param>
    /// <returns></returns>
    public static string? GetForDirectory(string path)
    {
        var root = GetRepositoryRootForDirectory(path);

        if (root is null)
        {
            return null;
        }

        using var repo = new Repository(root);

        // Get the head commit
        var commit = repo.Head?.Tip;

        // Get the closest tag and distance
        (var tag, var distance) = FindMostRecentTagAt(repo, commit);

        // Check if is modified
        var modified = repo.RetrieveStatus().IsDirty;

        // Compose into string
        var result = ComposeIntoVersionString(commit, tag, distance, modified);

        return result;
    }

    /// <summary>
    /// Find the root of the reposiutory, which contains ".git" directory
    /// </summary>
    /// <param name="path">Where to start</param>
    /// <returns>REpository root directory</returns>
    private static string? GetRepositoryRootForDirectory(string path)
    {
        var dir = path;

        while (dir is not null && !Directory.Exists(Path.Combine(dir, ".git")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return dir;
    }

    private static (Tag?,int) FindMostRecentTagAt(Repository repo, Commit? commit)
    {
        Tag? result = null;
        int distance = 0;

        if (commit is not null)
        {
            var tags = repo.Tags.Where(x => x.Target is Commit).ToList();
            List<Commit> current = [commit];
            while (current.Count > 0 && result is null)
            {
                // If there is a tag which matches one of the current commits, use it
                result = tags.FirstOrDefault(x => current.Contains(x.Target));

                // If not, move up the tree one level
                if (result is null)
                {
                    current = current.SelectMany(x => x.Parents).ToList();
                    ++distance;
                }
            }
        }

        return (result, distance);
    }

    private static string ComposeIntoVersionString(Commit? commit, Tag? tag, int distance, bool modified)
    {
        var components = new List<string>();

        if (tag is not null)
        {
            components.Add(tag.FriendlyName);
        }

        if (commit is null)
        {
            components.Add("empty");
        }

        if (distance > 0)
        {
            components.Add($"{distance}");

            if (commit is not null)
            {
                components.Add($"g{commit.Sha[0..7]}");
            }
        }

        if (modified)
        {
            components.Add($"modified");
        }

        return string.Join('-', components);
    }
}
