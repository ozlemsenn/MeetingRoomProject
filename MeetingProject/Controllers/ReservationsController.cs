using System;
using System.Linq;
using System.Web.Mvc;
using MeetingProject.Models;
using Microsoft.Ajax.Utilities;
using System.Data.Entity;
using System.Collections.Generic;

namespace MeetingProject.Controllers
{
    [Authorize] //sonradan ekledim
    public class ReservationsController : BaseController
    {

        public ActionResult Index()
        {
            if (string.IsNullOrEmpty(GecerliRol()))
            {
                return RedirectToAction("Login", "Account");
            }

            bool isAdmin = GecerliRol() == "Admin";
            bool isPersonel = GecerliRol() == "Personel";
            int aktifSirketId = GecerliSirketId();
            int aktifKullaniciId = GecerliKullaniciId();

            if (isAdmin)
            {
                ViewBag.Companies = new SelectList(db.Companies.ToList(), "Id", "Name");
            }

            var query = db.Reservations.AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(x => x.CompanyId == aktifSirketId);
            }

            if (isPersonel)
            {
                query = query.Where(x => x.UserId == aktifKullaniciId || (x.Attendees != null && x.Attendees != ""));
            }

            var rezervasyonlar = query.ToList();

            if (isPersonel)
            {
                rezervasyonlar = rezervasyonlar
                    .Where(x => x.UserId == aktifKullaniciId
                             || (!string.IsNullOrEmpty(x.Attendees)
                                 && x.Attendees.Split(',').Select(a => a.Trim()).Contains(aktifKullaniciId.ToString())))
                    .ToList();
            }

            return View(rezervasyonlar);
        }

        [HttpGet]
        public ActionResult GetReservations()
        {
            if (string.IsNullOrEmpty(GecerliRol()))
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            bool isAdmin = GecerliRol() == "Admin";
            bool isPersonel = GecerliRol() == "Personel";
            int aktifSirketId = GecerliSirketId();
            int aktifKullaniciId = GecerliKullaniciId();

            var query = db.Reservations.AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(r => r.CompanyId == aktifSirketId);
            }

            if (isPersonel)
            {
                query = query.Where(r => r.UserId == aktifKullaniciId || (r.Attendees != null && r.Attendees != ""));
            }

            var reservations = query.Select(r => new
            {
                r.Id,
                r.CompanyId,
                r.UserId,
                r.Attendees,
                RoomName = db.Rooms.FirstOrDefault(room => room.Id == r.RoomId).Name,
                UserName = db.Users.FirstOrDefault(user => user.Id == r.UserId).Name + " " + db.Users.FirstOrDefault(user => user.Id == r.UserId).Surname,
                Title = r.Title,
                Date = r.Date,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Description = r.Description,
                Status = r.Status
            }).ToList();

            if (isPersonel)
            {
                reservations = reservations
                    .Where(r => r.UserId == aktifKullaniciId
                             || (!string.IsNullOrEmpty(r.Attendees)
                                 && r.Attendees.Split(',').Select(a => a.Trim()).Contains(aktifKullaniciId.ToString())))
                    .ToList();
            }

            var formattedList = reservations.Select(r => {

                string gercekDurum = "Planlandı";

                if (r.Status != "İptal Edildi" && r.Status != "Bekliyor")
                {
                    DateTime rezTarihi = r.Date ?? DateTime.Today;

                    TimeSpan baslangicSaati = r.StartTime ?? TimeSpan.Zero;
                    TimeSpan bitisSaati = r.EndTime ?? TimeSpan.Zero;

                    DateTime baslangic = rezTarihi.Add(baslangicSaati);
                    DateTime bitis = rezTarihi.Add(bitisSaati);

                    if (DateTime.Now >= baslangic && DateTime.Now <= bitis)
                    {
                        gercekDurum = "Devam Ediyor";
                    }
                    else if (DateTime.Now > bitis)
                    {
                        gercekDurum = "Tamamlandı";
                    }
                }
                else
                {
                    gercekDurum = r.Status;
                }

                return new
                {
                    r.Id,
                    r.CompanyId,
                    r.Title,
                    r.RoomName,
                    r.UserName,
                    Date = r.Date.HasValue ? r.Date.Value.ToString("dd.MM.yyyy") : "",
                    StartTime = r.StartTime.HasValue ? r.StartTime.Value.ToString(@"hh\:mm") : "",
                    EndTime = r.EndTime.HasValue ? r.EndTime.Value.ToString(@"hh\:mm") : "",
                    r.Description,
                    Status = gercekDurum
                };
            });

