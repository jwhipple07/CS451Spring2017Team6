using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BudgetingApplication.Models;
using System.Data.Entity;
using BudgetingApplication.ViewModels;

namespace BudgetingApplication.Controllers
{
    public class BudgetGoalsController : Controller
    {
        private DataContext dbContext = new DataContext();
        private static int CLIENT_ID;

        /// <summary>
        /// Get the view for BudgetGoals 
        /// </summary>
        // GET: BudgetGoals
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            else
            {
                CLIENT_ID = int.Parse(Session["UserID"].ToString());
            }
            updateBudgetGoals();
            List<BudgetGoals_VW> BudgetGoalList = new List<BudgetGoals_VW>();

            //use the view that displays all transaction for current month
            BudgetGoalList = dbContext.BudgetGoals_VW.Where(x => x.ClientID == CLIENT_ID && x.Status == "A").OrderBy(x=>x.TransactionAmount/x.BudgetGoalAmount).ToList();

            BudgetGoalModelView budgetGoal = new BudgetGoalModelView();
            budgetGoal.budgetView = BudgetGoalList;
            budgetGoal.totalBudgeted = budgetGoal.budgetView.Select(x => x).Where(x => x.GoalCategory != 1 && x.GoalCategory != 17).Sum(x => Convert.ToDouble(x.BudgetGoalAmount));
            budgetGoal.totalSpent = budgetGoal.budgetView.Select(x => x).Where(x => x.GoalCategory != 1 && x.GoalCategory != 17).Sum(x => Convert.ToDouble(x.TransactionAmount)) * -1;
            return View(budgetGoal);
        }

        /// <summary>
        /// Used when user wants to create a new budget category or edit a current one. 
        /// If the id of budget is not client's or a real one, then it returns back to the budget page
        /// </summary>
        /// <param name="id">The id of the budget category</param>
        /// <returns>empty budget for inserting or view of budget to edit.</returns>
        public ActionResult InsertBudgetGoal(int? id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            else
            {
                CLIENT_ID = int.Parse(Session["UserID"].ToString());
            }

            BudgetGoalModelView budget = new BudgetGoalModelView();
            budget.category = GetSelectListItems(GetAllCategories());

            //null ID means we are inserting a new category
            if(id == null)
            {
                return View(budget);
            }

            //let's look for it
            budget.budgetGoal = dbContext.BudgetGoals.Find(id);
            budget.budgetView = dbContext.BudgetGoals_VW.Where(x => x.BudgetGoalID == budget.budgetGoal.BudgetGoalID);
            if(budget.budgetGoal == null || budget.budgetGoal.ClientID != CLIENT_ID)
            {
                //we couldn't find the budget category to edit or it is not the client's, redirect
                return RedirectToAction("Index"); 
            }
            return View(budget);
        }

        /// <summary>
        /// The action for the post when user submits the changes. It can tell between inserting new or updating one.
        /// </summary>
        /// <param name="model">This is passed in by form</param>
        /// <returns>returns us back to the budget page</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult InsertBudgetGoal(BudgetGoalModelView model)
        {
            //repopulate the dropdownlist
            model.category = GetSelectListItems(GetAllCategories());
            BudgetGoal newBudgetGoal = new BudgetGoal();
            newBudgetGoal = model.budgetGoal;
            //insert for next month
            newBudgetGoal.ClientID = CLIENT_ID;
            newBudgetGoal.BudgetPointValue = 25;
            if (newBudgetGoal.BudgetGoalID == 0) //create new category
            {
                newBudgetGoal.Month = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                newBudgetGoal.Status = "A";
                dbContext.BudgetGoals.Add(newBudgetGoal);
            }
            else //we're just updating a current budget
            {
                dbContext.Entry(newBudgetGoal).State = EntityState.Modified;
            }
            //ignore validation errors on Month and BudgetGoalId ---temporary fix?
            ModelState.Remove("budgetGoal.Month");
            ModelState.Remove("budgetGoal.BudgetGoalId");
            if (ModelState.IsValid)
            {
                dbContext.SaveChanges();
                return RedirectToAction("index");
            }
            return View(model);
        }
        public ActionResult inactivateBudget(int? id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            else
            {
                CLIENT_ID = int.Parse(Session["UserID"].ToString());
            }

            if(id == null)
            {
                return RedirectToAction("index");
            }
            BudgetGoal bg = dbContext.BudgetGoals.Where(x => x.ClientID == CLIENT_ID && x.BudgetGoalID == id).FirstOrDefault();
            if(bg == null)
            {
                return RedirectToAction("index");
            }
            else
            {
                bg.Status = "I";
                dbContext.SaveChanges();
                return RedirectToAction("index");
            }
        }

