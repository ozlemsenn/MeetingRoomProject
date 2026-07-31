using System;
using System.Linq;
using System.Web.Mvc;

namespace MeetingProject.Controllers
{
    [Authorize] // sadece giriş yapmış kullanıcılar girebilir
    public class PersonelController : BaseController
    {
        public ActionResult Index()
        {
            var kullanici = GecerliKullanici();

            if (kullanici == null)
                return RedirectToAction("Login", "Account"); // senin login action'ının gerçek adı neyse

            var bugun = DateTime.Today;

            // Sadece KENDİ oluşturduğu rezervasyonları çekiyoruz
            var benimRezervasyonlarim = db.Reservations
                .Where(x => x.UserId == kullanici.Id)
                .OrderByDescending(x => x.Date)
                .ToList();

            ViewBag.ToplamRezervasyonum = benimRezervasyonlarim.Count;
            ViewBag.BugunkuRezervasyonlarim = benimRezervasyonlarim.Count(x => x.Date == bugun && x.Status != "İptal Edildi");

            // Yaklaşan rezervasyonlarım (bugünden itibaren, iptal olmayanlar, en yakın 5 tanesi)
            ViewBag.YaklasanRezervasyonlarim = benimRezervasyonlarim
                .Where(x => x.Date >= bugun && x.Status != "İptal Edildi")
                .OrderBy(x => x.Date)
                .Take(5)
                .ToList();

            return View();
        }
    }
}