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
                return RedirectToAction("Login", "Account");
            }

            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();

            ViewBag.IsAdmin = isAdmin;

            if (isAdmin)
            {
                ViewBag.Companies = new SelectList(db.Companies.ToList(), "Id", "Name");
            }

            var filtreliKullanicilar = db.Users.Where(x => isAdmin || x.CompanyId == aktifSirketId).ToList();

            return View(filtreliKullanicilar);
        }

        [HttpGet]
        public ActionResult Create()
        {
            if (GecerliRol() != "Admin")
                return Content("<div class='alert alert-danger m-4 text-center'><i class='fas fa-shield-alt fa-3x mb-3 text-danger'></i><br><b>Yetkisiz İşlem:</b> Sadece sistem yöneticileri yeni kullanıcı ekleyebilir.</div>");

            ViewBag.CompanyId = new SelectList(db.Companies.ToList(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create([Bind(Include = "Id,Name,Surname,Email,Password,Role,CompanyId")] Users user)
        {
            if (GecerliRol() != "Admin")
            {
                return Json(new { success = false, message = "Yetkisiz işlem! Sadece admin kullanıcı ekleyebilir." });
            }

            try
            {
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

            if (GecerliRol() != "Admin")
                return Content("<div class='alert alert-danger m-4 text-center'><i class='fas fa-shield-alt fa-3x mb-3 text-danger'></i><br><b>Yetkisiz İşlem:</b> Sadece sistem yöneticileri kullanıcı bilgilerini düzenleyebilir.</div>");

            Users user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            ViewBag.CompanyId = new SelectList(db.Companies.ToList(), "Id", "Name", user.CompanyId);
            return View(user);
        }

        [HttpPost]
        public JsonResult Edit(Users model)
        {
            if (GecerliRol() != "Admin")
            {
                return Json(new { success = false, message = "Yetkisiz işlem! Sadece admin düzenleme yapabilir." });
            }

            try
            {
                var guncellenecekKullanici = db.Users.Find(model.Id);

                if (guncellenecekKullanici == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı!" });
                }

                guncellenecekKullanici.Name = model.Name;
                guncellenecekKullanici.Surname = model.Surname;
                guncellenecekKullanici.Email = model.Email;
                guncellenecekKullanici.Department = model.Department;
                guncellenecekKullanici.Role = model.Role;
                guncellenecekKullanici.CompanyId = model.CompanyId;

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
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (GecerliRol() != "Admin")
                return Content("<div class='alert alert-danger m-4 text-center'><i class='fas fa-shield-alt fa-3x mb-3 text-danger'></i><br><b>Yetkisiz İşlem:</b> Sadece sistem yöneticileri kullanıcı silebilir.</div>");

            Users user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteConfirmed(int id)
        {
            if (GecerliRol() != "Admin")
            {
                return Json(new { success = false, message = "Yetkisiz işlem! Sadece admin silebilir." });
            }

            try
            {
                var silinecekKullanici = db.Users.Find(id);

                if (silinecekKullanici == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı." });
                }

                db.Users.Remove(silinecekKullanici);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu." });
            }
        }

        [HttpGet]
        public JsonResult GetUsers()
        {
            if (string.IsNullOrEmpty(GecerliRol()))
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();

            var kullanicilar = db.Users
                                 .Where(x => isAdmin || x.CompanyId == aktifSirketId)
                                 .Select(x => new
                                 {
                                     Id = x.Id,
                                     Name = x.Name,
                                     Surname = x.Surname,
                                     CompanyId = x.CompanyId,
                                     Email = x.Email,
                                     Department = x.Department
                                 })
                                 .ToList();

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
                u.CompanyId
            }).FirstOrDefault();

            if (user != null && !isAdmin && user.CompanyId != aktifSirketId)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            return Json(user, JsonRequestBehavior.AllowGet);
        }
    }
}