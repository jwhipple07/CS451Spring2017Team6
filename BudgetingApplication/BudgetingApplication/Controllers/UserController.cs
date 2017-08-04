using BudgetingApplication.Models;
using BudgetingApplication.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BudgetingApplication.Controllers
{
    public class UserController : Controller
    {
        private DataContext dbContext = new DataContext();
        private static int CLIENT_ID;
        // GET: User
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
            return RedirectToAction("Details", new { id = CLIENT_ID });
        }

        // GET: User/Details/5
        public ActionResult Details(int id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            else
            {
                CLIENT_ID = int.Parse(Session["UserID"].ToString());
            }
            if(CLIENT_ID != id) //not your ID, let's take you to yours
            {
                return RedirectToAction("Details", new { id = CLIENT_ID });
            }

            //get the client info
            Client c = dbContext.Clients.Where(x=>x.ClientID == id).FirstOrDefault();
            if(c == null)
            {
                return RedirectToAction("index", "home");
            }
            return View(c);
        }
        
        // GET: User/Edit/5
        public ActionResult Edit(int id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            else
            {
                CLIENT_ID = int.Parse(Session["UserID"].ToString());
            }
            if (CLIENT_ID != id) //not your ID, let's take you to yours
            {
                return RedirectToAction("Details", new { id = CLIENT_ID });
            }
            UserViewModel userView = new UserViewModel();
            //get the client info
            Client c = dbContext.Clients.Where(x => x.ClientID == id).FirstOrDefault();
            if (c == null)
            {
                return RedirectToAction("index", "home");
            }
            userView.client = c;
            String userUploadedImages = Server.MapPath("~/Images/Users/" + CLIENT_ID);
            userView.allImages = new List<string>();
            if (Directory.Exists(userUploadedImages)){
                userView.allImages = Directory.GetFiles(userUploadedImages).Select(Path.GetFileName).ToList();
            }
            return View(userView);
        }

        // POST: User/Edit/5
        [HttpPost]
        public ActionResult Edit(Client client)
        {
            try
            {
                Client c = dbContext.Clients.Where(x => x.ClientID == client.ClientID).FirstOrDefault();
                c.Username = client.Username;
                c.Address = client.Address;
                c.City = client.City;
                c.State = client.State;
                c.PhotoURL = client.PhotoURL;
                Session["img"] = "/Images/Users/" + client.ClientID + "/" + client.PhotoURL; //update the sidebar image
                c.Notify = client.Notify;
                c.Email = client.Email;
                dbContext.Entry(c).State = EntityState.Modified;
                if (ModelState.IsValid)
                {
                    dbContext.SaveChanges();
                    return RedirectToAction("Details", new { id = CLIENT_ID });
                }
                else
                {
                    return View();
                }
            }
           
            catch
            {
                return View();
            }
        }

        [HttpPost]
        public ActionResult UploadPhoto(HttpPostedFileBase photo)
        {
            string userFolder = Request.MapPath("~/Images/Users/" + Session["UserID"]);
            if (!Directory.Exists(userFolder))
            {
                //create the user's folder to store images
                Directory.CreateDirectory(userFolder);
            }
            string path = userFolder + "\\" + photo.FileName;

            if (photo != null)
                
                    photo.SaveAs(path);

            return RedirectToAction("edit", new { id = CLIENT_ID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePhoto(string photoFileName)
        {
            //Session["DeleteSuccess"] = "No";
            var photoName = "";
            photoName = photoFileName;
            string fullPath = Request.MapPath("~/Images/Users/" + CLIENT_ID + "/"
            + photoName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                //Session["DeleteSuccess"] = "Yes";
            }
            return RedirectToAction("edit", new { id = CLIENT_ID });
        }
    }
}
