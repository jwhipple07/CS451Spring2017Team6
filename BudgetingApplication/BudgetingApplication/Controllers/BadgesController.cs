using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BudgetingApplication.ViewModels;
using BudgetingApplication.Models;
using System.Data.Entity;

namespace BudgetingApplication.Controllers
{
    public class BadgesController : Controller
    {
        private DataContext dbContext = new DataContext();
        private static int CLIENT_ID;

        // GET: Badges
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
            
            //get badges the user has earned
            BadgesModelView badgeModel = new BadgesModelView();
            badgeModel.badges = getUserBadges();
            badgeModel.tweetMessage = "! Come budget with Commerce today at commercebank.com";

            badgeTrigger(badgeModel);

            badgeModel.badgeCount = badgeModel.badges.Count();
            badgeModel.totalBadgeCount = dbContext.Badges.Count();

            //get most recent badge
            ClientBadge recent = dbContext.ClientBadges.Where(x => x.ClientID == CLIENT_ID).OrderByDescending(t => t.DateEarned).FirstOrDefault();
            badgeModel.mostRecent = dbContext.Badges.Where(x => x.BadgeID == recent.BadgeID).FirstOrDefault();
            
            return View(badgeModel);
        }

        private List<Badge> getUserBadges()
        {
            List<ClientBadge> ClientBadgeList = new List<ClientBadge>();
            ClientBadgeList = dbContext.ClientBadges.Where(x => x.ClientID == CLIENT_ID).ToList();
            List<Badge> TotalBadges = dbContext.Badges.ToList();
            List<Badge> BadgesEarned = new List<Badge>();

            for (int i = 0; i < ClientBadgeList.Count(); i++)
            {
                for (int j = 0; j < TotalBadges.Count(); j++)
                {
                    //find every badge the client has by their ID
                    if (ClientBadgeList[i].BadgeID == TotalBadges[j].BadgeID)
                    {
                        BadgesEarned.Add(TotalBadges[j]);
                    }
                }
            }


            return BadgesEarned;
        }

        private void badgeTrigger(BadgesModelView bmv)
        {
            //check if user does not have any badges tied to the badge page
            //bool array for achievement of each badge
            bool[] badgeObtained = new bool[9];
            int holidayCount = 0;

            List<ClientBadge> ClientBadgeList = new List<ClientBadge>();
            ClientBadgeList = dbContext.ClientBadges.Where(x => x.ClientID == CLIENT_ID).ToList();

            for(int i = 0; i < ClientBadgeList.Count(); i++)
            {
                //count number of holiday badges
                if(ClientBadgeList[i].BadgeID >= 109 && ClientBadgeList[i].BadgeID <= 121)
                {
                    holidayCount++;
                }

                //check if any badges related to the badge page are already acquired
                switch (ClientBadgeList[i].BadgeID)
                {
                    case (99):
                        badgeObtained[0] = true;
                        break;
                    case (100):
                        badgeObtained[1] = true;
                        break;
                    case (102):
                        badgeObtained[2] = true;
                        break;
                    case (103):
                        badgeObtained[3] = true;
                        break;
                    case (104):
                        badgeObtained[4] = true;
                        break;
                    case (107):
                        badgeObtained[5] = true;
                        break;
                    case (122):
                        badgeObtained[6] = true;
                        break;
                    case (123):
                        badgeObtained[7] = true;
                        break;
                    case (108):
                        badgeObtained[8] = true;
                        break;
                }
            }

            for(int j = 0; j < badgeObtained.Count(); j++)
            {
                if (!badgeObtained[j])
                {
                    switch (j)
                    {
                        case (0):
                            //check if user has shared one badge
                            break;
                        case (1):
                            //... shared 5 badges
                            break;
                        case (2):
                            //Achieved 5 badges
                            if(ClientBadgeList.Count() == 5)
                            {
                                bmv.addNewBadge(102, CLIENT_ID);
                            }
                            break;
                        case (3):
                            //10 badges
                            if (ClientBadgeList.Count() == 10)
                            {
                                bmv.addNewBadge(103, CLIENT_ID);
                            }
                            break;
                        case (4):
                            //20 badges
                            if (ClientBadgeList.Count() == 20)
                            {
                                bmv.addNewBadge(104, CLIENT_ID);
                            }
                            break;
                        case (5):
                            //auto since user has to click on badge page
                            bmv.addNewBadge(107, CLIENT_ID);
                            break;
                        case (6):
                            //check if all holiday badges are cleared
                            if(holidayCount.Equals(13))
                            {
                                bmv.addNewBadge(122, CLIENT_ID);
                            }
                            break;
                        case (7):
                            //check if 44 badges have been obtained
                            if (ClientBadgeList.Count() == 44)
                            {
                                bmv.addNewBadge(123, CLIENT_ID);
                            }
                            break;
                        case (8):
                            //check if 106, 107, and 75 have been obtained
                            int count = 0;

                            for (int k = 0; k < ClientBadgeList.Count(); k++)
                            {
                                switch (ClientBadgeList[k].BadgeID)
                                {
                                    case (106):
                                        count++;
                                        break;
                                    case (107):
                                        count++;
                                        break;
                                    case (75):
                                        count++;
                                        break;
                                }
                            }

                            if(count == 3)
                            {
                                bmv.addNewBadge(108, CLIENT_ID);
                            }
                            break;
                    }
                }
            }
        }
    }
}
