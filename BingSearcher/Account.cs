using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingSearcher
{
    internal class Account
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool Disabled { get; set; } = false;
        public bool GetDailyPoints { get; set; } = false;
        public int SwitchDelay { get; set; } = 6000;

        public SearchConfig DesktopSearches { get; set; } = new SearchConfig();
        public SearchConfig MobileSearches { get; set; } = new SearchConfig();

        private delegate Task<BrowserBase> SearchDelegate();

        private List<SearchDelegate> searchTypes = new List<SearchDelegate>();

        public Task<BrowserBase> LoginAsync(bool desktop)
        {
            return Task.Run<BrowserBase>(() =>
            {
                return this.Login(desktop);
            });
        }

        public BrowserBase Login(bool desktop)
        {
            BrowserBase search = desktop ? new DesktopBrowser() as BrowserBase : new MobileBrowser() as BrowserBase;
            search.LoginToMicrosoft(Email, Password);
            return search;
        }

        public BrowserBase StartSearches()
        {
            if (Disabled)
            {
                var c = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"{Email} - Disabled in config file");
                Console.ForegroundColor = c;
                return null;
            }

            BrowserBase s = null;

            if (GetDailyPoints && MobileSearches.Disabled && DesktopSearches.Disabled)
            {
                s = new DesktopBrowser();
                s.LoginToMicrosoft(Email, Password);
                s.GetDailyPoints();
                return s;
            }

            if (!DesktopSearches.Disabled)
            {
                Console.WriteLine($"{Email} - Starting Desktop searches");
                s = new DesktopBrowser();
                s.LoginToMicrosoft(Email, Password);

                if (GetDailyPoints)
                    s.GetDailyPoints();

                this.RunSearches(s, DesktopSearches);

                s.GetPointsBreakDown(this.Email);
                
                Console.WriteLine($"{Email} - Desktop searches complete");
                new RandomDelay().Delay("Delay switching to mobile", this.SwitchDelay, this.SwitchDelay + 10);
            }

            if (!MobileSearches.Disabled)
            {
                // Dispose the Desktop browser if it was set
                if (s != null)
                    s.Dispose();

                Console.WriteLine($"{Email} - Starting mobile searches");

                s = new MobileBrowser();
                s.LoginToMicrosoft(Email, Password);

                // Only try and get points if we didn't do it in the desktop searcher
                if (GetDailyPoints && DesktopSearches.Disabled)
                    s.GetDailyPoints();

                this.RunSearches(s, MobileSearches);

                s.GetPointsBreakDown(this.Email);

                Console.WriteLine($"{Email} - Mobile searches complete");
            }

            Console.WriteLine($"{Email} - ALL SEARCHES COMPLETE");

            return s;
        }

        public async Task<BrowserBase> StartSearchesAsync()
        {
            return await Task.Run(() =>
            {
                return this.StartSearches();
            });
        }

        private Task RunSearchesAsync(BrowserBase bing, SearchConfig config)
        {
            return Task.Run(() =>
            {
                RunSearches(bing, config);
            });
        }

        private void RunSearches(BrowserBase browser, SearchConfig config)
        {
            for (int i = 0; i < config.NumSearches; i++)
            {
                List<string> phrase = Program.GetOneSearch(Program.SearchTerms);
                browser.ExecuteSearch(phrase);

                if (config.ClickLinks)
                    browser.ClickLink();

                (int total, int earned) = browser.GetPoints();
                Console.WriteLine($"{this.Email} - Earned {earned}/{total}");
                if (total == earned)
                    break;

                int low = config.SearchDelay <= 5 ? 1 : config.SearchDelay - 5;
                new RandomDelay().Delay($"{Email} - Starting next search", low, config.SearchDelay + 5);
            }
        }

        internal void ExecuteDailyPoints(BrowserBase b)
        {
            b.GetDailyPoints();
        }
    }

    internal class AccountsList
    {
        public List<Account> Accounts { get; set; }

        public static List<Account> LoadAccounts()
        {
            // deserialize JSON directly from a file
            var config = File.Exists(@"config.local.json") ? File.OpenText(@"config.local.json") : File.OpenText(@"config.json");
            using (StreamReader file = config)
            {
                JsonSerializer serializer = new JsonSerializer();
                var accounts = (AccountsList)serializer.Deserialize(file, typeof(AccountsList));
                return accounts.Accounts;
            }
        }
    }

    internal class SearchConfig
    {
        public bool Disabled { get; set; } = false;
        public int NumSearches { get; set; } = 30;
        public int SearchDelay { get; set; } = 65;
        public bool ClickLinks { get; set; } = false;
    }
}