            return Json(formattedList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult SirketeGoreVerileriGetir(int sirketId)
        {
            var odalar = db.Rooms
                           .Where(r => r.CompanyId == sirketId)
                           .Select(r => new { Id = r.Id, Name = r.Name })
                           .ToList();

            var personeller = db.Users
                                .Where(u => u.CompanyId == sirketId)
                                .Select(u => new { Id = u.Id, TamAd = u.Name + " " + u.Surname })
                                .ToList();

            var adminler = db.Users
                             .Where(u => u.Role == "Admin") 
                             .Select(u => new { Id = u.Id, TamAd = u.Name + " " + u.Surname + " (Admin)" })
                             .ToList();

            var kullanicilar = personeller.ToList();

            foreach (var admin in adminler)
            {
                if (!kullanicilar.Any(k => k.Id == admin.Id))
                {
                    kullanicilar.Add(admin);
                }
            }

            kullanicilar = kullanicilar.OrderBy(u => u.TamAd).ToList();

            return Json(new { odalar = odalar, kullanicilar = kullanicilar }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Create()
        {
            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();
            int aktifKullaniciId = GecerliKullaniciId();

            ViewBag.IsAdmin = isAdmin;

            if (isAdmin)
            {
                ViewBag.Companies = new SelectList(db.Companies.ToList(), "Id", "Name");
                ViewBag.Rooms = new SelectList(new List<object>(), "Id", "Name");
                ViewBag.Users = new SelectList(new List<object>(), "Id", "TamAd");
                ViewBag.KullaniciListesi = new SelectList(new List<object>(), "Id", "TamAd");
            }
            else
            {
                var sirketOdalar = db.Rooms.Where(r => r.CompanyId == aktifSirketId).ToList();
                ViewBag.Rooms = new SelectList(sirketOdalar, "Id", "Name");

                var sirketKullanicilar = db.Users
                                           .Where(u => u.CompanyId == aktifSirketId)
                                           .Select(u => new { Id = u.Id, TamAd = u.Name + " " + u.Surname })
                                           .ToList();

                var adminler = db.Users
                                 .Where(u => u.Role == "Admin")
                                 .Select(u => new { Id = u.Id, TamAd = u.Name + " " + u.Surname + " (Admin)" })
                                 .ToList();

                var birlesikKullanicilar = sirketKullanicilar.Concat(adminler).OrderBy(u => u.TamAd).ToList();

                var sadeceBen = birlesikKullanicilar.Where(u => u.Id == aktifKullaniciId).ToList();
                ViewBag.Users = new SelectList(sadeceBen, "Id", "TamAd");

                ViewBag.KullaniciListesi = new SelectList(birlesikKullanicilar, "Id", "TamAd");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Reservations res, string[] SecilenKatilimcilar)
        {
            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();
            int aktifKullaniciId = GecerliKullaniciId();

            if (isAdmin)
            {
                res.CompanyId = Convert.ToInt32(Request.Form["CompanyId"]);
            }
            else
            {
                res.CompanyId = aktifSirketId;

                res.UserId = aktifKullaniciId;
            }

            var secilenOda = db.Rooms.FirstOrDefault(r => r.Id == res.RoomId);

            if (secilenOda == null || (!isAdmin && secilenOda.CompanyId != aktifSirketId))
            {
                return Json(new { success = false, message = "Bu odayı seçme yetkiniz yok!" });
            }

            if (SecilenKatilimcilar == null || SecilenKatilimcilar.Length == 0)
            {
                return Json(new { success = false, message = "En az bir katılımcı seçilmelidir." });
            }
            res.Attendees = string.Join(", ", SecilenKatilimcilar);

            if (!res.RoomId.HasValue || res.RoomId == 0)
                return Json(new { success = false, message = "Lütfen bir toplantı odası seçiniz." });

            if (!res.UserId.HasValue || res.UserId == 0)
                return Json(new { success = false, message = "Lütfen rezervasyonu yapacak kullanıcıyı seçiniz." });

            if (string.IsNullOrWhiteSpace(res.Title))
                return Json(new { success = false, message = "Lütfen toplantı başlığı giriniz." });

            if (!res.Date.HasValue)
                return Json(new { success = false, message = "Lütfen rezervasyon tarihini seçiniz." });

            if (!res.StartTime.HasValue || !res.EndTime.HasValue)
                return Json(new { success = false, message = "Lütfen başlangıç ve bitiş saatlerini eksiksiz giriniz." });

            if (res.StartTime >= res.EndTime)
                return Json(new { success = false, message = "Bitiş saati, başlangıç saatinden önce veya aynı olamaz." });

            if (string.IsNullOrWhiteSpace(res.Description))
                return Json(new { success = false, message = "Lütfen toplantı için bir açıklama giriniz." });

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
                return Json(new { success = false, message = "Bu saat aralığında odada başka bir toplantı var!" });
            }

            res.TransactionDate = DateTime.Now;
            res.TransactionTime = DateTime.Now.TimeOfDay;
            db.Reservations.Add(res);
            db.SaveChanges();

            return Json(new { success = true });
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

        [HttpGet]
        public ActionResult Delete(int id)
        {
            var res = db.Reservations.Find(id);
            if (res == null) return HttpNotFound();

            bool isAdmin = GecerliRol() == "Admin";

            if (!isAdmin)
            {
                return Content("<div class='alert alert-danger text-center m-4' style='border-radius:10px;'><i class='fas fa-shield-alt fa-3x mb-3 text-danger'></i><br><b class='d-block fs-5 mb-2'>Yetkisiz İşlem</b> Sadece sistem yöneticileri (Admin) kalıcı silme işlemi yapabilir. Rezervasyonu kaldırmak için lütfen 'İptal Et' (Yasak sembolü) butonunu kullanınız.</div>");
            }

            var oda = db.Rooms.Find(res.RoomId);
            var user = db.Users.Find(res.UserId);

            ViewBag.OdaAdi = oda != null ? oda.Name : "Oda Bilgisi Bulunamadı";
            ViewBag.KullaniciAdi = user != null ? user.Name + " " + user.Surname : "Kullanıcı Bulunamadı";

            return PartialView(res);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirm(int id)
        {
            bool isAdmin = GecerliRol() == "Admin";

            if (!isAdmin)
            {
                return Json(new { success = false, message = "Sadece sistem yöneticileri kalıcı silme işlemi yapabilir!" });
            }

            var rezervasyon = db.Reservations.Find(id);

            if (rezervasyon == null)
            {
                return Json(new { success = false, message = "Kayıt bulunamadı!" });
            }

            db.Reservations.Remove(rezervasyon);
            db.SaveChanges();

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            bool isAdmin = GecerliRol() == "Admin";
            bool isPersonel = GecerliRol() == "Personel";
            int aktifSirketId = GecerliSirketId();
            int aktifKullaniciId = GecerliKullaniciId();

            var rezervasyon = db.Reservations.Find(id);

            if (rezervasyon == null || (!isAdmin && rezervasyon.CompanyId != aktifSirketId))
            {
                return HttpNotFound("Bu rezervasyonu düzenleme yetkiniz yok veya kayıt bulunamadı.");
            }

            if (isPersonel && rezervasyon.UserId != aktifKullaniciId)
            {
                return HttpNotFound("Sadece kendi oluşturduğunuz rezervasyonu düzenleyebilirsiniz.");
            }

            var sirketOdalar = db.Rooms.Where(r => isAdmin || r.CompanyId == aktifSirketId).ToList();
            ViewBag.Rooms = new SelectList(sirketOdalar, "Id", "Name", rezervasyon.RoomId);

            var sirketKullanicilar = db.Users
                .Where(u => isAdmin || u.CompanyId == aktifSirketId)
                .Select(u => new { Value = u.Id.ToString(), Text = u.Name + " " + u.Surname })
                .ToList();

            if (isPersonel)
            {
                var sadeceBen = sirketKullanicilar.Where(u => u.Value == aktifKullaniciId.ToString()).ToList();
                ViewBag.Users = new SelectList(sadeceBen, "Value", "Text", rezervasyon.UserId);
            }
            else
            {
                ViewBag.Users = new SelectList(sirketKullanicilar, "Value", "Text", rezervasyon.UserId);
            }

            ViewBag.KullaniciListesi = new SelectList(sirketKullanicilar, "Value", "Text");

            return View(rezervasyon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Reservations res, string[] SecilenKatilimcilar)
        {
            try
            {
                int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);
                int aktifKullaniciId = Convert.ToInt32(Session["UserId"]);
                bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
                bool isPersonel = Session["UserRole"] != null && Session["UserRole"].ToString() == "Personel";

                var mevcutRezervasyon = db.Reservations.Find(res.Id);

                if (mevcutRezervasyon == null || (!isAdmin && mevcutRezervasyon.CompanyId != aktifSirketId))
                {
                    return Json(new { success = false, message = "Bu rezervasyona müdahale etme yetkiniz yok!" });
                }

                if (isPersonel && mevcutRezervasyon.UserId != aktifKullaniciId)
                {
                    return Json(new { success = false, message = "Sadece kendi oluşturduğunuz rezervasyonu düzenleyebilirsiniz!" });
                }
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
                    return Json(new { success = false, message = "Bu saat aralığında odada başka bir toplantı var!" });
                }

                mevcutRezervasyon.Title = res.Title;
                mevcutRezervasyon.RoomId = res.RoomId;
                mevcutRezervasyon.UserId = res.UserId; 
                mevcutRezervasyon.Date = res.Date;
                mevcutRezervasyon.StartTime = res.StartTime;
                mevcutRezervasyon.EndTime = res.EndTime;
                mevcutRezervasyon.Description = res.Description;

                if (SecilenKatilimcilar != null && SecilenKatilimcilar.Length > 0)
                {
                    mevcutRezervasyon.Attendees = string.Join(", ", SecilenKatilimcilar);
                }
                else
                {
                    mevcutRezervasyon.Attendees = null;
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Rezervasyon başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                string hataMesaji = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Kayıt sırasında bir hata oluştu: " + hataMesaji });
            }
        }


        public ActionResult Cancel(int id)
        {
            var res = db.Reservations.Find(id);
            if (res == null)
            {
                return HttpNotFound();
            }

            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
            bool isPersonel = Session["UserRole"] != null && Session["UserRole"].ToString() == "Personel";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);
            int aktifKullaniciId = Convert.ToInt32(Session["UserId"]);

            if (!isAdmin && res.CompanyId != aktifSirketId)
            {
                return HttpNotFound("Bu kaydı iptal etme yetkiniz yok.");
            }
            if (isPersonel && res.UserId != aktifKullaniciId)
            {
                return HttpNotFound("Sadece kendi rezervasyonunuzu iptal edebilirsiniz.");
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

            bool isAdmin = GecerliRol() == "Admin";
            bool isPersonel = GecerliRol() == "Personel";
            int aktifSirketId = GecerliSirketId();
            int aktifKullaniciId = GecerliKullaniciId();

            if (res == null || (!isAdmin && res.CompanyId != aktifSirketId))
            {
                return Json(new { success = false, message = "Yetkisiz işlem!" });
            }
            if (isPersonel && res.UserId != aktifKullaniciId)
            {
                return Json(new { success = false, message = "Sadece kendi rezervasyonunuzu iptal edebilirsiniz!" });
            }

            if (res != null)
            {
                res.Status = "İptal Edildi";
                res.CancelReason = CancelReason;

                res.TransactionDate = DateTime.Now;
                res.TransactionTime = DateTime.Now.TimeOfDay;


                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }

        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);
            int aktifKullaniciId = Convert.ToInt32(Session["UserId"]);
            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
            bool isPersonel = Session["UserRole"] != null && Session["UserRole"].ToString() == "Personel";

            var rezervasyon = db.Reservations.Find(id);
            if (rezervasyon == null || (!isAdmin && rezervasyon.CompanyId != aktifSirketId))
            {
                return HttpNotFound("Kayıt bulunamadı veya yetkiniz yok.");
            }

            if (isPersonel && rezervasyon.UserId != aktifKullaniciId)
            {
                bool katilimciMi = false;

                if (!string.IsNullOrEmpty(rezervasyon.Attendees))
                {
                    var katilimciIdListesi = rezervasyon.Attendees
                        .Split(',')
                        .Select(x => x.Trim())
                        .ToList();

                    katilimciMi = katilimciIdListesi.Contains(aktifKullaniciId.ToString());
                }

                if (!katilimciMi)
                {
                    return HttpNotFound("Bu rezervasyonun detayına bakma yetkiniz yok.");
                }
            }

            var oda = db.Rooms.FirstOrDefault(r => r.Id == rezervasyon.RoomId);
            ViewBag.RoomName = oda != null ? oda.Name : "Bilinmeyen Oda";

            var kuranKisi = db.Users.FirstOrDefault(u => u.Id == rezervasyon.UserId);
            ViewBag.UserName = kuranKisi != null ? kuranKisi.Name + " " + kuranKisi.Surname : "Bilinmeyen Kullanıcı";

            List<string> katilimciListesi = new List<string>();

            if (!string.IsNullOrEmpty(rezervasyon.Attendees))
            {
                var idStringListesi = rezervasyon.Attendees.Split(',');
                List<int> idListesi = new List<int>();

                foreach (var idStr in idStringListesi)
                {
                    if (int.TryParse(idStr.Trim(), out int parseEdilenId))
                    {
                        idListesi.Add(parseEdilenId);
                    }
                }

                katilimciListesi = db.Users
                                     .Where(u => idListesi.Contains(u.Id))
                                     .Select(u => u.Name + " " + u.Surname)
                                     .ToList();
            }

            ViewBag.KatilimciListesi = katilimciListesi;

            string gercekDurum = "Planlandı";
            if (rezervasyon.Status != "İptal Edildi" && rezervasyon.Status != "Bekliyor")
            {
                DateTime rezTarihi = rezervasyon.Date ?? DateTime.Today;
                TimeSpan baslangicSaati = rezervasyon.StartTime ?? TimeSpan.Zero;
                TimeSpan bitisSaati = rezervasyon.EndTime ?? TimeSpan.Zero;

                DateTime baslangic = rezTarihi.Add(baslangicSaati);
                DateTime bitis = rezTarihi.Add(bitisSaati);

                if (DateTime.Now >= baslangic && DateTime.Now <= bitis)
                {
                    gercekDurum = "Devam Ediyor";
                }
                else if (DateTime.Now > bitis)
                {
                    gercekDurum = "Tamamlandı";
                }
            }
            else
            {
                gercekDurum = rezervasyon.Status;
            }
            ViewBag.GercekDurum = gercekDurum;

            return View(rezervasyon);
        }
    }
}