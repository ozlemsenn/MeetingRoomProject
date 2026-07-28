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
                // 1. Şirket ID'sini de veritabanından çekiyoruz
                string adSoyad = user.Name + " " + user.Surname;
                string sirketId = user.CompanyId.ToString();

                // 2. Araya bir çizgi daha koyup 3 parçalı yapıyoruz: Rol|AdSoyad|SirketId
                // Örnek: "Personel|Ahmet Yılmaz|1"
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

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Auth");
        }
    }
}