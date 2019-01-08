using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BingSearcher
{
    internal abstract class BrowserBase : IDisposable
    {
        internal IWebDriver Driver { get; set; }

        protected void LoadBrowser(ChromeOptions options)
        {
            Driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), options);
        }

        abstract internal void LoginToMicrosoft(string username, string password);

        abstract internal void ClickLink();

        abstract internal void ExecuteSearch(List<string> phrase);

        abstract internal void PrintAllPoints(string email);

        abstract internal (int total, int earned) GetPoints();

        public void Dispose()
        {
            this.Driver.Dispose();
        }

        internal void GetPointsBreakDown(string email)
        {
            try
            {
                Driver.Navigate().GoToUrl("https://account.microsoft.com/rewards/pointsbreakdown");

                Task.Delay(4000);
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(30));
                wait.Until(d => d.FindElement(By.ClassName("ng-isolate-scope")));

                var p = Driver.FindElements(By.ClassName("pointsDetail"));
                var fc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"{email} - Edge Bonus: {p[1].Text}\tPC Points: {p[3].Text}\tMobile: {p[5].Text}\tOther: {p[9].Text}");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = fc;
            }
            catch (Exception)
            {

                Console.WriteLine($"{email} - Failed to get current points");
            }
        }

        internal void GetDailyPoints()
        {
            Driver.Navigate().GoToUrl("https://account.microsoft.com/rewards/");
            var points = FindDailyPoints();
            GetFreePoints(points);
            Thread.Sleep(2000);
            GetDailyPoll(points);
            Thread.Sleep(2000);
            GetDailyQuiz(points);
            Thread.Sleep(2000);
            GetSpecialPromotionPoints();
            Thread.Sleep(2000);
            GetRandomActivities();
        }

        private void GetRandomActivities()
        {
            // Get the whole group of other actvities
            var container = Driver.FindElements(By.ClassName("m-card-group"));


            // get the cards in the container
            var cards = container[2].FindElements(By.ClassName("c-card-content"));

            foreach (var card in cards)
            {
                try
                {
                    // get the cards that haven't been redeemed
                    var c = card.FindElement(By.ClassName("mee-icon-AddMedium"));
                    // Find the link and click it
                    var link = card.FindElement(By.TagName("a"));
                    link.Click();

                    // Return to the microsoft dashboard tab
                    ReadOnlyCollection<string> tabs = Driver.WindowHandles;
                    Driver.SwitchTo().Window(tabs[1]);
                    Driver.Close();
                    Driver.SwitchTo().Window(tabs[0]);
                }
                catch
                {
                }
            }
        }

        private void GetSpecialPromotionPoints()
        {
            try
            {
                Thread.Sleep(10);
                // Usually won't have promotional points.
                //var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                //wait.Until(d => d.FindElement(By.ClassName("promotional-container")));
                var promo = Driver.FindElement(By.ClassName("promotional-container"));
                var link = promo.FindElement(By.TagName("a"));
                link.Click();

                // Return to the microsoft dashboard tab
                ReadOnlyCollection<string> tabs = Driver.WindowHandles;
                Driver.SwitchTo().Window(tabs[1]);
                Driver.Close();
                Driver.SwitchTo().Window(tabs[0]);
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("No promotional points found");
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Timed-out looking for promotional points");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting getting special promotions points. {ex}");
            }
        }

        private ReadOnlyCollection<IWebElement> FindDailyPoints()
        {
            try
            {
                //var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                //wait.Until(d => d.FindElements(By.ClassName("rewards-card-container")));

                //var points = driver.FindElements(By.ClassName("rewards-card-container"));

                var dailySet = Driver.FindElement(By.ClassName("m-card-group"));
                ReadOnlyCollection<IWebElement>  actionLinks = dailySet.FindElements(By.ClassName("c-call-to-action"));
                return actionLinks;
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to find daily points");
                return new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
            }
        }

        private void GetFreePoints(ReadOnlyCollection<IWebElement> actionLinks)
        {
            try
            {
                actionLinks[0].Click();
                // Switch to the new tab
                var tabs = Driver.WindowHandles;
                Driver.SwitchTo().Window(tabs[1]);
                SignInToRewardsIfNeeded();

                // Return to the microsoft dashboard tab
                Driver.Close();
                tabs = Driver.WindowHandles;
                Driver.SwitchTo().Window(tabs[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get free points. {ex}");
            }
        }

        private void GetDailyPoll(ReadOnlyCollection<IWebElement> actionLinks)
        {
            try
            {
                // This opens a new tab
                actionLinks[2].Click();
                Thread.Sleep(10);

                // Switch to the new tab
                ReadOnlyCollection<string> tabs = Driver.WindowHandles;
                Driver.SwitchTo().Window(tabs[1]);

                this.SignInToRewardsIfNeeded();

                // Click on one of the options
                Driver.FindElement(By.ClassName("bt_PollRadio")).Click();
                Thread.Sleep(1000);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error getting daily poll points. {ex}");
            }
            finally
            {
                // Return to the microsoft dashboard tab
                Driver.Close();
                ReadOnlyCollection<string> tabs = Driver.WindowHandles;
                Driver.SwitchTo().Window(tabs[0]);
            }
        }

        private void SignInToRewardsIfNeeded()
        {
            if(Driver.Url.Contains("rewards/signin"))
            {
                var span = Driver.FindElement(By.ClassName("signInOptions"));
                var button = span.FindElement(By.PartialLinkText("/fd/auth/signin?"));
                // var button = span.FindElement(By.TagName("a"));
                button.Click();
            }
        }

        private void GetDailyQuiz(ReadOnlyCollection<IWebElement> actionLinks)
        {
            try
            {
                actionLinks[1].Click();
                // This opens a new tab
                Thread.Sleep(100);

                // Switch to the new tab
                var tabs = Driver.WindowHandles;
                Driver.SwitchTo().Window(tabs[1]);
                SignInToRewardsIfNeeded();

                // Figure out how many questions are in the quiz
                string questions = Driver.FindElement(By.ClassName("FooterText0")).Text;
                Regex regex = new Regex(@"of (?<total>\d+)");
                Match match = regex.Match(questions);
                int total = int.Parse(match.Groups["total"].ToString());

                // Start going through all the questions
                for(int i = 0; i<total; i++)
                {
                    // Pick an answer and select it
                    var answers = Driver.FindElements(By.ClassName("wk_paddingBtm"));

                    var answer = new Random().Next(0, answers.Count - 1);
                    answers[answer].Click();

                    Thread.Sleep(700);
                    // Click the 'NEXT' button
                    Driver.FindElement(By.ClassName("wk_buttons")).Click();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to execute daily quiz. {ex}");
            }
            finally
            {
                // Return to the microsoft dashboard tab
                Driver.Close();
                ReadOnlyCollection<string> tabs = Driver.WindowHandles;
                Driver.SwitchTo().Window(tabs[0]);
            }
        }
    }
}
