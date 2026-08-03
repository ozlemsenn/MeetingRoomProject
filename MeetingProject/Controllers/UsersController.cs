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
            if (string.IsNullOrEmpty(GecerliRol())) return RedirectToAction("Login", "Auth");

            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();

            ViewBag.IsAdmin = isAdmin || (GecerliRol() == "Yönetici");

            if (isAdmin)
                ViewBag.Companies = new SelectList(db.Companies.ToList(), "Id", "Name");

            // Artık IsActive kontrolü ile listeliyoruz
            var kullanicilar = db.Users.Where(x => isAdmin || x.CompanyId == aktifSirketId).ToList();

            return View(kullanicilar);
        }

        [HttpGet]
        public ActionResult Create()
        {
            string rol = GecerliRol();
            if (rol != "Admin" && rol != "Yönetici")
                return Content("Yetkisiz İşlem.");

            if (rol == "Admin") ViewBag.CompanyId = new SelectList(db.Companies.ToList(), "Id", "Name");
            else
            {
                int sirketId = GecerliSirketId();
                ViewBag.CompanyId = new SelectList(db.Companies.Where(c => c.Id == sirketId).ToList(), "Id", "Name", sirketId);
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // [Bind] içerisine "IsActive" ekledik
        public JsonResult Create([Bind(Include = "Id,Name,Surname,Email,Password,Role,CompanyId,Department,IsActive")] Users user)
        {
            string rol = GecerliRol();
            if (rol != "Admin" && rol != "Yonetici" && rol != "Yönetici")
                return Json(new { success = false, message = "Yetkisiz işlem!" });

            try
            {
                if (rol != "Admin") user.CompanyId = GecerliSirketId();

                // Yeni kullanıcı her zaman AKTİF başlar
                user.IsActive = true;

                if (string.IsNullOrWhiteSpace(user.Role)) user.Role = "Personel";
                if (string.IsNullOrWhiteSpace(user.Password)) user.Password = "123456";

                if (rol != "Admin" && user.Role == "Admin")
                    return Json(new { success = false, message = "Admin oluşturma yetkiniz yok!" });

                if (user.Role == "Admin") user.CompanyId = null;

                if (ModelState.IsValid)
                {
                    db.Users.Add(user);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Kullanıcı başarıyla oluşturuldu." });
                }
                return Json(new { success = false, message = "Eksik bilgi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Edit(Users model)
        {
            string rol = GecerliRol();
            int aktifSirketId = GecerliSirketId();

            if (rol != "Admin" && rol != "Yönetici")
                return Json(new { success = false, message = "Yetkisiz işlem!" });

            try
            {
                var guncellenecekKullanici = db.Users.Find(model.Id);

                if (guncellenecekKullanici == null || (rol == "Yönetici" && guncellenecekKullanici.CompanyId != aktifSirketId))
                    return Json(new { success = false, message = "Yetkiniz yok!" });

                guncellenecekKullanici.Name = model.Name;
                guncellenecekKullanici.Surname = model.Surname;
                guncellenecekKullanici.Email = model.Email;
                guncellenecekKullanici.Department = model.Department;
                guncellenecekKullanici.Role = model.Role;
                guncellenecekKullanici.IsActive = model.IsActive; // Durumu güncelledik

                if (rol == "Admin") guncellenecekKullanici.CompanyId = model.CompanyId;

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteConfirmed(int id, string PassiveReason, string ActionType)
        {
            string rol = GecerliRol();
            bool isAdmin = rol == "Admin";
            bool isYonetici = rol == "Yonetici" || rol == "Yönetici";

            if (!isAdmin && !isYonetici) return Json(new { success = false, message = "Yetkisiz işlem!" });

            try
            {
                var silinecekKullanici = db.Users.Find(id);
                if (silinecekKullanici == null) return Json(new { success = false, message = "Kullanıcı bulunamadı." });

                // KALICI SİLME (Sadece Admin)
                if (ActionType == "HardDelete")
                {
                    if (!isAdmin) return Json(new { success = false, message = "Sadece Admin kalıcı silebilir!" });
                    db.Users.Remove(silinecekKullanici);
                }
                // DURUM DEĞİŞTİRME (IsActive üzerinden)
                else
                {
                    // true ise false yap, false ise true yap (Toggle)
                    silinecekKullanici.IsActive = !silinecekKullanici.IsActive;
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetUsers()
        {
            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();

            var kullanicilar = db.Users
                .Where(x => isAdmin || x.CompanyId == aktifSirketId)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Surname,
                    x.IsActive, // Artık aktiflik bilgisini de gönderiyoruz
                    Role = (x.Role == "Yonetici" || x.Role == "Yönetici") ? "Yönetici" : x.Role
                }).ToList();

            return Json(kullanicilar, JsonRequestBehavior.AllowGet);
        }
    }
}