using System;
using System.Linq;
using System.Web.Mvc;

namespace MeetingProject.Controllers
{
    [Authorize] 
    public class PersonelController : BaseController
    {
        public ActionResult Index()
        {
            var kullanici = GecerliKullanici();

            if (kullanici == null)
                return RedirectToAction("Login", "Account"); 

            var bugun = DateTime.Today;

            var benimRezervasyonlarim = db.Reservations
                .Where(x => x.UserId == kullanici.Id)
                .OrderByDescending(x => x.Date)
                .ToList();

            ViewBag.ToplamRezervasyonum = benimRezervasyonlarim.Count;
            ViewBag.BugunkuRezervasyonlarim = benimRezervasyonlarim.Count(x => x.Date == bugun && x.Status != "İptal Edildi");

            ViewBag.YaklasanRezervasyonlarim = benimRezervasyonlarim
                .Where(x => x.Date >= bugun && x.Status != "İptal Edildi")
                .OrderBy(x => x.Date)
                .Take(5)
                .ToList();

            return View();
        }
    }
}