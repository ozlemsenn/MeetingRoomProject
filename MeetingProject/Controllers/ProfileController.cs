using System;
using System.Linq;
using System.Web.Mvc;
using MeetingProject.Models;

namespace MeetingProject.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private MeetingAppEntities1 db = new MeetingAppEntities1();

        [HttpGet]
        public ActionResult Index()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Find(userId);
            if (user == null) return HttpNotFound();

            return PartialView(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateProfile(Users model)
        {
            if (Session["UserId"] == null) return Json(new { success = false, message = "Oturum süresi doldu." });

            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                var mevcutKullanici = db.Users.Find(userId);

                if (mevcutKullanici == null) return Json(new { success = false, message = "Kullanıcı bulunamadı." });

                mevcutKullanici.Name = model.Name;
                mevcutKullanici.Surname = model.Surname;
                mevcutKullanici.Email = model.Email;

                db.SaveChanges();
                Session["UserName"] = model.Name + " " + model.Surname;

                return Json(new { success = true, message = "Profil bilgileriniz başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ChangePasswordConfirm(string currentPassword, string newPassword, string confirmPassword)
        {
            if (Session["UserId"] == null) return Json(new { success = false, message = "Oturum süresi doldu." });

            try
            {
                if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                    return Json(new { success = false, message = "Lütfen tüm alanları doldurun." });

                if (newPassword != confirmPassword)
                    return Json(new { success = false, message = "Yeni şifreler birbiriyle uyuşmuyor." });

                if (currentPassword == newPassword)
                    return Json(new { success = false, message = "Yeni şifreniz eski şifrenizle aynı olamaz." });

                if (newPassword.Length < 8)
                    return Json(new { success = false, message = "Şifreniz en az 8 karakter olmalıdır." });

                if (!newPassword.Any(char.IsDigit))
                    return Json(new { success = false, message = "Şifreniz en az bir rakam (0-9) içermelidir." });

                if (!newPassword.Any(char.IsLetter))
                    return Json(new { success = false, message = "Şifreniz en az bir harf içermelidir." });

                int userId = Convert.ToInt32(Session["UserId"]);
                var user = db.Users.Find(userId);

                if (user.Password != currentPassword)
                    return Json(new { success = false, message = "Mevcut şifrenizi yanlış girdiniz." });

                user.Password = newPassword;
                db.SaveChanges();

                return Json(new { success = true, message = "Şifreniz başarıyla değiştirildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
    }
}