using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BingerConsole
{
    class Program
    {
        private static List<string> _SearchTerms = new List<string>();
        internal static List<string> SearchTerms
        {
            get
            {
                if(DateTime.Now.Subtract(LastSearchTermUpdate).TotalHours > 3)
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

        static void Main(string[] args)
        {
            var accounts = AccountsList.LoadAccounts();

            if (args.Contains("login"))
            {
                Console.WriteLine("Logging into all accounts");
                List<BingSearcher> browsers = new List<BingSearcher>();
                foreach (var a in accounts)
                {
                    browsers.Add(a.Login(true));
                }
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                foreach (var b in browsers)
                {
                    b.Dispose();
                }
            }
            else if (args.Contains("points"))
            {
                List<BingSearcher> browsers = new List<BingSearcher>();
                foreach (var a in accounts)
                {
                    //var a = accounts[2];
                    var browser = a.Login(true);
                    browsers.Add(browser);
                    a.ExecuteDailyPoints(browser);
                }
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                foreach (var b in browsers)
                {
                    b.Dispose();
                }
            }

            else if (args.Contains("async"))
            {
                SearchTerms = GetNewSearches();
                List<Task<BingSearcher>> searchers = new List<Task<BingSearcher>>();

                // Start all the searchers
                accounts.ForEach(a => searchers.Add(a.StartSearchesAsync()));

                // Wait for all searches to complete
                Task.WaitAll(searchers.ToArray());

                Console.WriteLine("All searches complete. Press any key to exit");
                Console.Read();

                // Clean up
                searchers.ForEach(s => s.Result.Dispose());
            }
            else
            {
                SearchTerms = GetNewSearches();

                List<BingSearcher> searchers = new List<BingSearcher>();

                // Run the searches sequentially for accounts
                accounts.ForEach(a => searchers.Add(a.StartSearches()));

                Console.WriteLine("All searches complete. Press any key to exit");
                Console.Read();

                // Clean up
                searchers.ForEach(s => s.Dispose());
            }
        }

        private static List<string> GetNewSearches()
        {
            // Use a unique browser instance incase Bing is tracking stuff
            using (IWebDriver driver = new FirefoxDriver())
            {
                driver.Navigate().GoToUrl("https://trends.google.com/trends/trendingsearches/realtime?geo=US&category=all");
                Thread.Sleep(2000);
                //var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(13));
                //wait.Until(d => {
                //    var btn = d.FindElement(By.ClassName("feed-load-more-button"));
                //    btn.Click();
                //    return true;
                //    });
                driver.FindElement(By.ClassName("feed-load-more-button")).Click();
                ReadOnlyCollection<IWebElement> headlines = driver.FindElements(By.ClassName("title"));
                Console.WriteLine("Getting google headlines");
                while (headlines.Count < 50)
                {
                    Thread.Sleep(3000);
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