        // Just return a list of categories
        private IEnumerable<Category> GetAllCategories()
        {
            return dbContext.Categories.ToList();
        }

        // This returns categories as a selectlist item set
        private IEnumerable<SelectListItem> GetSelectListItems(IEnumerable<Category> elements)
        {
            // Create an empty list to hold result of the operation
            var selectList = new List<SelectListItem>();
            // This will result in MVC rendering each item as:
            //     <option value="CategoryID">Category Type</option>
            foreach (var element in elements)
            {
                if (element.CategoryID == 17) { continue; } //ignore category of Goals, mainly used on transactions
                if(dbContext.BudgetGoals_VW.Where(x=>x.GoalCategory == element.CategoryID && x.ClientID == CLIENT_ID && x.Status == "A").FirstOrDefault() == null) { 
                    selectList.Add(new SelectListItem
                    {
                        Value = element.CategoryID.ToString(),
                        Text = element.CategoryType
                    });
                }
            }
            selectList.OrderBy(x => x.Text);
            return selectList;
        }

        /// <summary>
        /// This method will be used as a scheduled task to update the budget goals
        /// Badges will also be earned for each user, no matter who is currently logged in
        /// </summary>
        private void updateBudgetGoals()
        {
            List<BudgetGoal> budgetGoals = dbContext.BudgetGoals.Where(x => x.Month.Month < DateTime.Now.Month && x.Month.Year <= DateTime.Now.Year && x.GoalCategory != 1 && x.Status =="A").ToList();
            DateTime lastMonth = DateTime.Now.AddMonths(-1);
            List<Transaction> transactions = dbContext.Transactions.Where(x => x.TransactionDate.Month == lastMonth.Month && x.TransactionDate.Year == lastMonth.Year && x.CategoryID != 1).ToList();

            foreach(Client client in dbContext.Clients)
            {
                var account = dbContext.Accounts.Where(x => x.ClientID == client.ClientID).Select(x => x.AccountNo).ToList();
                var total = transactions.Where(x => account.Contains(x.TransactionAccountNo)).Sum(x => x.TransactionAmount);
                var budgeted = budgetGoals.Where(x => x.ClientID == client.ClientID).Sum(x => x.BudgetGoalAmount);

                BadgesModelView bmv = new BadgesModelView();
                if (total <= budgeted)
                {
                    
                    bmv.addNewBadge(84, client.ClientID); //Give user inital load of app badge if they havent earned it.
                    //determine what month of the streak we're at, award badge accordingly

                    //determine if any holidays apply
                    switch (lastMonth.Month)
                    {
                        case (2): //February
                            bmv.addNewBadge(111, client.ClientID); //washington's birthday
                            break;
                        case (3): //March
                            bmv.addNewBadge(112, client.ClientID); //april fools
                            break;
                        case (4): //April
                            bmv.addNewBadge(113, client.ClientID);// easter
                            break;
                        case (5): //May
                            bmv.addNewBadge(114, client.ClientID); //memorial day
                            break;
                        case (6): //June
                            bmv.addNewBadge(116, client.ClientID); //Independence Day
                            break;
                        case (10): //October
                            bmv.addNewBadge(117, client.ClientID); //Halloween
                            break;
                        case (11): //November
                            bmv.addNewBadge(118, client.ClientID); //Thanksgiving
                            break;
                        case (12): //December
                            bmv.addNewBadge(120, client.ClientID); //xmas
                            break;
                    }
                }
                else if(total == budgeted)
                {
                    //client is in the black for this month
                    bmv.addNewBadge(97, client.ClientID);
                }
                else
                {
                    //client went over budget, streak is broken, update DB
                }
            }
            

        }
    }
}
