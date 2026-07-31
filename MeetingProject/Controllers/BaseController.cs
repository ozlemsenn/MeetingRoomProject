using System;
using System.Web.Mvc;

namespace MeetingProject.Controllers
{
    public class BaseController : Controller
    {
        protected Models.MeetingAppEntities1 db = new Models.MeetingAppEntities1();

        protected Models.Users GecerliKullanici()
        {
            if (Session["UserId"] == null)
                return null;

            int userId = (int)Session["UserId"];
            return db.Users.Find(userId);
        }

        protected string GecerliRol()
        {
            return Session["UserRole"] != null ? Session["UserRole"].ToString() : "";
        }

        protected int GecerliKullaniciId()
        {
            return Session["UserId"] != null ? Convert.ToInt32(Session["UserId"]) : 0;
        }

        protected int GecerliSirketId()
        {
            return Session["CompanyId"] != null ? Convert.ToInt32(Session["CompanyId"]) : 0;
        }
    }
}