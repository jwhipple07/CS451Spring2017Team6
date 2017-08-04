using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BudgetingApplication.Models
{
    public class BudgetGoalModelView
    {
        public IEnumerable<BudgetGoals_VW> budgetView { get; set; }
        public BudgetGoal budgetGoal { get; set; }
        public IEnumerable<SelectListItem> category { get; set; }
        public double totalBudgeted { get; set; }
        public double totalSpent { get; set; }
        string progressType { get; set; }
        
        public int getPercentage()
        {
            if(totalBudgeted == 0) { return 0; }
            double percentage = totalSpent / totalBudgeted;
            return Math.Abs((int)(percentage * 100));
        }
    }
}