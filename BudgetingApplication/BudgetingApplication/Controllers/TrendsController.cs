using BudgetingApplication.Models;
using BudgetingApplication.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BudgetingApplication.Controllers
{
    public class TrendsController : Controller
    {
        private DataContext dbContext = new DataContext();
        private static int CLIENT_ID;

        public ActionResult checkUser()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            else
            {
                CLIENT_ID = int.Parse(Session["UserID"].ToString());
                return null;
            }
        }

        // GET: Trends
        public ActionResult Index(bool? validDates)
        {
            ActionResult result = checkUser();
            if (result != null) { return result; }

            DateTime endDate = System.DateTime.Now;
            DateTime tempDate = endDate.AddMonths(-3); //default is spending from last three months

            //initialize startDate to be midnight on the first of the month
            DateTime startDate = new DateTime(tempDate.Year, tempDate.Month, 1, 0, 0, 0, 0);

            TrendsViewModel model = this.CreateModel(startDate, endDate, null);

            //set model's ValidDates to true if the validDates parameter is null
            model.ValidDates = validDates == null ? true : false;

            return View(model);
        }



        public ActionResult Filter(string category, string startMonth, string startYear, string endMonth, string endYear)
        {
            //ParseExact method is used to parse both month strings and convert them to an integer

            //gets the number of days in month of the selected ending date
            //used to select the last day of the month
            int daysInMonth = DateTime.DaysInMonth(int.Parse(endYear), DateTime.ParseExact(endMonth, "MMMM", CultureInfo.CurrentCulture).Month);

            //creates the start-date DateTime object
            DateTime startDate = new DateTime(int.Parse(startYear), DateTime.ParseExact(startMonth, "MMMM", CultureInfo.CurrentCulture).Month, 1);

            //creates the end-date DateTime object
            //time of day is set to 11:59 PM, with 59 seconds and 999 milliseconds
            DateTime endDate = new DateTime(int.Parse(endYear), DateTime.ParseExact(endMonth, "MMMM", CultureInfo.CurrentCulture).Month, daysInMonth, 23, 59, 59, 999);

            if(startDate > endDate)
            {
                return RedirectToAction("Index", new { validDates = false });
            }

            TrendsViewModel model = this.CreateModel(startDate, endDate, category);

            if (!String.IsNullOrEmpty(category))
            {
                model.Category = category;
            }
            return View("Index", model);
        }



        private TrendsViewModel CreateModel(DateTime startDate, DateTime endDate, string category)
        {
            TrendsViewModel model = new TrendsViewModel();
            model.StartDate = startDate;
            model.EndDate = endDate;
            //model.BudgetTotals = this.GetBudgetGoalTotals(startDate, endDate);
            model.Categories = this.GetCategories();

            if (!String.IsNullOrEmpty(category))
            {
                DateTime newStartDate = startDate;
                if (startDate.AddMonths(12) < endDate)
                {
                    startDate = endDate.AddMonths(-12);
                    newStartDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, 0);
                    model.StartDate = newStartDate;
                    model.TooManyMonths = true;                   
                }
                Dictionary<string, decimal> initialTransactions = this.GetTransactionsByCategory(newStartDate, endDate, category); 
                Dictionary<string, decimal> updatedTransactions = this.NewDictionary(newStartDate, endDate);
                updatedTransactions = this.UpdateDictionary(initialTransactions, updatedTransactions);
                model.TransactionAmounts = updatedTransactions;

                Dictionary<string, decimal> initialBudgetGoals = this.GetBudgetGoalsByCategory(startDate, endDate, category);
                Dictionary<string, decimal> updatedBudgetGoals = this.NewDictionary(startDate, endDate); 
                updatedBudgetGoals = this.UpdateDictionary(initialBudgetGoals, updatedBudgetGoals);
                model.BudgetTotals = updatedBudgetGoals;

                model.ChartLabels = updatedTransactions.Keys.ToList();
            }
            else
            {
                Dictionary<string, decimal> initialTransactions = this.GetTransactionTotals(startDate, endDate);
                Dictionary<string, decimal> updatedTransactions = this.InitializeDictionary(model.Categories);
                updatedTransactions = this.UpdateDictionary(initialTransactions, updatedTransactions);
                model.TransactionAmounts = updatedTransactions;

                Dictionary<string, decimal> initialBudgetGoals = this.GetBudgetGoalTotals(startDate, endDate);
                Dictionary<string, decimal> updatedBudgetGoals = this.InitializeDictionary(model.Categories);
                updatedBudgetGoals = this.UpdateDictionary(initialBudgetGoals, updatedBudgetGoals);
                model.BudgetTotals = updatedBudgetGoals;

                model.ChartLabels = model.Categories.Where(x => x.CategoryID != 1).Select(x => x.CategoryType).ToList();
                //model.ChartLabels = updatedTransactions.Keys.ToList();
            }
            
            model.TotalSpent = model.TransactionAmounts.Sum(x => x.Value);
            model.MostSpent = model.TransactionAmounts.Max(x => x.Value);
            model.MostSpentCategory = model.TransactionAmounts.OrderByDescending(x => x.Value).First().Key;
            model.LeastSpent = model.TransactionAmounts.Min(x => x.Value);
            model.LeastSpentCategory = model.TransactionAmounts.OrderBy(x => x.Value).First().Key;
            model.AverageSpending = model.TransactionAmounts.Average(x => x.Value);
            model.Colors = this.GenerateColors(model, model.TransactionAmounts.Count());
            model.ValidDates = true;
            return model;
        }



        private Dictionary<string, decimal> GetTransactionTotals(DateTime startDate, DateTime endDate)
        {
            var transactions = from transaction in dbContext.Transactions
                               join account in dbContext.Accounts
                               on transaction.TransactionAccountNo equals account.AccountNo
                               where account.ClientID == CLIENT_ID &&
                                     transaction.TransactionDate >= startDate &&
                                     transaction.TransactionDate <= endDate &&
                                     transaction.Category.ParentCategoryID == null
                                     && transaction.Category.CategoryID != 1
                               orderby transaction.Category.CategoryType
                               select transaction;
            
            var sums = transactions.GroupBy(x => x.Category.CategoryType).ToDictionary(x => x.Key, group => group.Sum(item => Math.Abs(item.TransactionAmount)));
             
            return sums;
        }

        private Dictionary<string, decimal> GetTransactionsByCategory(DateTime startDate, DateTime endDate, string category)
        {
            var transactions = from transaction in dbContext.Transactions
                               join account in dbContext.Accounts
                               on transaction.TransactionAccountNo equals account.AccountNo
                               where account.ClientID == CLIENT_ID &&
                                     transaction.TransactionDate >= startDate &&
                                     transaction.TransactionDate <= endDate &&
                                     transaction.Category.ParentCategoryID == null &&
                                     transaction.Category.CategoryType.Equals(category) &&
                                     transaction.CategoryID != 1
                               orderby transaction.Category.CategoryType
                               select transaction;

            var groupedTransactions = transactions.GroupBy(x => new { x.TransactionDate.Year, x.TransactionDate.Month }).ToDictionary(x => x.Key, group => group.Sum(item => Math.Abs(item.TransactionAmount)));

            Dictionary<string, decimal> dictionary = new Dictionary<string, decimal>();
            foreach (var item in groupedTransactions.Keys)
            {
                string dateString = item.Month.ToString() + "/" + item.Year.ToString();
                dictionary[dateString] = groupedTransactions[item];
            }

            return dictionary;
        }

        private Dictionary<string, decimal> GetBudgetGoalsByCategory(DateTime startDate, DateTime endDate, string category)
        {
            IQueryable<BudgetGoal> budgetGoals = null;
            if (startDate.Month == endDate.Month && startDate.Year == endDate.Year)
            {
                budgetGoals = from goal in dbContext.BudgetGoals
                                  where goal.ClientID == CLIENT_ID &&
                                        goal.Category.CategoryType.Equals(category) &&
                                        goal.Status.Equals("A")
                                  select goal;
            }
            else
            {
                budgetGoals = from goal in dbContext.BudgetGoals
                              orderby goal.Category.CategoryType
                              where goal.ClientID == CLIENT_ID &&
                                    goal.Month >= startDate &&
                                    goal.Month <= endDate &&
                                    goal.Category.CategoryType.Equals(category)
                              select goal;
            }

            var groupedBudgetGoals = budgetGoals.GroupBy(x => new { x.Month.Year, x.Month.Month }).ToDictionary(x => x.Key, group => group.Sum(item => Math.Abs(item.BudgetGoalAmount)));

            Dictionary<string, decimal> dictionary = new Dictionary<string, decimal>();
            foreach (var item in groupedBudgetGoals.Keys)
            {
                string dateString = item.Month.ToString() + "/" + item.Year.ToString();
                dictionary[dateString] = groupedBudgetGoals[item];
            }

            return dictionary;
        }


        private Dictionary<string, decimal> GetBudgetGoalTotals(DateTime startDate, DateTime endDate)
        {
            var budgetGoals = from goal in dbContext.BudgetGoals
                              orderby goal.Category.CategoryType
                              where goal.ClientID == CLIENT_ID &&
                                    goal.Month >= startDate &&
                                    goal.Month <= endDate &&
                                    goal.Category.CategoryID != 1 &&
                                    goal.Category.ParentCategoryID == null
                              select goal;

            var sums = budgetGoals.GroupBy(x => x.Category.CategoryType).ToDictionary(x => x.Key, group => group.Sum(item => Math.Abs(item.BudgetGoalAmount)));

            return sums;
        }



        private List<Category> GetCategories()
        {
            var categories = from category in dbContext.Categories
                             orderby category.CategoryType
                             where category.ParentCategoryID == null &&
                                   category.CategoryID != 1
                             select category;

            return categories.ToList();
        }



        private List<Category> FilterTransactions(List<Category> categories, string categoryString)
        {
            var filteredCategories = from category in categories
                                     where category.CategoryType.Equals(categoryString)
                                     select category;

            return filteredCategories.ToList();
        }

        private Dictionary<string, decimal> InitializeDictionary(List<Category> categories)
        {
            Dictionary<string, decimal> dictionary = new Dictionary<string, decimal>();
            foreach (Category category in categories)
            {
                dictionary[category.CategoryType] = 0;
            }
            return dictionary;
        }

        private Dictionary<string, decimal> UpdateDictionary(Dictionary<string, decimal> initial, Dictionary<string, decimal> updated)
        {
            foreach (string key in initial.Keys)
            {
                updated[key] = initial[key];
            }
            return updated;
        }

        private Dictionary<string, decimal> NewDictionary(DateTime startDate, DateTime endDate)
        {
            Dictionary<string, decimal> dictionary = new Dictionary<string, decimal>();
            int month = startDate.Month;
            int year = startDate.Year;
            while (startDate <= endDate)
            {
                dictionary[startDate.Month.ToString() + "/" + startDate.Year.ToString()] = 0;
                startDate = startDate.AddMonths(1);
            }
            return dictionary;
        }

        private List<string> GenerateColors(TrendsViewModel model, int size)
        {
            List<string> colors = new List<string>();
            for (int i = 0; i < size; i++)
            {
                colors.Add(model.ColorsList[i]);
            }
            return colors;
        }
    }
}