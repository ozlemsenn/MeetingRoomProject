using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MeetingProject.Models;

namespace MeetingProject.Controllers
{
    [AllowAnonymous] // Giriş sayfasına herkesin erişebilmesi için kiliti açıyoruz
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
                // Biletin içine gömeceğimiz veriyi hazırlıyoruz (Rol|Ad Soyad)
                string adSoyad = user.Name + " " + user.Surname;
                string userData = user.Role + "|" + adSoyad;

                // 1. GİRİŞ BAŞARILI! Bilet (Ticket) ve Cookie oluşturma işlemleri...
                var ticket = new FormsAuthenticationTicket(
                    1,
                    user.Email,
                    DateTime.Now,
                    DateTime.Now.AddHours(24),
                    false,
                    userData // <--- SADECE BURAYI DEĞİŞTİRDİK (user.Role yerine userData yazdık)
                );

                string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                Response.Cookies.Add(cookie);

                Session["UserId"] = user.Id;
                Session["UserName"] = user.Name + " " + user.Surname;
                Session["UserRole"] = user.Role;

                // 2. YENİ EKLENEN KISIM: ROL BAZLI YÖNLENDİRME (ROUTING)
                if (user.Role == "Admin")
                {
                    // Admin giriş yaparsa özel Admin paneline (Dashboard) gitsin
                    return RedirectToAction("Index", "Admin");
                }
                else if (user.Role == "Yonetici")
                {
                    // Yöneticiler için ayrı bir ekrana gitsin (şimdilik adminle aynı yapabiliriz)
                    return RedirectToAction("Index", "Yonetici");
                }
                else
                {
                    // Standart Personel giriş yaparsa doğrudan Takvim/Rezervasyon sayfasına gitsin
                    return RedirectToAction("Index", "Reservations");
                }
            }
            else
            {
                // 3. GİRİŞ BAŞARISIZ!
                ViewBag.Hata = "E-posta adresi veya şifre hatalı!";
                return View();
            }
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Auth");
        }
    }
}