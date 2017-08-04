using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BudgetingApplication.Controllers
{
    public class ErrorController : Controller
    {
        // GET: Error
        public ActionResult Index()
        {
            ViewBag.Message = Request["message"];
            return View();
        }

        public ActionResult PageNotFound(string message)
        {
            ViewBag.Message = message;
            return View();
        }

        public ActionResult GeneralError(string message)
        {
            ViewBag.Message = message;
            return View();
        }
    }
}