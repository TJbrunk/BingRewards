using McMaster.Extensions.CommandLineUtils;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BingSearcher
{
    [Subcommand("Account", typeof(Account))]
    class Program
    {
        private static List<string> _SearchTerms = new List<string>();
        internal static List<string> SearchTerms
        {
            get
            {
                if (DateTime.Now.Subtract(LastSearchTermUpdate).TotalHours > 3)
                {
                    _SearchTerms = GetNewSearches();
                    LastSearchTermUpdate = DateTime.Now;
                }
                return _SearchTerms;
            }
            set
            {
                LastSearchTermUpdate = DateTime.Now;
                _SearchTerms = value;
            }
        }

        private static DateTime LastSearchTermUpdate { get; set; }

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Option(Description = "Flag to try and get daily points. Buggy, and doesn't work too well")]
        public bool Points { get; }

        [Option(Description = "Wait time in seconds to wait between switching from Desktop to mobile searches")]
        public int Wait { get; } = 6000;

        [Option( ShortName = "L",
            Description = "Opens browser for all accounts and logs in. Useful for checking points. DOES NOT PERFORM SEARCHES!"
            )]
        public bool Login { get; }

        [Option(Description = "Runs searches for a single account at at time. As opposed to all at once",
        ShortName = "l")]
        public bool Linear { get; }

        [Option(Description = "Keeps the browser windows open at the completion of searches to get daily points, redeem rewards, etc",
        ShortName = "k")]
        public bool KeepOpen { get; }

        private void OnExecute()
        {
            if(Login)
            {
                LoginOnly();
            }

            if(Points)
            {
                Console.WriteLine("Getting Points");
                GetPoints();
            }

            if(Linear)
            {
                Console.WriteLine("Linear searches");
                SearchLinear();
            }
            else
            {
                Console.WriteLine("Default Async searches");
                SearchAsync();
            }
        }

        private void SearchAsync()
        {
            var accounts = AccountsList.LoadAccounts();

            SearchTerms = GetNewSearches();
            List<Task<BrowserBase>> searchers = new List<Task<BrowserBase>>();

            // Start all the searchers
            accounts.ForEach(a =>
                {
                    searchers.Add(a.StartSearchesAsync());
                    Thread.Sleep(3000);
                }
            );

            // Wait for all searches to complete
            Task.WaitAll(searchers.ToArray());

            if(KeepOpen)
            {
                Console.WriteLine("All searches complete. Press any key to exit");
                Console.Read();
            }
            // Clean up
            searchers.ForEach(s => s.Result.Dispose());
        }

        private void SearchLinear()
        {
            var accounts = AccountsList.LoadAccounts();

            List<BrowserBase> searchers = new List<BrowserBase>();

            // Run the searches sequentially for accounts
            accounts.ForEach(a => searchers.Add(a.StartSearches()));

            if(KeepOpen)
            {
                Console.WriteLine("All searches complete. Press any key to exit");
                Console.Read();
            }

            // Clean up
            searchers.ForEach(s => s.Dispose());
        }

        private void LoginOnly()
        {
            Console.WriteLine("Logging into all accounts");
            var accounts = AccountsList.LoadAccounts();

            List<Task<BrowserBase>> browsers = new List<Task<BrowserBase>>();

            accounts.ForEach(a => browsers.Add(a.LoginAsync(true)));

            Task.WaitAll(browsers.ToArray());
            Console.WriteLine("Press any key to exit");
            Console.Read();

            browsers.ForEach(b => b.Result.Dispose());
        }

        private void GetPoints()
        {
            var accounts = AccountsList.LoadAccounts();

            List<BrowserBase> browsers = new List<BrowserBase>();
            foreach (var a in accounts)
            {
                var browser = a.Login(true);
                browsers.Add(browser);
                a.ExecuteDailyPoints(browser);
            }
            Console.WriteLine("Press any key to exit");
            Console.Read();
            foreach (var b in browsers)
            {
                b.Dispose();
            }
        }

        private static List<string> GetNewSearches()
        {
            var d = Directory.GetCurrentDirectory();
            var f = Directory.GetFiles(d);
            foreach (var item in f)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine(d);
            // Use a unique browser instance incase Bing is tracking stuff
            using (IWebDriver driver = new FirefoxDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                driver.Navigate().GoToUrl("https://trends.google.com/trends/trendingsearches/realtime?geo=US&category=all");
                Thread.Sleep(5000);

                driver.FindElement(By.ClassName("feed-load-more-button")).Click();
                ReadOnlyCollection<IWebElement> headlines = driver.FindElements(By.ClassName("title"));

                Console.WriteLine("Getting google headlines");
                while (headlines.Count < 15)
                {
                    Thread.Sleep(1500);
                    try
                    {
                        driver.FindElement(By.ClassName("feed-load-more-button")).Click();
                    }
                    catch (NoSuchElementException)
                    {
                        headlines = driver.FindElements(By.ClassName("title"));
                        break;
                    }
                    headlines = driver.FindElements(By.ClassName("title"));
                }

                List<string> terms = new List<string>();
                foreach (var headline in headlines)
                {
                    terms.Add(headline.Text);
                }
                return terms;
            }
        }


        internal static List<string> GetOneSearch(List<string> terms)
        {
            Random rnd = new Random();
            string term = terms.ElementAt(rnd.Next(terms.Count - 1));
            term = term.Replace(@"• ", "");
            var phrase = term.Split(" ".ToCharArray(), options: StringSplitOptions.RemoveEmptyEntries);
            return phrase.Take(5).ToList();
        }
    }
}
