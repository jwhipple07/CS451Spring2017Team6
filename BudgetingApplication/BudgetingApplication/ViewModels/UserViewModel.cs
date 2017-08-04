using BudgetingApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BudgetingApplication.ViewModels
{
    public class UserViewModel
    {
        public Client client;
        public IEnumerable<string> allImages { get; set; }
    }
}