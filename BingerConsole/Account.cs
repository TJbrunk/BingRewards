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

        public async Task StartSearchesAsync()
        {

            await Task.Run(async () =>
            {
                if(DesktopSearches.Enabled && MobileSearches.Enabled)
                {
                    this.searchTypes.Add(RunMobileSearches);
                    this.searchTypes.Add(RunDesktopSearches);
                    var x = new Random().Next(0, 1);
                    await this.searchTypes[x]();

                    new RandomDelay().Delay($"{Email} - Switching search types", 2000, 3000);
                    BingSearcher s;
                    if (x == 0)
                    {
                        s = await this.searchTypes[1]();
                    }
                    else
                    {
                        s = await this.searchTypes[0]();
                    }
                    this.GetPoints(s);
                    s.Dispose();
                }
                else if(DesktopSearches.Enabled)
                {
                    BingSearcher s = await this.RunDesktopSearches();
                    this.GetPoints(s);
                    s.Dispose();
                }
                else if(MobileSearches.Enabled)
                {
                    BingSearcher s = await this.RunMobileSearches();
                    this.GetPoints(s);
                    s.Dispose();
                }
                Console.WriteLine($"{Email} - ALL SEARCHES COMPLETE");
            });
        }

        private Task<BingSearcher> RunMobileSearches()
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"{Email} - Starting mobile searches");
                MobileSearch browser = new MobileSearch();
                int numSearches = new Random().Next(MobileSearches.MinSearches, MobileSearches.MaxSearches);
                browser.LoginToMicrosoft(Email, Password);
                for (int i = 0; i < numSearches; i++)
                {
                    List<string> phrase = Program.GetOneSearch(Program.SearchTerms);
                    browser.ExecuteSearch(phrase);
                    browser.ClickLink();
                    new RandomDelay().Delay($"{Email} - Starting next search", MobileSearches.MinDelay, MobileSearches.MinDelay);
                }
                Console.WriteLine($"{Email} - Mobile searches complete");
                return browser as BingSearcher;
            });
        }


        private Task<BingSearcher> RunDesktopSearches()
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"{Email} - Starting Desktop searches");
                var browser = new DesktopSearch();
                browser.LoginToMicrosoft(Email, Password);
                int numSearches = new Random().Next(DesktopSearches.MinSearches, DesktopSearches.MaxSearches);
                for (int i = 0; i < numSearches; i++)
                {
                    List<string> phrase = Program.GetOneSearch(Program.SearchTerms);

                    browser.ExecuteSearch(phrase);
                    browser.ClickLink();
                    new RandomDelay().Delay($"{Email} - Starting next search", DesktopSearches.MinDelay, DesktopSearches.MaxDelay);
                }
                Console.WriteLine($"{Email} - Desktop searches complete");
                return browser as BingSearcher;
            });
        }
    }

    internal class AccountsList
    {
        public List<Account> Accounts { get; set; }

        public static List<Account> LoadAccounts()
        {
            // deserialize JSON directly from a file
            using (StreamReader file = File.OpenText(@"config.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                var accounts = (AccountsList)serializer.Deserialize(file, typeof(AccountsList));
                return accounts.Accounts;
            }
        }
    }

    internal class SearchConfig
    {
        public bool Enabled { get; set; }
        public int MaxSearches { get; set; }
        public int MinSearches { get; set; }
        public int MaxDelay { get; set; }
        public int MinDelay { get; set; }
    }
}

