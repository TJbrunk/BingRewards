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
        public SearchConfig DesktopSearches { get; set; }
        public SearchConfig MobileSearches { get; set; }

        private delegate Task<BingSearcher> SearchDelegate();

        private List<SearchDelegate> searchTypes = new List<SearchDelegate>();

        public BingSearcher Login()
        {
            MobileSearch search = new MobileSearch();
            search.LoginToMicrosoft(Email, Password);
            return search;
        }

        public void GetPoints(BingSearcher browser)
        {
            browser.GetPointsBreakDown(this.Email);
        }

        public void StartSearches()
        {
            if (!DesktopSearches.Disabled)
            {
                BingSearcher s = this.RunDesktopSearches();
                this.GetPoints(s);
                s.Dispose();
            }

            if (!MobileSearches.Disabled)
            {
                BingSearcher s = this.RunMobileSearches();
                this.GetPoints(s);
                s.Dispose();
            }
            Console.WriteLine($"{Email} - ALL SEARCHES COMPLETE");
        }
        public async Task StartSearchesAsync()
        {
            await Task.Run(() =>
            {
                this.StartSearches();
            });
        }

        private BingSearcher RunMobileSearches()
        {
            Console.WriteLine($"{Email} - Starting mobile searches");
            MobileSearch browser = new MobileSearch();
            browser.LoginToMicrosoft(Email, Password);
            for (int i = 0; i < MobileSearches.NumSearches; i++)
            {
                List<string> phrase = Program.GetOneSearch(Program.SearchTerms);
                browser.ExecuteSearch(phrase);
                if (MobileSearches.ClickLinks)
                {
                    browser.ClickLink();
                }

                int low = MobileSearches.SearchDelay <= 5 ? 1 : MobileSearches.SearchDelay - 5;
                new RandomDelay().Delay($"{Email} - Starting next search", low, MobileSearches.SearchDelay);
            }
            Console.WriteLine($"{Email} - Mobile searches complete");
            return browser as BingSearcher;
        }

        private Task<BingSearcher> RunMobileSearchesAsync()
        {
            return Task.Run(() =>
            {
                return RunMobileSearches();
            });
        }


        private BingSearcher RunDesktopSearches()
        {
            Console.WriteLine($"{Email} - Starting Desktop searches");
            var browser = new DesktopSearch();
            browser.LoginToMicrosoft(Email, Password);
            for (int i = 0; i < DesktopSearches.NumSearches; i++)
            {
                List<string> phrase = Program.GetOneSearch(Program.SearchTerms);

                browser.ExecuteSearch(phrase);
                if (DesktopSearches.ClickLinks)
                {
                    browser.ClickLink();
                }
                int low = DesktopSearches.SearchDelay <= 5 ? 1 : DesktopSearches.SearchDelay - 5;
                new RandomDelay().Delay($"{Email} - Starting next search", low, DesktopSearches.SearchDelay + 5);
            }
            Console.WriteLine($"{Email} - Desktop searches complete");
            return browser as BingSearcher;
        }

        private Task<BingSearcher> RunDesktopSearchesAsync()
        {
            return Task.Run(() =>
            {
                return RunDesktopSearches();
            });
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
        public int NumSearches { get; set; } = 15;
        public int SearchDelay { get; set; } = 30;
        public bool ClickLinks { get; set; } = false;
    }
}

