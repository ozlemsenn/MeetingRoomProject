using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MeetingProject.Models;

namespace MeetingProject.Controllers
{
    [AllowAnonymous] 
    public class AuthController : Controller
    {
        private MeetingAppEntities1 db = new MeetingAppEntities1();

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string Email, string Password)
        {
            var user = db.Users.FirstOrDefault(x => x.Email == Email && x.Password == Password);

            if (user != null)
            {
                string adSoyad = user.Name + " " + user.Surname;
                string sirketId = user.CompanyId.ToString();

                string userData = user.Role + "|" + adSoyad + "|" + sirketId;

                var ticket = new FormsAuthenticationTicket(
                    1, user.Email, DateTime.Now, DateTime.Now.AddHours(24), false, userData
                
                );

                string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                Response.Cookies.Add(cookie);

                Session["UserId"] = user.Id;
                Session["UserName"] = user.Name + " " + user.Surname;
                Session["UserRole"] = user.Role;
                Session["CompanyId"] = user.CompanyId;

                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (user.Role == "Yonetici")
                {
                    return RedirectToAction("Index", "Yonetici");
                }
                else
                {
                    return RedirectToAction("Index", "Reservations");
                }
            }
            else
            {
                ViewBag.Hata = "E-posta adresi veya şifre hatalı!";
                return View();
            }
        }

        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            if (Request.Cookies["UserCookie"] != null)
            {
                var cookie = new HttpCookie("UserCookie");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            return RedirectToAction("Login", "Auth");
        }

        [HttpPost]
        public JsonResult ForgotPassword(string Email)
        {
            var user = db.Users.FirstOrDefault(x => x.Email == Email);

            if (user != null)
            {
                user.Password = "123456";
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Şifreniz başarıyla <b>123456</b> olarak sıfırlandı.<br><br>Lütfen giriş yapıp profilinizden şifrenizi güncelleyiniz."
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Sistemde bu e-posta adresine ait bir kullanıcı bulunamadı."
                });
            }
        }
    }
}