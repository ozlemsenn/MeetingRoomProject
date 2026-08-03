using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MeetingProject.Models;

namespace MeetingProject.Controllers
{
    [Authorize]
    public class UsersController : BaseController
    {
        public ActionResult Index()
        {
            if (string.IsNullOrEmpty(GecerliRol()))
            {
                return RedirectToAction("Login", "Auth");
            }

            bool isAdmin = GecerliRol() == "Admin";
            bool isYonetici = GecerliRol() == "Yönetici";
            int aktifSirketId = GecerliSirketId();

            // Arayüzde (UI) Ekle/Düzenle butonlarını göstermek için ikisine de true gönderiyoruz
            ViewBag.IsAdmin = isAdmin || isYonetici;

            if (isAdmin)
            {
                ViewBag.Companies = new SelectList(db.Companies.ToList(), "Id", "Name");
            }

            // Filtreleme: Admin tüm kullanıcıları görür, Yönetici ve Personel sadece kendi şirketindekileri
            // Ayrıca "Pasif" olanları sadece Admin ve Yönetici görsün (isteğe bağlı eklenebilir)
            var filtreliKullanicilar = db.Users.Where(x => isAdmin || x.CompanyId == aktifSirketId).ToList();

            return View(filtreliKullanicilar);
        }

        [HttpGet]
        public ActionResult Create()
        {
            string rol = GecerliRol();
            if (rol != "Admin" && rol != "Yönetici")
                return Content("<div class='alert alert-danger m-4 text-center'><i class='fas fa-shield-alt fa-3x mb-3 text-danger'></i><br><b>Yetkisiz İşlem:</b> Sadece yöneticiler yeni kullanıcı ekleyebilir.</div>");

            if (rol == "Admin")
            {
                ViewBag.CompanyId = new SelectList(db.Companies.ToList(), "Id", "Name");
            }
            else
            {
                int sirketId = GecerliSirketId();
                var sirket = db.Companies.Where(c => c.Id == sirketId).ToList();
                ViewBag.CompanyId = new SelectList(sirket, "Id", "Name", sirketId);
            }
                return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create([Bind(Include = "Id,Name,Surname,Email,Password,Role,CompanyId,Department")] Users user)
        {
            string rol = GecerliRol();
            if (rol != "Admin" && rol != "Yonetici" && rol != "Yönetici")
            {
                return Json(new { success = false, message = "Yetkisiz işlem! Sadece yöneticiler kullanıcı ekleyebilir." });
            }

            try
            {
                // Yönetici ekliyorsa, CompanyId'yi arka planda kendi şirketine zorluyoruz
                if (rol != "Admin")
                {
                    user.CompanyId = GecerliSirketId();
                }

                // --- İŞTE EKLENEN KISIM BURASI (NULL ÇÖZÜMÜ) ---
                if (string.IsNullOrWhiteSpace(user.Role))
                {
                    user.Role = "Personel"; // Eğer formdan boş gelirse varsayılan olarak Personel yap
                }

                if (ModelState.IsValid)
                {
                    db.Users.Add(user);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Kullanıcı başarıyla oluşturuldu." });
                }
                return Json(new { success = false, message = "Lütfen alanları kontrol ediniz. Tüm alanlar zorunludur." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            string rol = GecerliRol();
            if (rol != "Admin" && rol != "Yönetici")
                return Content("<div class='alert alert-danger m-4 text-center'><i class='fas fa-shield-alt fa-3x mb-3 text-danger'></i><br><b>Yetkisiz İşlem:</b> Sadece yöneticiler kullanıcı bilgilerini düzenleyebilir.</div>");

            Users user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            int aktifSirketId = GecerliSirketId();
            if (rol == "Yönetici" && user.CompanyId != aktifSirketId)
            {
                return Content("<div class='alert alert-danger m-4 text-center'><i class='fas fa-ban fa-3x mb-3 text-danger'></i><br><b>Yetkisiz İşlem:</b> Başka bir şirketin kullanıcısını düzenleyemezsiniz.</div>");
            }

            if (rol == "Admin")
            {
                ViewBag.CompanyId = new SelectList(db.Companies.ToList(), "Id", "Name", user.CompanyId);
            }
            else
            {
                var sirket = db.Companies.Where(c => c.Id == aktifSirketId).ToList();
                ViewBag.CompanyId = new SelectList(sirket, "Id", "Name", user.CompanyId);
            }
                return View(user);
        }

        [HttpPost]
        public JsonResult Edit(Users model)
        {
            string rol = GecerliRol();
            int aktifSirketId = GecerliSirketId();

            if (rol != "Admin" && rol != "Yönetici")
            {
                return Json(new { success = false, message = "Yetkisiz işlem! Düzenleme yapamazsınız." });
            }

            try
            {
                var guncellenecekKullanici = db.Users.Find(model.Id);

                if (guncellenecekKullanici == null || (rol == "Yönetici" && guncellenecekKullanici.CompanyId != aktifSirketId))
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı veya yetkiniz yok!" });
                }

                guncellenecekKullanici.Name = model.Name;
                guncellenecekKullanici.Surname = model.Surname;
                guncellenecekKullanici.Email = model.Email;
                guncellenecekKullanici.Department = model.Department;
                guncellenecekKullanici.Role = model.Role;

                // Admin değilse, CompanyId'yi değiştirmesine izin vermiyoruz (Güvenlik)
                if (rol == "Admin")
                {
                    guncellenecekKullanici.CompanyId = model.CompanyId;
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Users user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();

            if (!isAdmin && user.CompanyId != aktifSirketId)
            {
                return Content("<div class='alert alert-danger m-4 text-center'><i class='fas fa-ban fa-3x mb-3 text-danger'></i><br><b>Yetkisiz İşlem:</b> Sadece kendi şirketinizdeki personelleri görüntüleyebilirsiniz.</div>");
            }

            return View(user);
        }

        [HttpGet]
        public ActionResult Delete(int? id, string mode = "")
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            string rol = GecerliRol();
            bool isAdmin = rol == "Admin";
            bool isYonetici = rol == "Yonetici" || rol == "Yönetici";
            int aktifKullaniciId = GecerliKullaniciId();

            if (!isAdmin && !isYonetici) return Content("<div class='alert alert-danger m-4 text-center'>Yetkisiz İşlem</div>");

            if (id == aktifKullaniciId) return Content("<div class='alert alert-warning m-4 text-center'>Kendi hesabınızı silemezsiniz.</div>");

            Users user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            if (isYonetici && user.CompanyId != GecerliSirketId()) return Content("<div class='alert alert-danger m-4 text-center'>Yetkisiz İşlem</div>");

            ViewBag.IsAdmin = isAdmin;
            ViewBag.Mode = mode; // "status" veya "delete"
            return PartialView(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteConfirmed(int id, string PassiveReason, string ActionType)
        {
            string rol = GecerliRol();
            bool isAdmin = rol == "Admin";
            bool isYonetici = rol == "Yonetici" || rol == "Yönetici";
            int aktifSirketId = GecerliSirketId();
            int aktifKullaniciId = GecerliKullaniciId();

            if (!isAdmin && !isYonetici)
                return Json(new { success = false, message = "Yetkisiz işlem!" });

            if (id == aktifKullaniciId)
                return Json(new { success = false, message = "Kendi hesabınızı silemez veya pasife alamazsınız!" });

            try
            {
                var silinecekKullanici = db.Users.Find(id);

                if (silinecekKullanici == null || (isYonetici && silinecekKullanici.CompanyId != aktifSirketId))
                    return Json(new { success = false, message = "Kullanıcı bulunamadı veya yetkiniz yok." });

                // Geriye dönük uyumluluk: Eğer formdan ActionType boş gelirse
                // Adminse kalıcı sil, Yöneticiyse durumu değiştir varsayıyoruz.
                if (string.IsNullOrEmpty(ActionType))
                {
                    ActionType = isAdmin ? "HardDelete" : "ToggleStatus";
                }

                // 1. İHTİMAL: KALICI SİLME
                if (ActionType == "HardDelete")
                {
                    if (!isAdmin) return Json(new { success = false, message = "Sadece Admin kalıcı silme işlemi yapabilir!" });

                    db.Users.Remove(silinecekKullanici);
                }
                // 2. İHTİMAL: PASİFE / AKTİFE ALMA
                else
                {
                    if (silinecekKullanici.Role == "Pasif")
                    {
                        silinecekKullanici.Role = "Personel"; // Aktife al
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(PassiveReason))
                            return Json(new { success = false, message = "Lütfen pasife alma sebebini belirtiniz!" });

                        silinecekKullanici.Role = "Pasif"; // Pasife al
                    }
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        // ... GetUsers ve GetUserData metotları aynen kalabilir, admin ve sirketId kontrolü zaten doğru çalışıyor.
        [HttpGet]
        public JsonResult GetUsers()
        {
            if (string.IsNullOrEmpty(GecerliRol())) return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();
            int aktifKullaniciId = GecerliKullaniciId();

            // 1. Önce veritabanından Ham datayı çekiyoruz
            var usersFromDb = db.Users
                                .Where(x => isAdmin || x.CompanyId == aktifSirketId)
                                .ToList();

            // 2. Ardından formatlıyoruz (Kendi ismine "Kendiniz" ekliyoruz, Rolünü "Yönetici" olarak sabitliyoruz)
            var kullanicilar = usersFromDb.Select(x => new
            {
                Id = x.Id,
                // Eğer ID benim ID'm ise sonuna (Kendiniz) yaz
                Name = x.Id == aktifKullaniciId ? x.Name + " (Kendiniz)" : x.Name,
                Surname = x.Surname,
                CompanyId = x.CompanyId,
                Email = x.Email,
                Department = x.Department,
                // Türkçe/İngilizce karakter sorununu çöz ve netleştir
                Role = (x.Role == "Yonetici" || x.Role == "Yönetici") ? "Yönetici" : x.Role,
                IsCurrentUser = (x.Id == aktifKullaniciId) // Frontend'e bu benim diye sinyal gönderiyoruz
            }).ToList();

            return Json(kullanicilar, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetUserData(int id)
        {
            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();

            var user = db.Users.Where(u => u.Id == id).Select(u => new {
                u.Id,
                u.Name,
                u.Surname,
                u.Email,
                u.Department,
                u.CompanyId,
                u.Role
            }).FirstOrDefault();

            if (user != null && !isAdmin && user.CompanyId != aktifSirketId)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            return Json(user, JsonRequestBehavior.AllowGet);
        }
    }
}