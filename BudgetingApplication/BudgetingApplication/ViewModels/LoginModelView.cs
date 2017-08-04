using BudgetingApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BudgetingApplication.ViewModels
{
    public class LoginModelView
    {
        public Client client { get; set; }

        public IEnumerable<Client> allClients { get; set; }
    }
}