namespace RepoPulse.Cli.Data;

public static class Schema
{
    // Normalized tables:
    // Repos (1) -> Commits (many)
    // Authors (1) -> Commits (many)
    public const string Sql = @"
IF OBJECT_ID('dbo.Repos', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.Repos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Owner NVARCHAR(100) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    FullName AS (Owner + '/' + Name) PERSISTED,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Repos_Owner_Name UNIQUE (Owner, Name)
  );
END;

IF OBJECT_ID('dbo.Authors', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.Authors (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Login NVARCHAR(200) NOT NULL,
    DisplayName NVARCHAR(200) NULL,
    Email NVARCHAR(320) NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Authors_Login UNIQUE (Login)
  );
END;

IF OBJECT_ID('dbo.Commits', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.Commits (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    RepoId INT NOT NULL,
    AuthorId INT NULL,
    Sha NVARCHAR(64) NOT NULL,
    Message NVARCHAR(400) NOT NULL,
    CommitDateUtc DATETIME2 NOT NULL,
    HtmlUrl NVARCHAR(500) NULL,
    InsertedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Commits_Repo_Sha UNIQUE (RepoId, Sha),
    CONSTRAINT FK_Commits_RepoId FOREIGN KEY (RepoId) REFERENCES dbo.Repos(Id),
    CONSTRAINT FK_Commits_AuthorId FOREIGN KEY (AuthorId) REFERENCES dbo.Authors(Id)
  );

  CREATE INDEX IX_Commits_RepoId_DateUtc ON dbo.Commits(RepoId, CommitDateUtc DESC);
END;
";
}