using BudgetingApplication.Models;
using BudgetingApplication.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace BudgetingApplication.Controllers
{
    public class HomeController : Controller
    {
        DataContext dbContext = new DataContext();

       
        public static int CLIENT_ID;
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

        // GET: Home
        public ActionResult Index()
        {
            ActionResult result = checkUser();
            if(result != null) { return result; }

            BadgesModelView bmv = new BadgesModelView();
            bmv.addNewBadge(74, CLIENT_ID); //Give user inital load of app badge if they havent earned it.

            indexModelView mv = new indexModelView();
            mv.savingsGoals = getSavingGoals();
            mv.budgetGoals = getBudgetInfo();
            mv.transactions = getTransactionInfo();
            mv.badges = getAllBadges();
            mv.clientBadges = getClientBadges();
            decimal[] totals = getTotalIncomeAndSpent();
            mv.totalIncome = totals[0];
            mv.totalSpent = totals[1];
            badgeTriggers();
            return View(mv);
        }

        /// <summary>
        /// This was an assignment that was given to us. This may not work with all the updated changes that have been made. 
        /// </summary>
        /// <returns></returns>
        public ActionResult TechnicalPrototype()
        {
            checkUser();
            return View();
        }
        /// <summary>
        /// Clear the session cache, and list all clients to choose.
        /// </summary>
        /// <returns></returns>
        public ActionResult Login()
        {
            Session.Clear();
            LoginModelView loginView = new LoginModelView();
            loginView.allClients = dbContext.Clients.ToList();
            return View(loginView);
        }

        /// <summary>
        /// Clears session cache, then go to log in page.
        /// </summary>
        /// <returns></returns>
        public ActionResult Logout()
        {
            Session.Clear();
            CLIENT_ID = -1;
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Validates that the log in page passed correct values for logging in. 
        /// Else goes back to the login page.
        /// </summary>
        /// <param name="objClient"></param>
        /// <returns>redirect back to overview page or if error back to log in screen.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModelView objClient)
        {
            if (ModelState.IsValid)
            {
                using (dbContext)
                {
                    var obj = dbContext.Clients.Where(a => a.Username.Equals(objClient.client.Username)).FirstOrDefault();
                    if (obj != null)
                    {
                        Session["UserID"] = obj.ClientID.ToString();
                        Session["UserName"] = obj.Username.ToString();
                        Session["FullName"] = obj.FirstName + " " + obj.LastName;
                        string photourl = "/Images/avatar-default-icon.png";
                        if (obj.PhotoURL != null && obj.PhotoURL != "")
                        {
                            photourl = "/Images/Users/" + obj.ClientID + "/" + obj.PhotoURL;
                        }
                        Session["img"] = photourl;
                        return RedirectToAction("index");
                    }
                }
            }
            return View(objClient);
        }

        /// <summary>
        /// Calculates the total income from all of the client's accounts and the total
        /// amount spent. This is for the current month. 
        /// </summary>
        /// <returns>2 element array with total income first and spent second.</returns>
        private decimal[] getTotalIncomeAndSpent()
        {
            decimal spent = 0, income = 0;
            //Each account get all transactions and sum them up.
            var query = from trans in dbContext.Transactions
                        from account in dbContext.Accounts
                        where trans.TransactionAccountNo == account.AccountNo && account.ClientID == CLIENT_ID
                        && trans.TransactionDate.Month == System.DateTime.Now.Month && trans.TransactionDate.Year == System.DateTime.Now.Year
                        group new { trans, account } by trans.CategoryID;

            //sum up all different accounts into one 
            foreach (var item in query)
            {
                foreach(var grouping in item)
                {
                    switch (grouping.trans.CategoryID)
                    {
                        case (1):
                            //deposit
                            income += grouping.trans.TransactionAmount;
                            break;
                        case (17):
                            //transfers into a goal, money was not actually spent
                            break;
                        default:
                            spent += Math.Abs(grouping.trans.TransactionAmount);
                            break;

                    }
                }
            }
            return new decimal[] { income, spent };
        }

        /// <summary>
        /// Get all the badges available
        /// </summary>
        /// <returns>All badges</returns>
        private IEnumerable<Badge> getAllBadges()
        {
            return dbContext.Badges;
        }

        /// <summary>
        /// Get all the badges earned by logged in client
        /// </summary>
        /// <returns>All earned badges</returns>
        private IEnumerable<ClientBadge> getClientBadges()
        {
            return dbContext.ClientBadges.Where(x => x.ClientID == CLIENT_ID);
        }

        /// <summary>
        /// Get all the transactions made by the Client in each of their accounts.
        /// Then calaculate the total amount in each account
        /// The purpose of this is to show the total amounts in the sidebar on the overview page.
        /// </summary>
        /// <returns>List of all transactions, ever</returns>
        public IEnumerable<Transaction> getTransactionInfo()
        {
            List<Transaction> transactions = new List<Transaction>();

            //get all accounts and the sum of them, create a new object to hold them
            var query2 = from trans in dbContext.Transactions
                    from account in dbContext.Accounts
                    where trans.TransactionAccountNo == account.AccountNo && account.ClientID == CLIENT_ID
                    group new { trans, account } by trans.TransactionAccountNo into f
                    select new
                    {
                        accountDescr = f.Select(x => x.account.AccountType),
                        accountNo = f.Select(x => x.account.AccountNo),
                        totalMoney = f.Sum(x => x.trans.TransactionAmount)
                    };

            //convert the created object to Transaction, add to the list
            foreach (var item in query2)
            {
                Transaction trans = new Transaction();
                trans.Description = item.accountDescr.First();
                trans.TransactionAccountNo = item.accountNo.First();
                trans.TransactionAmount = item.totalMoney;
                transactions.Add(trans);
            }
            return transactions;
        }

        /// <summary>
        /// Calculate the budget of the client to display on the overview page.
        /// </summary>
        /// <returns></returns>
        private BudgetGoalModelView getBudgetInfo()
        {
            List<BudgetGoals_VW> BudgetGoalList = new List<BudgetGoals_VW>();

            //all budgets categories the client has
            BudgetGoalList = dbContext.BudgetGoals_VW.Where(x => x.ClientID == CLIENT_ID && x.Status == "A").OrderBy(x => x.TransactionAmount / x.BudgetGoalAmount).ToList();

            //Insert them into the view model, calculate the totals
            BudgetGoalModelView budgetGoal = new BudgetGoalModelView();
            budgetGoal.budgetView = BudgetGoalList;
            budgetGoal.totalBudgeted = budgetGoal.budgetView.Select(x => x).Where(x => x.GoalCategory != 1 && x.GoalCategory != 17).Sum(x => Convert.ToDouble(x.BudgetGoalAmount));
            budgetGoal.totalSpent = budgetGoal.budgetView.Select(x => x).Where(x => x.GoalCategory != 1 && x.GoalCategory != 17).Sum(x => Convert.ToDouble(x.TransactionAmount)) * -1;
            return budgetGoal;
        }

        private SavingsGoalsViewModel getSavingGoals()
        {
            List<SavingsGoal> SavingsGoalList = new List<SavingsGoal>();
            SavingsGoalList = dbContext.SavingsGoals.Where(x => x.ClientID == CLIENT_ID && x.Status == "Active").OrderByDescending(x => x.CurrentGoalAmount / x.SavingsGoalAmount).ToList();

            SavingsGoalsViewModel savingsGoal = new SavingsGoalsViewModel();
            savingsGoal.savingsView = SavingsGoalList;
            savingsGoal.totalBudgeted = savingsGoal.savingsView.Select(x => x).Where(x => x.SavingGoalID != 0).Sum(x => Convert.ToDouble(x.SavingsGoalAmount));
            savingsGoal.totalSaved = savingsGoal.savingsView.Select(x => x).Sum(x => Convert.ToDouble(x.CurrentGoalAmount));
            return savingsGoal;
        }
        /// <summary>
        /// JSON requests for the charts that are shown in the overview page. This JSON data is created
        /// to fit the needs of Chart.js.
        /// This format of JSON works on all different types of charts, refer to the BudgetApplication.js.
        /// </summary>
        /// <returns>JSON of graph data</returns>
        public JsonResult GetDonutAndBarGraphData()
        {
            List<Transaction> tr = new List<Transaction>();
            Dictionary<string, List<object>> dataDict = new Dictionary<string, List<object>>();
            Dictionary<string, List<object>> datasetsDict = new Dictionary<string, List<object>>();

            //get each transaction made this month.
            var monthlyTransacations = from trans in dbContext.Transactions
                                       from account in dbContext.Accounts
                                       where trans.TransactionDate.Month == DateTime.Now.Month && trans.TransactionDate.Year == DateTime.Now.Year && trans.TransactionAccountNo == account.AccountNo && account.ClientID == CLIENT_ID
                                       select new { trans, account };

            //organize the transaction into categories and sum the total spent.
            var sumQuery = from querys in monthlyTransacations
                           group querys by new { querys.account.ClientID, querys.trans.CategoryID } into f
                           select new
                           {
                               ClientID = f.Key.ClientID,
                               TransactionAmount = f.Sum(x => x.trans.TransactionAmount),
                               CategoryID = f.Max(x => x.trans.CategoryID),
                               CategoryType = f.Select(x => x.trans.Category.CategoryType)

                           };


            List<object> labels = new List<object>();
            List<object> nums = new List<object>();

            //colors of the graph sections. 
            string[] colors = {
                "#000000", //black
                "#8b84b7", //lavendar purple
                "#264653", //japanese indigo
                "#2a9d8f", //jungle green
                "#e9c46a", //hansa yellow
                "#f4a261", //sandy brown
                "#e76f51", //light red ochre
                "#bce784", //medium spring bud
                "#348aa7", //steel blue
                "#5dd39e", //medium aquamarine
                "#525174", //independence
                "#513b56", //pineapple
                "#ff6b6b", //pastel red
                "#ffe66d"  //maize
            };

            foreach (var trans in sumQuery)
            {
                //add all categories to the labels except category 1 (this is the income category)
                //and 17 - the transfers to goals.
                if (trans.CategoryID != 1 && trans.CategoryID != 17)
                {
                    labels.Add(trans.CategoryType.First());
                    nums.Add(Math.Abs(trans.TransactionAmount));
                }
            }

            dataDict["datasets"] = new List<object>();

            datasetsDict["backgroundColor"] = colors.ToList<object>();
            datasetsDict["data"] = nums;

            dataDict["datasets"].Add(datasetsDict);
            dataDict["labels"] = labels;

            return new JsonResult { Data = dataDict, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Get all the badges that the user has earned but have not been notified of yet.
        /// Once notified, change the status.
        /// </summary>
        /// <returns>JSON of badge info</returns>
        public JsonResult GetNewlyEarnedBadges()
        {
            Client c = dbContext.Clients.Where(x => x.ClientID == CLIENT_ID).FirstOrDefault();
            if (c != null  && c.Notify == "N") //user does not want notifications
            {
                return new JsonResult
                {
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            //get all badges that client has earned, but the status is "new"
            List<ClientBadge> newlyEarnedBadges = dbContext.ClientBadges.Where(x => x.ClientID == CLIENT_ID && x.Status == "new").ToList();
            Dictionary<String, object> dict = new Dictionary<String, object>();
            
            //get the badge name and details, change the client's badge status
            foreach ( ClientBadge cb in newlyEarnedBadges)
            {
                Badge badge = dbContext.Badges.Where(x => x.BadgeID == cb.BadgeID).FirstOrDefault();
                var badgeInfo = new { url = badge.BadgeName, date = cb.DateEarned.Date.ToLongDateString(), descr = badge.Description };
                dict[cb.BadgeID.ToString()] = badgeInfo;
                cb.Status = "earned";
            }

            //updated the badges as notified. 
            dbContext.SaveChangesAsync();

            return new JsonResult {Data = dict, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public JsonResult getWarnings()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();


            if (Session["UserID"] != null)
            {
                Client c = dbContext.Clients.Where(x => x.ClientID == CLIENT_ID).FirstOrDefault();
                if (c.Notify == "N") //user does not want notifications
                {
                   return new JsonResult
                    {
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                }
                //get budget warnings
                List<BudgetGoals_VW> BudgetGoalList = dbContext.BudgetGoals_VW.Where(x => x.ClientID == CLIENT_ID).ToList();
                foreach (BudgetGoals_VW budgetGoal in BudgetGoalList)
                {
                    int percentage = (int)(100 * (Math.Abs(budgetGoal.TransactionAmount) / budgetGoal.BudgetGoalAmount));
                    if(percentage > 100)
                    {
                        dict[budgetGoal.CategoryType] = "You have exceeded your " + budgetGoal.CategoryType + " budget by $" + (int)(Math.Abs(budgetGoal.TransactionAmount) - budgetGoal.BudgetGoalAmount) + ".";
                    }
                    else if (percentage > 90)
                    {
                        dict[budgetGoal.CategoryType] = "You are within " + (100 - percentage) + "% of exceeding your " + budgetGoal.CategoryType + " budget.";
                    }
                }
            }

            return new JsonResult { Data = dict, JsonRequestBehavior = JsonRequestBehavior.AllowGet };

        }
        
        //adds badge if user has started
        public void badgeTriggers()
        {
            BadgesModelView bmv = new BadgesModelView();
            bmv.addNewBadge(74, CLIENT_ID);
        }
    }
}
