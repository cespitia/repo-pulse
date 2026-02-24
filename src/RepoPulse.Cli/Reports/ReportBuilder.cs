using System.Net;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RepoPulse.Cli.Reports;

public sealed class ReportBuilder
{
    private readonly string _cs;

    public ReportBuilder(string connectionString)
    {
        _cs = connectionString;
    }

    public async Task<string> BuildHtmlAsync(int repoId, string repoFullName, int days, DateTime sinceUtc)
    {
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync();

        var totalCommits = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM dbo.Commits WHERE RepoId=@RepoId AND CommitDateUtc >= @SinceUtc;",
            new { RepoId = repoId, SinceUtc = sinceUtc });

        var topAuthors = (await conn.QueryAsync<TopAuthorRow>(
            @"
SELECT TOP 10
    COALESCE(a.Login, 'unknown') AS Author,
    COUNT(*) AS CommitCount
FROM dbo.Commits c
LEFT JOIN dbo.Authors a ON a.Id = c.AuthorId
WHERE c.RepoId=@RepoId AND c.CommitDateUtc >= @SinceUtc
GROUP BY COALESCE(a.Login, 'unknown')
ORDER BY COUNT(*) DESC;",
            new { RepoId = repoId, SinceUtc = sinceUtc }))
            .ToList();

        var latest = (await conn.QueryAsync<LatestCommitRow>(
            @"
SELECT TOP 20
    c.Sha,
    c.CommitDateUtc,
    COALESCE(a.Login, 'unknown') AS Author,
    c.Message,
    c.HtmlUrl
FROM dbo.Commits c
LEFT JOIN dbo.Authors a ON a.Id = c.AuthorId
WHERE c.RepoId=@RepoId AND c.CommitDateUtc >= @SinceUtc
ORDER BY c.CommitDateUtc DESC;",
            new { RepoId = repoId, SinceUtc = sinceUtc }))
            .ToList();

