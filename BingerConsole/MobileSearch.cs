using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace BingerConsole
{
    internal class MobileSearch : BingSearcher
    {
        public MobileSearch()
        {
            this.LoadBrowser();
        }

        private void LoadBrowser()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-agent=Mozilla/5.0 (Linux; U; Android 4.0.2; en-us; Galaxy Nexus Build/ICL53F) AppleWebKit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30");
            driver = new ChromeDriver(options);
        }

        internal override void ClickLink()
        {
            try
            { 
                new RandomDelay().Delay("Clicking link", 5, 20);
                By css = By.CssSelector("#b_results > li > div.b_algoheader > a");
                var elements = driver.FindElements(css);

                elements[new Random().Next(elements.Count - 1)].Click();
            }
            catch (Exception)
            {
                var c = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to click link in search results");
                Console.ForegroundColor = c;
            }
        }

        internal override void ExecuteSearch(List<string> phrase)
        {
            driver.Navigate().GoToUrl("http://www.bing.com/");
            
            // Find the text input element by its name
            IWebElement query = driver.FindElement(By.Name("q"));

            try
            {
                string search = string.Empty;
                foreach (string word in phrase)
                {
                    query.Clear();

                    new RandomDelay().Delay(null, 5, 12);

                    search += $"{word} ";
                    // Enter something to search for
                    query.SendKeys(search);

                    // Now submit the form.
                    query.Submit();
                    query = driver.FindElement(By.Name("q"));
                }
            
                // Wait for the page to load, timeout after 20 seconds
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                wait.Until(d => d.Title.StartsWith(phrase[0], StringComparison.OrdinalIgnoreCase));
            }
            catch(UnhandledAlertException)
            {
                driver.SwitchTo().Alert().Dismiss();
            }
            catch (WebDriverTimeoutException)
            {
                // Try one more time to excute the search
                query.Submit();
            }
        }

        internal override void LoginToMicrosoft(string username, string password)
        {
            // Go to login page
            driver.Navigate().GoToUrl("https://login.live.com");

            new RandomDelay().Delay("Entering username", 3, 10);
            // Enter username
            driver.FindElement(By.Name("loginfmt")).SendKeys(username);

            // Go to password input
            driver.FindElement(By.Id("idSIButton9")).Click();


            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(3));
            wait.Until(d => d.FindElement(By.Name("passwd")));

            // Give the page a second to load
            new RandomDelay().Delay("Entering password", 3, 10);

            // Enter the password
            driver.FindElement(By.Name("passwd")).SendKeys(password);

            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(3));
            wait.Until(d => d.FindElement(By.Id("idSIButton9")));
            //Submit username and password
            driver.FindElement(By.Id("idSIButton9")).Click();
        }

        internal override void PrintAllPoints(string email)
        {
            string date = DateTime.Now.ToString("M/dd/yyyy");
            driver.Navigate().GoToUrl($"https://bing.com/rewardsapp/bepflyoutpage?style=modular&date={date}");
            string pc = driver.FindElement(By.ClassName("pcsearch")).Text;

            string mobile = driver.FindElement(By.ClassName("mobilesearch")).Text;

            string edge = driver.FindElement(By.ClassName("edgesearch")).Text;
            Console.WriteLine($"PC: {pc}\t Mobile: {mobile}\t Edge: {edge}");
        }

        internal override (int total, int earned) GetPoints()
        {
            try
            {
                string date = DateTime.Now.ToString("M/dd/yyyy");
                driver.Navigate().GoToUrl($"https://bing.com/rewardsapp/bepflyoutpage?style=modular&date={date}");
                string pc = driver.FindElement(By.ClassName("mobilesearch")).Text;

                Regex regex = new Regex(@"(?<earned>\d{1,4})\/(?<total>\d{1,4})");
                Match match = regex.Match(pc);
                int earned = int.Parse(match.Groups["earned"].ToString());
                int total = int.Parse(match.Groups["total"].ToString());

                return (total, earned);
            }
            catch (Exception)
            {
                return (1, 0);
            }
        }
    }
}
