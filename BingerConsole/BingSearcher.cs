using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BingerConsole
{
    internal abstract class BingSearcher : IDisposable
    {
        internal IWebDriver driver { get; set; }

        abstract internal void LoginToMicrosoft(string username, string password);

        abstract internal void ClickLink();

        abstract internal void ExecuteSearch(List<string> phrase);

        abstract internal void PrintAllPoints(string email);

        abstract internal (int total, int earned) GetPoints();
        public void Dispose()
        {
            this.driver.Dispose();
        }

        internal void GetPointsBreakDown(string email)
        {
            try
            {
                driver.Navigate().GoToUrl("https://account.microsoft.com/rewards/pointsbreakdown");

                Task.Delay(4000);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                wait.Until(d => d.FindElement(By.ClassName("ng-isolate-scope")));

                var p = driver.FindElements(By.ClassName("pointsDetail"));
                //var pc = driver.FindElement(By.CssSelector("#userPointsBreakdown > div > div:nth-child(2) > div:nth-child(2) > div > div.pointsDetail > mee-rewards-user-points-details > div > div > div > div > p.pointsDetail.c-subheading-3.ng-binding.x-hidden-focus")).Text;
                //var edge = driver.FindElement(By.CssSelector("#userPointsBreakdown > div > div:nth-child(2) > div:nth-child(1) > div > div.pointsDetail > mee-rewards-user-points-details > div > div > div > div > p.pointsDetail.c-subheading-3.ng-binding.x-hidden-focus")).Text;
                //var mobile = driver.FindElement(By.CssSelector("#userPointsBreakdown > div > div:nth-child(2) > div:nth-child(3) > div > div.pointsDetail > mee-rewards-user-points-details > div > div > div > div > p.pointsDetail.c-subheading-3.ng-binding.x-hidden-focus")).Text;
                //var other = driver.FindElement(By.CssSelector("#userPointsBreakdown > div > div:nth-child(2) > div:nth-child(5) > div > div.pointsDetail > mee-rewards-user-points-details > div > div > div > div > p.pointsDetail.c-subheading-3.ng-binding.x-hidden-focus")).Text;
                var fc = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Magenta;
                //Console.WriteLine($"{email} - Edge Bonus: {edge}\tPC Points: {pc}\tMobile: {mobile}\tOther: {other}");
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
            var points = FindDailyPoints();
            GetFreePoints(points);
            GetDailyPoll(points);
            GetDailyQuiz(points);
        }

        private ReadOnlyCollection<IWebElement> FindDailyPoints()
        {
            driver.Navigate().GoToUrl("https://account.microsoft.com/rewards/");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait.Until(d => d.FindElements(By.ClassName("rewards-card-container")));

            //var points = driver.FindElements(By.ClassName("rewards-card-container"));

            var dailySet = driver.FindElement(By.ClassName("m-card-group"));
            var actionLinks = dailySet.FindElements(By.ClassName("c-call-to-action"));
            return actionLinks;
        }

        private void GetFreePoints(ReadOnlyCollection<IWebElement> actionLinks)
        {
            try
            {
                actionLinks[0].Click();
                // Switch to the new tab
                var tabs = driver.WindowHandles;
                driver.SwitchTo().Window(tabs[1]);

                // Return to the microsoft dashboard tab
                driver.Close();
                tabs = driver.WindowHandles;
                driver.SwitchTo().Window(tabs[0]);
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
                ReadOnlyCollection<string> tabs = driver.WindowHandles;
                driver.SwitchTo().Window(tabs[1]);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                wait.Until(d => d.FindElements(By.ClassName("bt_PollRadio")));

                // Click on one of the options
                driver.FindElement(By.ClassName("bt_PollRadio")).Click();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error getting daily poll points. {ex}");
            }
            finally
            {
                // Return to the microsoft dashboard tab
                driver.Close();
                ReadOnlyCollection<string> tabs = driver.WindowHandles;
                driver.SwitchTo().Window(tabs[0]);
            }
        }

        private void GetDailyQuiz(ReadOnlyCollection<IWebElement> actionLinks)
        {
            try
            {
                actionLinks[1].Click();
                // This opens a new tab
                Thread.Sleep(10);

                // Switch to the new tab
                var tabs = driver.WindowHandles;
                driver.SwitchTo().Window(tabs[1]);

                // Figure out how many questions are in the quiz
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                wait.Until(d => d.FindElement(By.ClassName("FooterText0")));
                string questions = driver.FindElement(By.ClassName("FooterText0")).Text;
                Regex regex = new Regex(@"of (?<total>\d)");
                Match match = regex.Match(questions);
                int total = int.Parse(match.Groups["total"].ToString());

                // Start going through all the questions
                for(int i = 0; i<total; i++)
                {
                    // Pick an answer and select it
                    wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                    wait.Until(d => d.FindElements(By.ClassName("wk_paddingBtm")));
                    var answers = driver.FindElements(By.ClassName("wk_paddingBtm"));

                    // TODO: Pick a random answer
                    answers[2].Click();

                    Thread.Sleep(10);
                    // Click the 'NEXT' button
                    driver.FindElement(By.ClassName("wk_buttons")).Click();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to execute daily quiz. {ex}");
            }
            finally
            {
                // Return to the microsoft dashboard tab
                driver.Close();
                ReadOnlyCollection<string> tabs = driver.WindowHandles;
                driver.SwitchTo().Window(tabs[0]);
            }
        }
    }
}