        return RenderHtml(repoFullName, days, sinceUtc, totalCommits, topAuthors, latest);
    }

    private static string RenderHtml(
        string repoFullName,
        int days,
        DateTime sinceUtc,
        int totalCommits,
        List<TopAuthorRow> topAuthors,
        List<LatestCommitRow> latest)
    {
        static string H(string s) => WebUtility.HtmlEncode(s);

        var sb = new StringBuilder();

        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine($"<title>RepoPulse Report - {H(repoFullName)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(@"
:root { --bg:#0b1220; --card:#111b2e; --muted:rgba(255,255,255,.72); --line:rgba(255,255,255,.10); --accent:#512BD4; }
* { box-sizing:border-box; }
body { margin:0; font-family: ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Arial; background: radial-gradient(1200px 700px at 20% 0%, rgba(81,43,212,.30), transparent), var(--bg); color:white; }
.wrap { max-width: 1040px; margin: 32px auto; padding: 0 16px; }
.header { display:flex; flex-wrap:wrap; gap:12px; align-items:flex-end; justify-content:space-between; }
h1 { margin:0; font-size: 28px; letter-spacing:-.02em; }
.sub { margin: 6px 0 0; color: var(--muted); }
.grid { display:grid; grid-template-columns: 1fr; gap: 14px; margin-top: 18px; }
@media (min-width: 920px){ .grid { grid-template-columns: 1.1fr .9fr; } }
.card { background: rgba(17,27,46,.85); border: 1px solid var(--line); border-radius: 16px; padding: 14px; box-shadow: 0 18px 40px rgba(0,0,0,.35); }
.kpis { display:grid; grid-template-columns: repeat(3, 1fr); gap: 10px; }
.kpi { padding: 12px; border-radius: 14px; border: 1px solid var(--line); background: rgba(255,255,255,.03); }
.kpi .label { color: var(--muted); font-size: 12px; }
.kpi .value { font-size: 22px; margin-top: 6px; font-weight: 700; }
.table { width:100%; border-collapse: collapse; }
th, td { text-align:left; padding: 10px 8px; border-bottom: 1px solid var(--line); vertical-align: top; }
th { font-size: 12px; color: var(--muted); font-weight: 600; }
.badge { display:inline-block; padding: 4px 10px; border-radius: 999px; border: 1px solid var(--line); background: rgba(255,255,255,.04); font-size: 12px; color: var(--muted); }
a { color: #c7b9ff; text-decoration: none; }
a:hover { text-decoration: underline; }
.footer { margin: 18px 0 40px; color: var(--muted); font-size: 12px; }
code { font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, ""Liberation Mono"", ""Courier New"", monospace; }
");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"wrap\">");

        sb.AppendLine("<div class=\"header\">");
        sb.AppendLine("<div>");
        sb.AppendLine($"<h1>RepoPulse Report</h1>");
        sb.AppendLine($"<div class=\"sub\"><span class=\"badge\">{H(repoFullName)}</span> · Last {days} days (since {sinceUtc:yyyy-MM-dd})</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"sub\">Generated: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"grid\">");

        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("<h2 style=\"margin:0 0 12px; font-size:16px;\">Summary</h2>");
        sb.AppendLine("<div class=\"kpis\">");
        sb.AppendLine($"<div class=\"kpi\"><div class=\"label\">Total commits</div><div class=\"value\">{totalCommits}</div></div>");
        sb.AppendLine($"<div class=\"kpi\"><div class=\"label\">Unique authors</div><div class=\"value\">{topAuthors.Count}</div></div>");
        sb.AppendLine($"<div class=\"kpi\"><div class=\"label\">Latest commit</div><div class=\"value\">{(latest.Count == 0 ? "-" : latest[0].CommitDateUtc.ToString("yyyy-MM-dd"))}</div></div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("<h2 style=\"margin:0 0 12px; font-size:16px;\">Top authors</h2>");
        sb.AppendLine("<table class=\"table\">");
        sb.AppendLine("<thead><tr><th>Author</th><th>Commits</th></tr></thead><tbody>");
        foreach (var a in topAuthors)
        {
            sb.AppendLine($"<tr><td>{H(a.Author)}</td><td>{a.CommitCount}</td></tr>");
        }
        if (topAuthors.Count == 0)
            sb.AppendLine("<tr><td colspan=\"2\" class=\"sub\">No commits in range.</td></tr>");
        sb.AppendLine("</tbody></table>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>"); // grid

        sb.AppendLine("<div class=\"card\" style=\"margin-top:14px;\">");
        sb.AppendLine("<h2 style=\"margin:0 0 12px; font-size:16px;\">Latest activity</h2>");
        sb.AppendLine("<table class=\"table\">");
        sb.AppendLine("<thead><tr><th>Date (UTC)</th><th>SHA</th><th>Author</th><th>Message</th></tr></thead><tbody>");
        foreach (var c in latest)
        {
            var sha7 = c.Sha.Length >= 7 ? c.Sha[..7] : c.Sha;
            var msg = c.Message.Split('\n')[0];
            var shaCell = string.IsNullOrWhiteSpace(c.HtmlUrl)
                ? $"<code>{H(sha7)}</code>"
                : $"<a href=\"{H(c.HtmlUrl)}\"><code>{H(sha7)}</code></a>";

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{c.CommitDateUtc:yyyy-MM-dd}</td>");
            sb.AppendLine($"<td>{shaCell}</td>");
            sb.AppendLine($"<td>{H(c.Author)}</td>");
            sb.AppendLine($"<td>{H(msg)}</td>");
            sb.AppendLine("</tr>");
        }
        if (latest.Count == 0)
            sb.AppendLine("<tr><td colspan=\"4\" class=\"sub\">No commits in range.</td></tr>");
        sb.AppendLine("</tbody></table>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"footer\">Generated by <strong>RepoPulse</strong> (C# CLI). Data source: GitHub API, stored in SQL Server.</div>");
        sb.AppendLine("</div></body></html>");

        return sb.ToString();
    }

    private sealed record TopAuthorRow(string Author, int CommitCount);

    private sealed record LatestCommitRow(
        string Sha,
        DateTime CommitDateUtc,
        string Author,
        string Message,
        string? HtmlUrl
    );
}