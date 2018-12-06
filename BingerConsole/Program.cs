﻿using HtmlAgilityPack;
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
                List<BingSearcher> browsers = new List<BingSearcher>();
                foreach (var a in accounts)
                {
                    browsers.Add(a.Login());
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
                    var b = a.Login();
                    browsers.Add(b);
                    a.GetPoints(b);
                }
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                foreach (var b in browsers)
                {
                    b.Dispose();
                }
            }
            else
            {
                SearchTerms = GetNewSearches();
                foreach (var a in accounts)
                {
                    a.StartSearches();
                }
            }
        }

        private static List<string> GetNewSearches()
        {
            // Use a unique browser instance incase Bing is tracking stuff
            using (IWebDriver driver = new FirefoxDriver())
            {
                driver.Navigate().GoToUrl("https://trends.google.com/trends/trendingsearches/realtime?geo=US&category=all");
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(13));
                wait.Until(d => {
                    var btn = d.FindElement(By.ClassName("feed-load-more-button"));
                    btn.Click();
                    return true;
                    });
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
