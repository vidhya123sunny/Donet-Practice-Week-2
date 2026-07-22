namespace RetailInventory.Api.Data;

public static class DatabaseSettings
{
    public static string SqliteConnectionString
    {
        get
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var projectRoot = Path.GetFileName(currentDirectory) == "RetailInventory.Api"
                ? currentDirectory
                : Path.Combine(currentDirectory, "RetailInventory.Api");

            if (!Directory.Exists(projectRoot))
            {
                projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            }

            var databasePath = Path.Combine(projectRoot, "retail-inventory.db");
            return $"Data Source={databasePath}";
        }
    }
}
