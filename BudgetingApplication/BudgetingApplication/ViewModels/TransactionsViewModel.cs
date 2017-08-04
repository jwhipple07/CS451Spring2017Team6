using BudgetingApplication.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BudgetingApplication.ViewModels
{
    public class TransactionsViewModel
    {
        public string InvalidAmount { get; set; }
        public string InvalidSplit { get; set; }
        public Transaction Transaction { get; set; }
        public int AccountNo { get; set; } //selected account via filtering
        public List<Account> Accounts { get; set; } //for displaying account filtering options
        public List<BudgetGoal> BudgetGoals { get; set; }
        public List<Category> Categories { get; set; } //for displaying category filtering options
        public string Category { get; set; } //selected category via filtering
        public Client Client { get; set; } //
        public DateTime DateTime { get; set; } //for displaying month as text on top of page
        public int Month { get; set; } //for switching between months
        public List<Transaction> Transactions { get; set; } //client's transactions after filtering and searching
        public int Year { get; set; } //for switching between years
    }
}