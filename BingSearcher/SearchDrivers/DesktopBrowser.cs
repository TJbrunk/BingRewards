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

namespace BingSearcher
{
    internal class DesktopBrowser : BrowserBase
    {
        public DesktopBrowser()
        {
            this.LoadBrowser();
        }

        private void LoadBrowser()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.10240");
            base.LoadBrowser(options);
        }

        internal override void ClickLink()
        {
            try
            {
                new RandomDelay().Delay("Clicking link", 5, 20);
                By css = By.CssSelector("li.b_algo > h2 > a");
                var elements = Driver.FindElements(css);

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
            Driver.Navigate().GoToUrl("http://www.bing.com/");

            // Find the text input element by its name
            IWebElement query = Driver.FindElement(By.Name("q"));
            try
            {
                string search = string.Empty;
                foreach (string word in phrase)
                {
                    query.Clear();

                    new RandomDelay().Delay(null, 1, 2);

                    search += $"{word} ";
                    // Enter something to search for
                    query.SendKeys(search);
                    new RandomDelay().Delay(null, 5, 10);

                    // Now submit the form.
                    //driver.FindElement(By.ClassName("b_searchboxSubmit")).Click();
                    query.Submit();
                    query = Driver.FindElement(By.Name("q"));
                }

                // Wait for the page to load, timeout after 10 seconds
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
                wait.Until(d => d.Title.StartsWith(phrase[0], StringComparison.OrdinalIgnoreCase));
            }
            catch (UnhandledAlertException)
            {
                Driver.SwitchTo().Alert().Dismiss();
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
            Driver.Navigate().GoToUrl("https://login.live.com");

            Console.WriteLine($"{username} - Starting Login");

            new RandomDelay().Delay(null, 3, 10);
            // Enter username
            Driver.FindElement(By.Name("loginfmt")).SendKeys(username);

            // Go to password input
            Driver.FindElement(By.Id("idSIButton9")).Click();


            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(3));
            wait.Until(d => d.FindElement(By.Name("passwd")));

            // Give the page a second to load
            new RandomDelay().Delay(null, 3, 10);

            // Enter the password
            Driver.FindElement(By.Name("passwd")).SendKeys(password);

            wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(3));
            wait.Until(d => d.FindElement(By.Id("idSIButton9")));
            //Submit username and password
            Driver.FindElement(By.Id("idSIButton9")).Click();

            // go to the rewards page as default
            Driver.Navigate().GoToUrl("https://account.microsoft.com/rewards/");

            Console.WriteLine($"{username} - Login Complete");
        }

        internal override (int total, int earned) GetPoints()
        {
            string date = DateTime.Now.ToString("M/dd/yyyy");
            Driver.Navigate().GoToUrl($"https://bing.com/rewardsapp/bepflyoutpage?style=modular&date={date}");
            string pc = Driver.FindElement(By.ClassName("pcsearch")).Text;

            Regex regex = new Regex(@"(?<earned>\d{1,4})\/(?<total>\d{1,4})");
            Match match = regex.Match(pc);
            string earned = match.Groups["earned"].ToString();
            string total = match.Groups["total"].ToString();

            return (int.Parse(total), int.Parse(earned));
        }
    }
}
