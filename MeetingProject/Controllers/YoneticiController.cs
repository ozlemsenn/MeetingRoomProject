using System.Web.Mvc;

namespace MeetingProject.Controllers
{
    [Authorize(Roles = "Yonetici")]
    public class YoneticiController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}