using System.Web.Mvc;

namespace MeetingProject.Controllers { 
    public class AuthController : Controller
    {
        public ActionResult Login()
        {
            return View();
        }
    }
}