using Microsoft.Extensions.CommandLineUtils;
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

        static void Main(string[] args)
        {

            CommandLineApplication app = new Microsoft.Extensions.CommandLineUtils.CommandLineApplication();

            var search = app.Command("search", config => {
                config.Description = "Main Program - Run searches to get points";
                config.HelpOption("-? | -h | --help");
                config.OnExecute(()=> {
                   config.ShowHelp();
                   return 1; //return error since we didn't do anything
                });
            });

            search.Command("async", config => {
                config.Description = "Run searches for all the configured accounts at the same time.";
                config.OnExecute(() => {
                    config.Description = "Run searches for all accounts at the same time";

                    var accounts = AccountsList.LoadAccounts();

                    List<Task<BrowserBase>> searchers = new List<Task<BrowserBase>>();

                    SearchTerms = GetNewSearches();

                    // Start all the searchers
                    accounts.ForEach(a =>
                        {
                            searchers.Add(a.StartSearchesAsync());
                            Thread.Sleep(5000);
                        }
                    );

                    // Wait for all searches to complete
                    Task.WaitAll(searchers.ToArray());

                    Console.WriteLine("All searches complete. Press any key to exit");
                    Console.Read();

                    // Clean up
                    searchers.ForEach(s => s.Result.Dispose());
                    return 1;
                });
            });

            search.Command("linear", config => {
                config.Description = "Run searches on the configured accounts one account at a time";
                config.OnExecute(() => {
                    var accounts = AccountsList.LoadAccounts();

                    List<BrowserBase> searchers = new List<BrowserBase>();

                    // Run the searches sequentially for accounts
                    accounts.ForEach(a => searchers.Add(a.StartSearches()));

                    Console.WriteLine("All searches complete. Press any key to exit");
                    Console.Read();

                    // Clean up
                    searchers.ForEach(s => s.Dispose());
                    return 1;
                });
            });

            var login = app.Command("login", config => {
                config.Description = "Opens a browser and logs into all accounts.\n\tUse to manually check points, get daily points, etc";
                config.HelpOption("-? | -h | --help");
                config.OnExecute(() => {
                    Console.WriteLine("Logging into all accounts");
                    var accounts = AccountsList.LoadAccounts();

                    List<Task<BrowserBase>> browsers = new List<Task<BrowserBase>>();

                    accounts.ForEach(a => browsers.Add(a.LoginAsync(true)));

                    Task.WaitAll(browsers.ToArray());
                    Console.WriteLine("Press any key to exit");
                    Console.Read();

                    browsers.ForEach(b => b.Result.Dispose());
                    return 0;
                });
            });

            var points = app.Command("points", config => {
                config.Description = "Logs into each account and (trys) to get the daily points. Daily quiz, Daily poll, etc\n\tLogic not fully flushed out so it may not get all points";
                config.OnExecute(() => {
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
                    return 0;
                });
            });

            points.Command("help", config => {
                config.Description = "get help!";
                config.OnExecute(()=>{
                login.ShowHelp("WIP: Attempts to get daily point (quizzes, polls, etc)");
                    return 1;
                });
            });

             //give people help with --help
            app.HelpOption("-? | -h | --help");

            app.Execute(args);

            if(args.Length == 0){
                app.ShowHelp();
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
