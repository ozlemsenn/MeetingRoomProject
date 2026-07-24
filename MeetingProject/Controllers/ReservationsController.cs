using System;
using System.Linq;
using System.Web.Mvc;
using MeetingProject.Models;
using Microsoft.Ajax.Utilities;

namespace MeetingProject.Controllers
{
    public class ReservationsController : Controller
    {
        MeetingAppEntities1 db = new MeetingAppEntities1();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetReservations()
        {
            var reservations = db.Reservations.Select(r => new
            {
                r.Id,
                RoomName = db.Rooms.FirstOrDefault(room => room.Id == r.RoomId).Name,
                UserName = db.Users.FirstOrDefault(user => user.Id == r.UserId).Name + " " + db.Users.FirstOrDefault(user => user.Id == r.UserId).Surname,
                Date = r.Date,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Description = r.Description,
                Status = r.Status
            }).ToList();

            var formattedList = reservations.Select(r => {

                string guncelDurum = r.Status; 

                if (guncelDurum != "İptal Edildi" && r.Date.HasValue && r.EndTime.HasValue)
                {
                    DateTime toplantininBitisZamani = r.Date.Value.Add(r.EndTime.Value);
                    if (toplantininBitisZamani < DateTime.Now)
                    {
                        guncelDurum = "Tamamlandı";
                    }
                }

                return new
                {
                    r.Id,
                    r.RoomName,
                    r.UserName,
                    Date = r.Date.HasValue ? r.Date.Value.ToString("dd.MM.yyyy") : "",
                    StartTime = r.StartTime.HasValue ? r.StartTime.Value.ToString(@"hh\:mm") : "",
                    EndTime = r.EndTime.HasValue ? r.EndTime.Value.ToString(@"hh\:mm") : "",
                    r.Description,
                    Status = guncelDurum
                };
            });

            return Json(formattedList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Create()
        {
            ViewBag.Rooms = new SelectList(db.Rooms, "Id", "Name");
            ViewBag.Users = new SelectList(db.Users, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Reservations res)
        {
            if (!res.RoomId.HasValue || res.RoomId == 0)
            {
                return Json(new { success = false, message = "Lütfen bir toplantı odası seçiniz." });
            }

            if (!res.UserId.HasValue || res.UserId == 0)
            {
                return Json(new { success = false, message = "Lütfen rezervasyonu yapacak kullanıcıyı seçiniz." });
            }

            if (!res.Date.HasValue)
            {
                return Json(new { success = false, message = "Lütfen rezervasyon tarihini seçiniz." });
            }

            if (!res.StartTime.HasValue || !res.EndTime.HasValue)
            {
                return Json(new { success = false, message = "Lütfen başlangıç ve bitiş saatlerini eksiksiz giriniz." });
            }

            if (res.StartTime >= res.EndTime)
            {
                return Json(new { success = false, message = "Bitiş saati, başlangıç saatinden önce veya aynı olamaz." });
            }

            if (string.IsNullOrWhiteSpace(res.Description))
            {
                return Json(new { success = false, message = "Lütfen toplantı için bir açıklama (konu) giriniz." });
            }


            bool isOverlap = db.Reservations.Any(r =>
                r.Status != "İptal Edildi" &&
                r.RoomId == res.RoomId &&
                r.Date == res.Date &&
                (
                    (res.StartTime >= r.StartTime && res.StartTime < r.EndTime) ||
                    (res.EndTime > r.StartTime && res.EndTime <= r.EndTime) ||
                    (res.StartTime <= r.StartTime && res.EndTime >= r.EndTime)
                )
            );

            if (isOverlap)
            {
                return Json(new { success = false, message = "Seçtiğiniz saat aralığında bu oda doludur. Lütfen farklı bir saat veya oda seçiniz." });
            }


            res.Status = "Aktif";
            db.Reservations.Add(res);

            try
            {
                db.SaveChanges();
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "Veritabanına kaydedilirken bir hata oluştu." });
            }

            return Json(new { success = true, message = "Rezervasyon başarıyla oluşturuldu!" });
        }
        [HttpGet]
        public ActionResult GetReservationsByDate(DateTime date, int? excludeId = null)
        {
            var tumRezervasyonlar = db.Reservations
                .Where(x => x.Date == date
                            && x.Status != "İptal Edildi"
                            && (!excludeId.HasValue || x.Id != excludeId.Value))
                .ToList();

            var sonuc = tumRezervasyonlar.Select(x => new {
                RoomName = db.Rooms.Find(x.RoomId) != null ? db.Rooms.Find(x.RoomId).Name : "Oda Adı Yok",
                StartTime = x.StartTime.HasValue ? x.StartTime.Value.ToString(@"hh\:mm") : "",
                EndTime = x.EndTime.HasValue ? x.EndTime.Value.ToString(@"hh\:mm") : ""
            }).ToList();

            return Json(sonuc, JsonRequestBehavior.AllowGet);
        }
        
        public ActionResult Delete(int id)
        {
            var res = db.Reservations.Find(id);
            if (res == null) return HttpNotFound();

            var oda = db.Rooms.Find(res.RoomId);
            var user = db.Users.Find(res.UserId);

            ViewBag.OdaAdi = oda != null ? oda.Name : "Oda Bilgisi Bulunamadı";
            ViewBag.KullaniciAdi = user != null ? user.Name + " " + user.Surname : "Kullanıcı Bulunamadı";

            return PartialView(res);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirm(int id)
        {
            var res = db.Reservations.Find(id);
            if (res != null)
            {
                db.Reservations.Remove(res);
                db.SaveChanges();
            }
            return Json(new { success = true });
        }

        public ActionResult Edit(int id)
        {
            var res = db.Reservations.Find(id);
            if (res == null)
            {
                return HttpNotFound();
            }

            ViewBag.Rooms = new SelectList(db.Rooms, "Id", "Name", res.RoomId);
            ViewBag.Users = new SelectList(db.Users, "Id", "Name", res.UserId);

            return View(res);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Reservations res)
        {
            if (!res.RoomId.HasValue || res.RoomId == 0) return Json(new { success = false, message = "Lütfen bir toplantı odası seçiniz." });
            if (!res.UserId.HasValue || res.UserId == 0) return Json(new { success = false, message = "Lütfen rezervasyonu yapacak kullanıcıyı seçiniz." });
            if (!res.Date.HasValue) return Json(new { success = false, message = "Lütfen rezervasyon tarihini seçiniz." });
            if (!res.StartTime.HasValue || !res.EndTime.HasValue) return Json(new { success = false, message = "Lütfen başlangıç ve bitiş saatlerini eksiksiz giriniz." });
            if (res.StartTime >= res.EndTime) return Json(new { success = false, message = "Bitiş saati, başlangıç saatinden önce veya aynı olamaz." });
            if (string.IsNullOrWhiteSpace(res.Description)) return Json(new { success = false, message = "Lütfen toplantı için bir açıklama (konu) giriniz." });

            bool isOverlap = db.Reservations.Any(r =>
                r.Id != res.Id &&
                r.Status != "İptal Edildi" &&
                r.RoomId == res.RoomId &&
                r.Date == res.Date &&
                (
                    (res.StartTime >= r.StartTime && res.StartTime < r.EndTime) ||
                    (res.EndTime > r.StartTime && res.EndTime <= r.EndTime) ||
                    (res.StartTime <= r.StartTime && res.EndTime >= r.EndTime)
                )
            );

            if (isOverlap)
            {
                return Json(new { success = false, message = "Seçtiğiniz saat aralığında bu oda doludur." });
            }

            var existingRes = db.Reservations.Find(res.Id);
            if (existingRes != null)
            {
                existingRes.RoomId = res.RoomId;
                existingRes.UserId = res.UserId;
                existingRes.Date = res.Date;
                existingRes.StartTime = res.StartTime;
                existingRes.EndTime = res.EndTime;
                existingRes.Description = res.Description;

                db.SaveChanges();
            }

            return Json(new { success = true, message = "Rezervasyon başarıyla güncellendi!" });
        }

        public ActionResult Cancel(int id)
        {
            var res = db.Reservations.Find(id);
            if (res == null)
            {
                return HttpNotFound();
            }
            var oda = db.Rooms.Find(res.RoomId);
            ViewBag.OdaAdi = oda != null ? oda.Name : "Oda Bilgisi Bulunamadı";

            return PartialView(res);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelConfirm(int id, string CancelReason)
        {
            var res = db.Reservations.Find(id);
            if (res != null)
            {
                res.Status = "İptal Edildi";
                res.CancelReason = CancelReason;

                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }
        public ActionResult Details(int id)
        {
            var res = db.Reservations.Find(id);
            if (res == null)
            {
                return HttpNotFound();
            }

            ViewBag.RoomName = db.Rooms.FirstOrDefault(r => r.Id == res.RoomId)?.Name;

            var user = db.Users.FirstOrDefault(u => u.Id == res.UserId);
            ViewBag.UserName = user != null ? user.Name + " " + user.Surname : "";

            string guncelDurum = res.Status;

            if (guncelDurum != "İptal Edildi" && res.Date.HasValue && res.EndTime.HasValue)
            {
                DateTime toplantininBitisZamani = res.Date.Value.Add(res.EndTime.Value);
                if (toplantininBitisZamani < DateTime.Now)
                {
                    guncelDurum = "Tamamlandı";
                }
            }

            ViewBag.GuncelDurum = guncelDurum;

            return View(res);
        }
    }
}