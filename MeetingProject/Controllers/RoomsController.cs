using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ClosedXML.Excel;
using MeetingProject.Models;

namespace MeetingProject.Controllers
{
    [Authorize]
    public class RoomsController : BaseController
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

            ViewBag.IsAdmin = isAdmin || isYonetici;

            if (isAdmin)
            {
                ViewBag.Companies = new SelectList(db.Companies.ToList(), "Id", "Name");
            }

            var filtreliOdalar = db.Rooms.Where(x => isAdmin || x.CompanyId == aktifSirketId).ToList();

            return View(filtreliOdalar);
        }

        [HttpGet]
        public ActionResult ExcelIndir()
        {
            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();

            var odalar = db.Rooms.Where(x => isAdmin || x.CompanyId == aktifSirketId).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Odalar");

                worksheet.Cell(1, 1).Value = "Oda Adı";
                worksheet.Cell(1, 2).Value = "Kapasite";
                worksheet.Cell(1, 3).Value = "Projeksiyon";
                worksheet.Cell(1, 4).Value = "Durum";
                worksheet.Range("A1:D1").Style.Font.Bold = true;

                int row = 2;
                foreach (var oda in odalar)
                {
                    worksheet.Cell(row, 1).Value = oda.Name.Replace(" (Pasif)", "");
                    worksheet.Cell(row, 2).Value = oda.Capacity;
                    worksheet.Cell(row, 3).Value = (oda.HasProjector == true) ? "Var" : "Yok";
                    worksheet.Cell(row, 4).Value = oda.Name.Contains("(Pasif)") ? "Pasif" : "Aktif";
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Odalar_{DateTime.Now:ddMMyyyy}.xlsx");
                }
            }
        }

        [HttpGet]
        public JsonResult GetRooms()
        {
            if (Session["UserRole"] == null) return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            bool isAdmin = Session["UserRole"].ToString() == "Admin";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

            var odalar = db.Rooms
                           .Where(x => isAdmin || x.CompanyId == aktifSirketId)
                           .ToList()
                           .Select(x => new
                           {
                               Id = x.Id,
                               Name = x.Name.Replace(" (Pasif)", ""),
                               companyId = x.CompanyId,
                               Capacity = x.Capacity,
                               HasProjector = x.HasProjector,
                               Status = x.Name.Contains("(Pasif)") ? "Pasif" : "Aktif"
                           }).ToList();

            return Json(odalar, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Rooms rooms = db.Rooms.Find(id);
            if (rooms == null) return HttpNotFound();

            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();

            if (!isAdmin && rooms.CompanyId != aktifSirketId)
            {
                return Content("<div class='alert alert-danger m-4 text-center'><i class='fas fa-ban fa-3x mb-3 text-danger'></i><br><b>Yetkisiz İşlem</b></div>");
            }
            return PartialView(rooms);
        }

        [HttpGet]
        public ActionResult Create()
        {
            string rol = GecerliRol();
            if (rol != "Admin" && rol != "Yönetici")
                return Content("<div class='alert alert-danger'>Yetkisiz İşlem</div>");

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

            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,Capacity,HasProjector,CompanyId")] Rooms rooms)
        {
            string rol = GecerliRol();

            if (rol != "Admin" && rol != "Yönetici")
            {
                return Json(new { success = false, message = "Yetkisiz İşlem" });
            }

            if (rol != "Admin")
            {
                rooms.CompanyId = GecerliSirketId();
            }

            if (ModelState.IsValid)
            {
                db.Rooms.Add(rooms);
                db.SaveChanges();
                return Json(new { success = true, room = rooms });
            }

            return Json(new { success = false, message = "Lütfen tüm alanları eksiksiz doldurun." });
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Rooms rooms = db.Rooms.Find(id);
            if (rooms == null) return HttpNotFound();

            string rol = GecerliRol();
            int aktifSirketId = GecerliSirketId();

            if (rol != "Admin" && rol != "Yönetici") return Content("<div class='alert alert-danger'>Yetkisiz İşlem</div>");
            if (rol == "Yönetici" && rooms.CompanyId != aktifSirketId) return Content("<div class='alert alert-danger'>Başka şirketin odasını düzenleyemezsiniz.</div>");

            if (rol == "Admin")
            {
                ViewBag.CompanyId = new SelectList(db.Companies.ToList(), "Id", "Name", rooms.CompanyId);
            }
            else
            {
                var sirket = db.Companies.Where(c => c.Id == aktifSirketId).ToList();
                ViewBag.CompanyId = new SelectList(sirket, "Id", "Name", rooms.CompanyId);
            }

            return PartialView(rooms);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Capacity,HasProjector,CompanyId")] Rooms rooms)
        {
            string rol = GecerliRol();
            int aktifSirketId = GecerliSirketId();

            if (rol != "Admin" && rol != "Yönetici") return Json(new { success = false, message = "Yetkisiz işlem!" });

            if (rol != "Admin")
            {
                rooms.CompanyId = aktifSirketId;
            }

            if (ModelState.IsValid)
            {
                var gercekOda = db.Rooms.AsNoTracking().FirstOrDefault(x => x.Id == rooms.Id);
                if (gercekOda == null || (rol == "Yönetici" && gercekOda.CompanyId != aktifSirketId))
                {
                    return Json(new { success = false, message = "Yetkisiz işlem!" });
                }

                db.Entry(rooms).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Form verileri geçersiz." });
        }

        [HttpGet]
        public ActionResult Delete(int? id, string mode = "")
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Rooms rooms = db.Rooms.Find(id);
            if (rooms == null) return HttpNotFound();

            string rol = GecerliRol();
            bool isAdmin = rol == "Admin";

            if (rol != "Admin" && rol != "Yönetici" && rol != "Yonetici") return Content("<div class='alert alert-danger'>Yetkisiz İşlem</div>");
            if (!isAdmin && rooms.CompanyId != GecerliSirketId()) return Content("<div class='alert alert-danger'>Yetkisiz İşlem</div>");

            ViewBag.IsAdmin = isAdmin;
            ViewBag.Mode = mode;
            return PartialView(rooms);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id, string PassiveReason, string ActionType)
        {
            string rol = GecerliRol();
            bool isAdmin = rol == "Admin";
            bool isYonetici = rol == "Yonetici" || rol == "Yönetici";
            int aktifSirketId = GecerliSirketId();

            if (!isAdmin && !isYonetici) return Json(new { success = false, message = "Yetkisiz işlem!" });

            Rooms rooms = db.Rooms.Find(id);

            if (rooms == null || (isYonetici && rooms.CompanyId != aktifSirketId))
            {
                return Json(new { success = false, message = "Oda bulunamadı veya yetkiniz yok!" });
            }

            if (string.IsNullOrEmpty(ActionType))
            {
                ActionType = isAdmin ? "HardDelete" : "ToggleStatus";
            }

            if (ActionType == "HardDelete")
            {
                if (!isAdmin) return Json(new { success = false, message = "Sadece Admin kalıcı silme işlemi yapabilir!" });
                db.Rooms.Remove(rooms);
            }
            else 
            {
                if (rooms.Name.Contains("(Pasif)")) 
                {
                    rooms.Name = rooms.Name.Replace(" (Pasif)", "");
                }
                else 
                {
                    var yakinRezervasyon = db.Reservations
                        .Where(r => r.RoomId == id && r.Date >= DateTime.Today && r.Status != "İptal Edildi")
                        .OrderBy(r => r.Date)
                        .ThenBy(r => r.StartTime)
                        .FirstOrDefault();

                    if (yakinRezervasyon != null)
                    {
                        string tarihStr = yakinRezervasyon.Date.Value.ToString("dd.MM.yyyy");
                        return Json(new { success = false, message = $"Bu oda için en yakın {tarihStr} tarihinde onaylı bir rezervasyon bulunmaktadır. Oda pasife alınamaz!" });
                    }

                    if (string.IsNullOrWhiteSpace(PassiveReason))
                        return Json(new { success = false, message = "Lütfen odayı pasife alma sebebini belirtiniz!" });

                    rooms.PassiveReason = PassiveReason;
                    rooms.Name += " (Pasif)";
                }
            }

            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult GetRoomData(int id)
        {
            var oda = db.Rooms
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    Name = r.Name.Replace(" (Pasif)", ""),
                    r.Capacity,
                    r.HasProjector,
                    Status = r.Name.Contains("(Pasif)") ? "Pasif" : "Aktif",
                    r.PassiveReason
                })
                .FirstOrDefault();

            return Json(oda, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}