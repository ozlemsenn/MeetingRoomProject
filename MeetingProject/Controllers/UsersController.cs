using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MeetingProject.Models; 

namespace MeetingProject.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private MeetingAppEntities1 db = new MeetingAppEntities1();

        public ActionResult Index()
        {
            if (Session["UserRole"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            if (Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin")
            {
                ViewBag.Companies = new SelectList(db.Companies.ToList(), "Id", "Name");
            }

            int aktifSirketId = 0;
            if (Session["CompanyId"] != null)
            {
                aktifSirketId = Convert.ToInt32(Session["CompanyId"]);
            }

            bool isAdmin = Session["UserRole"].ToString() == "Admin";

            var filtreliKullanicilar = db.Users.Where(x => isAdmin || x.CompanyId == aktifSirketId).ToList();

            return View(filtreliKullanicilar);
        }

        [HttpGet]
        public ActionResult Create()
        {
            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";

            if (isAdmin)
            {
                ViewBag.CompanyId = new SelectList(db.Companies.ToList(), "Id", "Name");
            }
            return View();
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Users user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

            if (!isAdmin && user.CompanyId != aktifSirketId)
            {
                return RedirectToAction("Index", "Users");
            }
            if (isAdmin)
            {
                ViewBag.CompanyId = new SelectList(db.Companies.ToList(), "Id", "Name", user.CompanyId);
            }

            return View(user);
        }

        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Users user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

            if (!isAdmin && user.CompanyId != aktifSirketId)
            {
                return RedirectToAction("Index", "Users"); 
            }

            return View(user); 
        }

        [HttpGet]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Users user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

            if (!isAdmin && user.CompanyId != aktifSirketId)
            {
                return RedirectToAction("Index", "Users");
            }

            return View(user);
        }

        [HttpGet]
        public JsonResult GetUsers()
        {
            if (Session["UserRole"] == null)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            bool isAdmin = Session["UserRole"].ToString() == "Admin";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

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
            var user = db.Users.Where(u => u.Id == id).Select(u => new {
                u.Id,
                u.Name,
                u.Surname,
                u.Email,
                u.Department
            }).FirstOrDefault();

            return Json(user, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create([Bind(Include = "Id,Name,Surname,Email,Password,Role,CompanyId")] Users user)
        {
            try
            {
                bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";

                if (!isAdmin)
                {
                    user.CompanyId = Convert.ToInt32(Session["CompanyId"]);
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

        [HttpPost]
        public JsonResult Edit(Users model)
        {
            try
            {
                bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
                int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

                var guncellenecekKullanici = db.Users.Find(model.Id);

                if (guncellenecekKullanici == null || (!isAdmin && guncellenecekKullanici.CompanyId != aktifSirketId))
                {
                    return Json(new { success = false, message = "Yetkisiz erişim veya kullanıcı bulunamadı!" });
                }

                guncellenecekKullanici.Name = model.Name;
                guncellenecekKullanici.Surname = model.Surname;
                guncellenecekKullanici.Email = model.Email;
                guncellenecekKullanici.Department = model.Department;

                if (isAdmin)
                {
                    guncellenecekKullanici.Role = model.Role;
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken] 
        public JsonResult DeleteConfirmed(int id)
        {
            try
            {
                bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
                int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

                var silinecekKullanici = db.Users.Find(id);

                if (silinecekKullanici == null || (!isAdmin && silinecekKullanici.CompanyId != aktifSirketId))
                {
                    return Json(new { success = false, message = "Yetkisiz işlem veya kullanıcı bulunamadı." });
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
    }
}