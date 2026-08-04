using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MeetingProject.Models;
using ClosedXML.Excel;
using System.IO;

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

            var kullanicilar = db.Users.Where(x => isAdmin || x.CompanyId == aktifSirketId).ToList();

            return View(kullanicilar);
        }

        [HttpGet]
        public ActionResult ExcelIndir()
        {
            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();

            var kullanicilar = db.Users.Where(x => isAdmin || x.CompanyId == aktifSirketId).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Kullanici Listesi");

                worksheet.Cell(1, 1).Value = "Ad";
                worksheet.Cell(1, 2).Value = "Soyad";
                worksheet.Cell(1, 3).Value = "E-Posta";
                worksheet.Cell(1, 4).Value = "Departman";
                worksheet.Cell(1, 5).Value = "Rol";
                worksheet.Cell(1, 6).Value = "Durum";

                worksheet.Range("A1:F1").Style.Font.Bold = true;
                worksheet.Range("A1:F1").Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 2;
                foreach (var user in kullanicilar)
                {
                    worksheet.Cell(row, 1).Value = user.Name;
                    worksheet.Cell(row, 2).Value = user.Surname;
                    worksheet.Cell(row, 3).Value = user.Email;
                    worksheet.Cell(row, 4).Value = user.Department;
                    worksheet.Cell(row, 5).Value = user.Role;
                    worksheet.Cell(row, 6).Value = (user.IsActive == true) ? "Aktif" : "Pasif";
                    row++;
                }

                worksheet.Columns().AdjustToContents(); 

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Kullanicilar_{DateTime.Now:ddMMyyyy}.xlsx");
                }
            }
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
        public JsonResult Create([Bind(Include = "Id,Name,Surname,Email,Password,Role,CompanyId,Department,IsActive")] Users user)
        {
            string rol = GecerliRol();
            if (rol != "Admin" && rol != "Yonetici" && rol != "Yönetici")
                return Json(new { success = false, message = "Yetkisiz işlem!" });

            try
            {
                if (rol != "Admin") user.CompanyId = GecerliSirketId();

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
                guncellenecekKullanici.IsActive = model.IsActive; 

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

                if (ActionType == "HardDelete")
                {
                    if (!isAdmin) return Json(new { success = false, message = "Sadece Admin kalıcı silebilir!" });
                    db.Users.Remove(silinecekKullanici);
                }
                else
                {
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
            if (string.IsNullOrEmpty(GecerliRol())) return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();

            var kullanicilar = db.Users
                .Where(x => isAdmin || x.CompanyId == aktifSirketId)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Surname,
                    x.Email,       
                    x.Department,  
                    x.IsActive,
                    Role = (x.Role == "Yonetici" || x.Role == "Yönetici") ? "Yönetici" : x.Role
                }).ToList();

            return Json(kullanicilar, JsonRequestBehavior.AllowGet);
        }
    }
}