using RepoPulse.Cli.Cli;
using RepoPulse.Cli.Config;
using RepoPulse.Cli.Data;
using RepoPulse.Cli.GitHub;

var cliArgs = ArgsParser.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
if (cliArgs.Help)
{
    ArgsParser.PrintHelp();
    return 0;
}

try
{
    // DB ready (we won't write commits yet, but keep the pipeline consistent)
    var config = AppConfig.Load();
    var cs = AppConfig.GetConnectionString(config);

    var db = new Db(cs);
    await db.EnsureReadyAsync();

    // Parse repo
    var parts = cliArgs.RepoFullName.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 2)
        throw new InvalidOperationException("Repo must be in owner/name form.");

    var owner = parts[0];
    var repo = parts[1];
    var sinceUtc = DateTime.UtcNow.AddDays(-cliArgs.Days);

    // GitHub API
    using var http = new HttpClient();
    var gh = new GitHubClient(http);

    // Optional token for rate limits
    gh.SetToken(Environment.GetEnvironmentVariable("GITHUB_TOKEN"));

    var commits = await gh.GetCommitsAsync(owner, repo, sinceUtc);

    Console.WriteLine($"RepoPulse: {owner}/{repo}");
    Console.WriteLine($"Days: {cliArgs.Days} (since {sinceUtc:yyyy-MM-dd})");
    Console.WriteLine($"Commits returned: {commits.Count}");
    Console.WriteLine();

    foreach (var c in commits.Take(10))
    {
        var who = c.Author?.Login ?? c.Commit.Author.Name;
        Console.WriteLine($"{c.Sha[..7]}  {c.Commit.Author.Date:yyyy-MM-dd}  {who}  {c.Commit.Message.Split('\n')[0]}");
    }

    Console.WriteLine();
    Console.WriteLine("Next: persist commits into SQL + generate report.html.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("RepoPulse failed:");
    Console.Error.WriteLine(ex.Message);
    return 1;
}