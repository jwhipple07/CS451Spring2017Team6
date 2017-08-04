using BudgetingApplication.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BudgetingApplication.Models
{
    public class indexModelView
    {
        public decimal totalSpent = 0, totalIncome = 0;
        public BudgetGoalModelView budgetGoals { get; set; }
        public SavingsGoalsViewModel savingsGoals { get; set; }
        public IEnumerable<Transaction> transactions { get; set; }

        public IEnumerable<Badge> badges { get; set; }
        public IEnumerable<ClientBadge> clientBadges { get; set; }
    }
}