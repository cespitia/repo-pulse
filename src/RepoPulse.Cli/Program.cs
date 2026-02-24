using RepoPulse.Cli.Config;
using RepoPulse.Cli.Data;

try
{
    var config = AppConfig.Load();
    var cs = AppConfig.GetConnectionString(config);

    var db = new Db(cs);
    await db.EnsureReadyAsync();

    Console.WriteLine("RepoPulse: database OK (created if missing), schema ensured.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("RepoPulse failed:");
    Console.Error.WriteLine(ex.Message);
    return 1;
}