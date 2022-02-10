using Cronos;
using Blockfrost.Api.Extensions;
using Blockfrost.Api;
using System.Text.Json;
class WalletNotifierService
{
    private readonly ILogger<WalletNotifierService> _logger;

    public WalletNotifierService(ILogger<WalletNotifierService> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DoWork();
            await WaitForNextSchedule(ConfigurationHelper.GetByName("cron"));
            _logger.LogInformation("ExecuteAsync {time}", DateTimeOffset.Now);

        }
    }

    private async void DoWork()
    {
        var wallets = ConfigurationHelper.GetWallets("wallets");
        var apiKey = ConfigurationHelper.GetByName("blockfrostio:APIKey");
        var network = ConfigurationHelper.GetByName("blockfrostio:network");
        var provider = new ServiceCollection().AddBlockfrost(network, apiKey).BuildServiceProvider();
        var accSrv = provider.GetRequiredService<IAccountService>();
        string docPath = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(docPath);
        foreach (string wallet in wallets)
        {
            var acc = await accSrv.GetAccountsAsync(wallet);
            string? accRet = await GetBFIAccountMethod(docPath, dir, wallet, acc, "account");
            var rewards = await accSrv.RewardsAsync(wallet, 100, 1, ESortOrder.Desc);
            string? rewardsRet = await GetBFIAccountMethod(docPath, dir, wallet, rewards, "rewards");
            var history = await accSrv.HistoryAsync(wallet, 100, 1, ESortOrder.Desc);
            string? historyRet = await GetBFIAccountMethod(docPath, dir, wallet, history, "history");
            var assets = await accSrv.AssetsAllAsync(wallet, 100, 1, ESortOrder.Desc);
            string? assetsRet = await GetBFIAccountMethod(docPath, dir, wallet, assets, "assets");
            var delegations = await accSrv.AssetsAllAsync(wallet, 100, 1, ESortOrder.Desc);
            string? delegationsRet = await GetBFIAccountMethod(docPath, dir, wallet, delegations, "delegations");
            if (!String.IsNullOrEmpty(accRet) || !String.IsNullOrEmpty(rewardsRet) || !String.IsNullOrEmpty(historyRet) || !String.IsNullOrEmpty(assetsRet) || !String.IsNullOrEmpty(delegationsRet))
            {
                string subj = accRet + "<br />" + rewardsRet + "<br />" + historyRet + "<br />" + assetsRet + "<br />" + delegationsRet + "<br />wallet: " + "https://pool.pm/" + wallet;
                Comms.SendEmail(subj, ConfigurationHelper.GetByName("MailSettings:Subject") + ": " + wallet);
            }
        }

        _logger.LogInformation("DoWork {time}", DateTimeOffset.Now);
    }

    private async Task<string?> GetBFIAccountMethod(string docPath, DirectoryInfo dir, string wallet, object acc, string fileIdentifier)
    {
        string? ret = null;
        string accJson = JsonSerializer.Serialize(acc);
        var fileInfos = dir.GetFiles(wallet + "*" + fileIdentifier + "*.json").OrderByDescending(fileInfo => fileInfo.CreationTime).FirstOrDefault();
        try
        {
            string? n = fileInfos!.FullName;
            var t = "";
            using (var sr = new StreamReader(n))
            {
                t = await sr.ReadToEndAsync();
            }

            var rez = accJson == t;
            if (rez)
            {
                // _logger.LogInformation("wallet " + wallet + ": " + fileIdentifier + ": nochange");
            }
            else
            {
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, wallet + "-" + fileIdentifier +
                    "-" + Guid.NewGuid().ToString() + ".json")))
                {
                    await outputFile.WriteAsync(accJson);
                }

                ret = fileIdentifier + " change" + "<br />";
                if (fileIdentifier == "account")
                {
                    Account acc2 = (Account)acc;
                    ret = FormatAccount(ret, acc2, "current");
                    Account? prev = JsonSerializer.Deserialize<Account>(t);
                    ret = FormatAccount(ret, prev!, "prev");
                }
                else if (fileIdentifier == "assets")
                {
                    ICollection<StakeAddressAddressesAssetsResponse>? prev = JsonSerializer.Deserialize<ICollection<StakeAddressAddressesAssetsResponse>>(t);
                    ret += "<table><tr><th>quantity</th><th>policy id</th></tr>";
                    foreach (StakeAddressAddressesAssetsResponse asset in prev!)
                    {                        
                        ret += "<tr><td>" + asset.Quantity + "</td><td><a href='https://cardanoscan.io/token/" + asset.Unit + "'>" + asset.Unit + "</a></td></tr>";
                    }
                    ret += "</table>";

                    ICollection<StakeAddressAddressesAssetsResponse> acc2 = (ICollection<StakeAddressAddressesAssetsResponse>)acc;
                }

                _logger.LogInformation("wallet " + wallet + ": " + fileIdentifier + ": change");
            }
        }
        catch (NullReferenceException)
        {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, wallet + "-" + fileIdentifier +
                "-" + Guid.NewGuid().ToString() + ".json")))
            {
                await outputFile.WriteAsync(accJson);
            }

            ret = fileIdentifier + " new";
            _logger.LogWarning("wallet " + wallet + ": " + fileIdentifier + ": new");
        }

        return ret;
    }

    private static string FormatAccount(string? ret, Account acc2, string accountType)
    {
        ret += "<br/><b>" + accountType + " acccount</b>:<br/>";
        ret += "active: " + acc2.Active + "<br/>";
        ret += "active epoch: " + acc2.Active_epoch + "<br/>";
        ret += "controlled amount: " + Double.Parse(acc2.Controlled_amount) / 1000000 + "₳<br/>";
        ret += "pool id: <a href='https://pool.pm/" + acc2.Pool_id + "'>" + acc2.Pool_id + "</a><br/>";
        ret += "reserves sum: " + Double.Parse(acc2.Reserves_sum) / 1000000 + "₳<br/>";
        ret += "rewards sum :" + Double.Parse(acc2.Rewards_sum) / 1000000 + "₳<br/>";
        ret += "stake address: " + acc2.Stake_address + "<br/>";
        ret += "treasury sum: " + Double.Parse(acc2.Treasury_sum) / 1000000 + "<br/>";
        ret += "withdrawable amount: " + Double.Parse(acc2.Withdrawable_amount) / 1000000 + "₳<br/>";
        ret += "withdrawals sum: " + Double.Parse(acc2.Withdrawals_sum) / 1000000 + "₳<br/>";
        return ret;
    }

    private async Task WaitForNextSchedule(string cronExpression)
    {
        var parsedExp = CronExpression.Parse(cronExpression);
        var currentUtcTime = DateTimeOffset.UtcNow.UtcDateTime;
        var occurenceTime = parsedExp.GetNextOccurrence(currentUtcTime);
        var delay = occurenceTime.GetValueOrDefault() - currentUtcTime;
        _logger.LogInformation("The run is delayed for {delay}. Current time: {time}", delay, DateTimeOffset.Now);
        await Task.Delay(delay);
    }
}
