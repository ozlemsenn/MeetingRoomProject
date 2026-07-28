using System.Web.Mvc;

namespace MeetingProject.Controllers
{
    [Authorize(Roles = "Personel")]
    public class PersonelController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Reservations");
        }
    }
}