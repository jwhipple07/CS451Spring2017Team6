using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Data.Entity;
using BudgetingApplication.Models;
using BudgetingApplication.Controllers;

namespace BudgetingApplication
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            ModelBinders.Binders.Add(typeof(decimal), new DecimalModelBinder());
            RouteConfig.RegisterRoutes(RouteTable.Routes);

        }
        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            Response.Clear();

            HttpException httpException = exception as HttpException;

            if (httpException != null)
            {
                string action;

                switch (httpException.GetHttpCode())
                {
                    case 404:
                        // page not found
                        action = "PageNotFound";
                        break;
                    case 500:
                        // server error
                        action = "GeneralError";
                        break;
                    default:
                        action = "GeneralError";
                        break;
                }

                // clear error on server
                Server.ClearError();
                String urlClean = HttpContext.Current.Server.UrlEncode(exception.Message);
                Response.Redirect(String.Format("~/Error/{0}/?message={1}", action, urlClean));
            } else
            {
                String urlClean = HttpContext.Current.Server.UrlEncode(exception.Message);
                Response.Redirect(String.Format("~/Error/{0}/?message={1}", "index", urlClean));
            }
        }
    }
}
