using Dapper;
using Microsoft.Data.SqlClient;
using RepoPulse.Cli.GitHub;

namespace RepoPulse.Cli.Data;

public sealed class RepoStore
{
    private readonly string _cs;

    public RepoStore(string connectionString)
    {
        _cs = connectionString;
    }

    public async Task<int> UpsertRepoAsync(string owner, string name)
    {
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync();

        var sql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.Repos WHERE Owner=@Owner AND Name=@Name)
BEGIN
    INSERT INTO dbo.Repos (Owner, Name) VALUES (@Owner, @Name);
END

SELECT Id FROM dbo.Repos WHERE Owner=@Owner AND Name=@Name;
";

        return await conn.ExecuteScalarAsync<int>(sql, new { Owner = owner, Name = name });
    }

    public async Task<int?> UpsertAuthorAsync(string login, string displayName, string? email)
    {
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync();

        var sql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.Authors WHERE Login=@Login)
BEGIN
    INSERT INTO dbo.Authors (Login, DisplayName, Email)
    VALUES (@Login, @DisplayName, @Email);
END

SELECT Id FROM dbo.Authors WHERE Login=@Login;
";

        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            Login = login,
            DisplayName = displayName,
            Email = email
        });
    }

    public async Task InsertCommitsAsync(int repoId, IReadOnlyList<CommitDto> commits)
    {
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync();

        foreach (var c in commits)
        {
            var login = c.Author?.Login ?? c.Commit.Author.Name;

            int? authorId = null;
            if (!string.IsNullOrWhiteSpace(login))
            {
                authorId = await UpsertAuthorAsync(
                    login,
                    c.Commit.Author.Name,
                    c.Commit.Author.Email
                );
            }

            var sql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.Commits WHERE RepoId=@RepoId AND Sha=@Sha)
BEGIN
    INSERT INTO dbo.Commits (RepoId, AuthorId, Sha, Message, CommitDateUtc, HtmlUrl)
    VALUES (@RepoId, @AuthorId, @Sha, @Message, @CommitDateUtc, @HtmlUrl);
END
";

            await conn.ExecuteAsync(sql, new
            {
                RepoId = repoId,
                AuthorId = authorId,
                Sha = c.Sha,
                Message = c.Commit.Message.Length > 400 ? c.Commit.Message[..400] : c.Commit.Message,
                CommitDateUtc = c.Commit.Author.Date,
                HtmlUrl = c.Html_Url
            });
        }
    }
}