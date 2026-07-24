using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;

namespace MeetingProject.Controllers
{
    public class HomeController : Controller
    {
        Models.MeetingAppEntities1 db = new Models.MeetingAppEntities1();
        public ActionResult Index()
        {
            var bugun = DateTime.Today;

            ViewBag.ToplamRezervasyon = db.Reservations.Count();

            ViewBag.BugunkuRezervasyon = db.Reservations.Count(x => x.Date == bugun && x.Status != "İptal Edildi");

            ViewBag.ToplamOda = db.Rooms.Count();

            ViewBag.IptalEdilenler = db.Reservations.Count(x => x.Status == "İptal Edildi");

            List<string> grafikGunler = new List<string>();
            List<int> grafikSayilar = new List<int>();

            for (int i = 0; i < 7; i++)
            {
                var aktifTarih = bugun.AddDays(i);

                grafikGunler.Add(aktifTarih.ToString("dd MMM dddd"));

                var gunlukSayi = db.Reservations.Count(x => x.Date == aktifTarih && x.Status != "İptal Edildi");
                grafikSayilar.Add(gunlukSayi);
            }

            ViewBag.GrafikGunler = grafikGunler;
            ViewBag.GrafikSayilar = grafikSayilar;

            var hamListe = db.Reservations
                .Where(x => x.Status != "İptal Edildi" && x.Date >= bugun)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.StartTime)
                .Take(5)
                .ToList();

            var siradakiToplantilar = new List<ToplantiOzet>();
            foreach (var rez in hamListe)
            {
                var oda = db.Rooms.Find(rez.RoomId);
                siradakiToplantilar.Add(new ToplantiOzet
                {
                    OdaAdi = oda != null ? oda.Name : "Oda Bulunamadı",
                    Tarih = rez.Date.HasValue ? rez.Date.Value.ToString("dd MMM yyyy") : "",
                    Saat = rez.StartTime.HasValue ? rez.StartTime.Value.ToString(@"hh\:mm") : ""
                });
            }

            ViewBag.SiradakiToplantilar = siradakiToplantilar;

            var sonIslemler = db.Reservations
                .OrderByDescending(x => x.Id)
                .Take(5)
                .ToList();

            var aktiviteler = new List<AktiviteOzet>();
            foreach (var islem in sonIslemler)
            {
                var oda = db.Rooms.Find(islem.RoomId);
                var user = db.Users.Find(islem.UserId); 

                string odaAd = oda != null ? oda.Name : "Bilinmeyen Oda";
                string kullaniciAd = user != null ? (user.Name + " " + user.Surname) : "Bilinmeyen Kullanıcı";

                bool iptalMi = islem.Status == "İptal Edildi";

                aktiviteler.Add(new AktiviteOzet
                {
                    Aciklama = iptalMi
                        ? $"{kullaniciAd}, {odaAd} için yaptığı rezervasyonu iptal etti."
                        : $"{kullaniciAd}, {odaAd} için yeni bir rezervasyon oluşturdu.",
                    TarihSaat = islem.Date.HasValue ? islem.Date.Value.ToString("dd MMM yyyy") : "",
                    RenkSinifi = iptalMi ? "danger" : "success",
                    Ikon = iptalMi ? "fa-times-circle" : "fa-check-circle"
                });
            }

            ViewBag.Aktiviteler = aktiviteler;


            var bugunkuListe = db.Reservations
                .Where(x => x.Date == bugun && x.Status != "İptal Edildi")
                .OrderBy(x => x.StartTime)
                .ToList();

            var bugunkuToplantilar = new List<ToplantiOzet>();
            foreach (var rez in bugunkuListe)
            {
                var oda = db.Rooms.Find(rez.RoomId);
                bugunkuToplantilar.Add(new ToplantiOzet
                {
                    OdaAdi = oda != null ? oda.Name : "Bilinmeyen Oda",
                    Saat = rez.StartTime.HasValue ? rez.StartTime.Value.ToString(@"hh\:mm") : "",
                    Tarih = "Bugün"

                });
            }
            ViewBag.BugunkuToplantilar = bugunkuToplantilar;
            return View();

        }
        public ActionResult ExcelIndir()
        {
            var tumRezervasyonlar = db.Reservations.OrderByDescending(x => x.Date).ToList();

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Oda Adı;Tarih;Başlangıç Saati;Bitiş Saati;Durum");

            foreach (var item in tumRezervasyonlar)
            {
                var oda = db.Rooms.Find(item.RoomId);
                string odaAdi = oda != null ? oda.Name : "Bilinmeyen Oda";
                string tarih = item.Date.HasValue ? item.Date.Value.ToString("dd.MM.yyyy") : "-";
                string bas = item.StartTime.HasValue ? item.StartTime.Value.ToString(@"hh\:mm") : "-";
                string bit = item.EndTime.HasValue ? item.EndTime.Value.ToString(@"hh\:mm") : "-";
                string durum = item.Status ?? "-";

                sb.AppendLine($"{odaAdi};{tarih};{bas};{bit};{durum}");
            }

            byte[] excelBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

            string dosyaAdi = "Rezervasyon_Raporu_" + DateTime.Now.ToString("ddMMyyyy") + ".csv";
            return File(excelBytes, "text/csv", dosyaAdi);
        }

        public JsonResult GetCalendarEvents()
        {

            var reservations = db.Reservations.ToList();


            var events = new List<object>();

            foreach (var rez in reservations)
            {
                var oda = db.Rooms.Find(rez.RoomId);
                var user = db.Users.Find(rez.UserId);

                string odaAdi = oda != null ? oda.Name : "Bilinmeyen Oda";
                string kisiAdi = user != null ? (user.Name + " " + user.Surname) : "Bilinmeyen Kullanıcı";
                string durum = rez.Status ?? "Bekliyor";


                string renk = "#10b981"; 

                if (durum == "İptal Edildi")
                    renk = "#ef4444";

                else if (durum == "Bekliyor")
                    renk = "#f59e0b"; 



                string startDateTime = "";
                if (rez.Date.HasValue && rez.StartTime.HasValue)
                {
                    startDateTime = rez.Date.Value.ToString("yyyy-MM-dd") + "T" + rez.StartTime.Value.ToString(@"hh\:mm\:ss");
                }


                events.Add(new
                {
                    title = odaAdi,
                    start = startDateTime,
                    color = renk,
                    extendedProps = new
                    {
                        kisi = kisiAdi,
                        durum = durum
                    }
                });
            }


            return Json(events, JsonRequestBehavior.AllowGet);
        }
        public class ToplantiOzet
        {
            public string OdaAdi { get; set; }
            public string Tarih { get; set; }
            public string Saat { get; set; }
        }

        public class AktiviteOzet
        {
            public string Aciklama { get; set; }
            public string TarihSaat { get; set; }
            public string RenkSinifi { get; set; }
            public string Ikon { get; set; }
        }
    }
}