using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
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

            string hashliSifre = Sifrele(Password);
            var user = db.Users.FirstOrDefault(x => x.Email == Email && x.Password == hashliSifre);

            if (user != null)
            {
                if (user.IsActive == false)
                {
                    ViewBag.Hata = "Hesabınız pasif durumdadır, giriş yapamazsınız.";
                    return View();
                }

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

        private string RastgeleSifreUret(int uzunluk = 6)
        {
            string karakterler = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random rnd = new Random();
            char[] sifre = new char[uzunluk];

            for (int i = 0; i < uzunluk; i++)
            {
                sifre[i] = karakterler[rnd.Next(karakterler.Length)];
            }

            return new string(sifre);
        }

        private string Sifrele(string metin)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(metin));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2")); 
                }
                return builder.ToString();
            }
        }

        [HttpPost]
        public JsonResult ForgotPassword(string Email)
        {
            var user = db.Users.FirstOrDefault(x => x.Email == Email);

            if (user != null)
            {
                string yeniSifre = RastgeleSifreUret(6);
                user.Password = Sifrele(yeniSifre);
                db.SaveChanges();

                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    SmtpClient smtpClient = new SmtpClient("sandbox.smtp.mailtrap.io")
                    {
                        Port = 2525,
                        Credentials = new NetworkCredential("ad1f1f81bc7a2c", "3eaf082f6b67c8"),
                        EnableSsl = true,
                    };

                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress("sistem@bookroom.com", "BookRoom Sistemi");
                    mail.To.Add(Email);
                    mail.Subject = "Şifre Sıfırlama Talebi";

                    mail.Body = $"Merhaba {user.Name},\n\nŞifreniz başarıyla sıfırlandı.\n\nYeni şifreniz: {yeniSifre}\n\nGüvenliğiniz için sisteme giriş yaptıktan sonra profilinizden şifrenizi değiştirmenizi öneririz.";
                    mail.IsBodyHtml = false;

                    smtpClient.Send(mail);

                    return Json(new
                    {
                        success = true,
                        message = "Şifreniz sıfırlandı! Lütfen gelen kutunuzu kontrol ediniz."
                    });
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mail gönderilirken hata oluştu: " + ex.Message
                    });
                }
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
