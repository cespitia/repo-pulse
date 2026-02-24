using RepoPulse.Cli.Cli;
using RepoPulse.Cli.Config;
using RepoPulse.Cli.Data;
using RepoPulse.Cli.GitHub;

// Parse CLI arguments
var cliArgs = ArgsParser.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());

if (cliArgs.Help)
{
    ArgsParser.PrintHelp();
    return 0;
}

try
{
    // Load configuration
    var config = AppConfig.Load();
    var cs = AppConfig.GetConnectionString(config);

    // Ensure database + schema exist
    var db = new Db(cs);
    await db.EnsureReadyAsync();

    // Parse repo owner/name
    var parts = cliArgs.RepoFullName.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 2)
        throw new InvalidOperationException("Repo must be in owner/name form (e.g. dotnet/runtime).");

    var owner = parts[0];
    var repo = parts[1];

    var sinceUtc = DateTime.UtcNow.AddDays(-cliArgs.Days);

    Console.WriteLine($"RepoPulse: {owner}/{repo}");
    Console.WriteLine($"Days: {cliArgs.Days} (since {sinceUtc:yyyy-MM-dd})");
    Console.WriteLine();

    // GitHub API client
    using var http = new HttpClient();
    var gh = new GitHubClient(http);

    // Optional: Personal Access Token for rate limits
    var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    gh.SetToken(token);

    // Fetch commits
    var commits = await gh.GetCommitsAsync(owner, repo, sinceUtc);

    Console.WriteLine($"Commits returned: {commits.Count}");
    Console.WriteLine();

    // Persist to SQL Server
    var store = new RepoStore(cs);

    var repoId = await store.UpsertRepoAsync(owner, repo);

    await store.InsertCommitsAsync(repoId, commits);

    Console.WriteLine($"Commits persisted: {commits.Count}");
    Console.WriteLine();

    // Print sample output for visibility
    foreach (var c in commits.Take(10))
    {
        var who = c.Author?.Login ?? c.Commit.Author.Name;
        var firstLine = c.Commit.Message.Split('\n')[0];

        Console.WriteLine($"{c.Sha[..7]}  {c.Commit.Author.Date:yyyy-MM-dd}  {who}  {firstLine}");
    }

    Console.WriteLine();
    Console.WriteLine("Next step: generate HTML report artifact.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("RepoPulse failed:");
    Console.Error.WriteLine(ex.Message);
    return 1;
}