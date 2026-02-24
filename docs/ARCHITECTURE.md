
#### `docs/ARCHITECTURE.md`
```md
# Architecture

RepoPulse is a simple data pipeline:

CLI → GitHub API → SQL Server → HTML report artifact

## Components
- RepoPulse.Cli: argument parsing, API calls, persistence, report generation
- SQL Server: normalized tables for repos, commits, and authors
- report.html: generated artifact for sharing during interviews