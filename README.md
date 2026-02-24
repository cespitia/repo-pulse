# RepoPulse

CLI tool that pulls repository activity from the GitHub API, stores it in SQL Server, and generates an HTML report artifact.

## Tech
- C# / .NET (Console)
- GitHub REST API
- SQL Server
- HTML report output
- Docs: architecture + test plan

## Planned Workflow
CLI → GitHub API + SQL Server → report.html

## MVP Scope
- Inputs: owner/repo and days range (default 7)
- Fetch commits and store in normalized tables
- Generate report.html with totals and latest activity
- Friendly errors for rate limits and missing auth

## Run (later)
```bash
dotnet run --project src/RepoPulse.Cli -- --repo owner/name --days 7