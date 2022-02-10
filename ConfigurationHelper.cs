public static class ConfigurationHelper
{
    public static string GetByName(string configKeyName)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        IConfigurationSection section = config.GetSection(configKeyName);
        return section.Value;
    }

    public static List<String> GetWallets(string walletsKeyName)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        return config.GetSection(walletsKeyName).Get<List<string>>();
    }
}

