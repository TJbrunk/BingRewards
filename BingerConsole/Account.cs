using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingerConsole
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

        private delegate Task<BingSearcher> SearchDelegate();

        private List<SearchDelegate> searchTypes = new List<SearchDelegate>();

        public BingSearcher Login(bool desktop)
        {
            BingSearcher search = desktop ? new DesktopSearch() as BingSearcher : new MobileSearch() as BingSearcher;
            search.LoginToMicrosoft(Email, Password);
            return search;
        }

        public BingSearcher StartSearches()
        {
            if (Disabled)
            {
                var c = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"{Email} - Disabled in config file");
                Console.ForegroundColor = c;
                return null;
            }

            BingSearcher s = null;

            if (GetDailyPoints && !MobileSearches.Disabled && !DesktopSearches.Disabled)
            {
                s = new DesktopSearch();
                s.LoginToMicrosoft(Email, Password);
                s.GetDailyPoints();
                return s;
            }

            if (!DesktopSearches.Disabled)
            {
                Console.WriteLine($"{Email} - Starting Desktop searches");
                s = new DesktopSearch();
                s.LoginToMicrosoft(Email, Password);

                if (GetDailyPoints)
                    s.GetDailyPoints();

                this.RunSearches(s, DesktopSearches);

                s.GetPointsBreakDown(this.Email);
                
                Console.WriteLine($"{Email} - Desktop searches complete");
            }

            if (!MobileSearches.Disabled)
            {
                // Dispose the Desktop browser if it was set
                if (s != null)
                    s.Dispose();

                new RandomDelay().Delay("Delay switching to mobile", this.SwitchDelay, this.SwitchDelay + 10);
                Console.WriteLine($"{Email} - Starting mobile searches");

                s = new MobileSearch();
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

        public async Task<BingSearcher> StartSearchesAsync()
        {
            return await Task.Run(() =>
            {
                return this.StartSearches();
            });
        }

        private Task RunSearchesAsync(BingSearcher bing, SearchConfig config)
        {
            return Task.Run(() =>
            {
                RunSearches(bing, config);
            });
        }

        private void RunSearches(BingSearcher browser, SearchConfig config)
        {
            for (int i = 0; i < config.NumSearches; i++)
            {
                List<string> phrase = Program.GetOneSearch(Program.SearchTerms);
                browser.ExecuteSearch(phrase);

                if (config.ClickLinks)
                    browser.ClickLink();

                int low = config.SearchDelay <= 5 ? 1 : config.SearchDelay - 5;
                new RandomDelay().Delay($"{Email} - Starting next search", low, config.SearchDelay + 5);
                (int total, int earned) = browser.GetPoints();
                if (total == earned)
                    break;
            }
        }

        internal void ExecuteDailyPoints(BingSearcher b)
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

