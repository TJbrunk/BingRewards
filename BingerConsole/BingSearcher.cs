using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
