using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Odbc;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MeetingProject.Models;

namespace MeetingProject.Controllers
{
    [Authorize] 
    public class RoomsController : BaseController
    {

        public ActionResult Index()
        {
            // 1. Güvenlik Kontrolü
            if (string.IsNullOrEmpty(GecerliRol()))
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. BaseController'dan verileri temiz bir şekilde alıyoruz
            bool isAdmin = GecerliRol() == "Admin";
            int aktifSirketId = GecerliSirketId();

            // 3. HTML (View) tarafında "Yeni Ekle, Sil, Düzenle" butonlarını gizlemek için bunu gönderiyoruz!
            ViewBag.IsAdmin = isAdmin;

            // 4. Eğer Admin ise sayfadaki filtreleme Dropdown'ı için şirket listesini gönderiyoruz
            if (isAdmin)
            {
                ViewBag.Companies = new SelectList(db.Companies.ToList(), "Id", "Name");
            }

            // 5. Veritabanından filtreli çekim
            // Admin ise tüm odalar gelir, personel ise SADECE aktifSirketId'ye uyan odalar gelir.
            var filtreliOdalar = db.Rooms.Where(x => isAdmin || x.CompanyId == aktifSirketId).ToList();

            return View(filtreliOdalar);
        }

        [HttpGet]
        public JsonResult GetRooms()
        {
            if (Session["UserRole"] == null) return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            bool isAdmin = Session["UserRole"].ToString() == "Admin";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

            var odalar = db.Rooms
                           .Where(x => isAdmin || x.CompanyId == aktifSirketId)
                           .Select(x => new
                           {
                               Id = x.Id,
                               Name = x.Name,
                               companyId = x.CompanyId,
                               Capacity = x.Capacity,
                               HasProjector = x.HasProjector,
                           })
                           .ToList();

            return Json(odalar, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Rooms rooms = db.Rooms.Find(id);
            if (rooms == null)
            {
                return HttpNotFound();
            }
            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

            if (!isAdmin && rooms.CompanyId != aktifSirketId)
            {
                return RedirectToAction("Index", "Rooms");
            }
            return PartialView(rooms);
        }

        [HttpGet]
        public ActionResult Create()
        {
            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";

            if (isAdmin)
            {
                ViewBag.CompanyId = new SelectList(db.Companies.ToList(), "Id", "Name");
            }
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,Capacity,HasProjector,CompanyId")] Rooms rooms)
        {
            if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Admin")
            {
                rooms.CompanyId = Convert.ToInt32(Session["CompanyId"]);
            }

            if (ModelState.IsValid)
            {
                db.Rooms.Add(rooms);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    room = rooms
                });
            }

            return Json(new
            {
                success = false,
                message = "Lütfen tüm alanları eksiksiz doldurun."
            });
        }
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Rooms rooms = db.Rooms.Find(id);
            if (rooms == null)
            {
                return HttpNotFound();
            }

            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

            if(!isAdmin && rooms.CompanyId != aktifSirketId)
            {
                return RedirectToAction("Index", "Rooms");
            }
            return PartialView(rooms);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Capacity,HasProjector,CompanyId")] Rooms rooms)
        {
            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

            if (!isAdmin)
            {
                rooms.CompanyId = aktifSirketId;
            }

            if (ModelState.IsValid)
            {
                var gercekOda = db.Rooms.AsNoTracking().FirstOrDefault(x => x.Id == rooms.Id);
                if (gercekOda == null || (!isAdmin && gercekOda.CompanyId != aktifSirketId))
                {
                    return Json(new { success = false, message = "Yetkisiz işlem!" });
                }

                db.Entry(rooms).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Form verileri geçersiz." });
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Rooms rooms = db.Rooms.Find(id);
            if (rooms == null)
            {
                return HttpNotFound();
            }
            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

            if(!isAdmin && rooms.CompanyId != aktifSirketId)
            {
                return RedirectToAction("Index", "Rooms");
            }
            return PartialView(rooms);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            bool isAdmin = Session["UserRole"] != null && Session["UserRole"].ToString() == "Admin";
            int aktifSirketId = Convert.ToInt32(Session["CompanyId"]);

            Rooms rooms = db.Rooms.Find(id);

            if (rooms == null || (!isAdmin && rooms.CompanyId != aktifSirketId))
            {
                return Json(new { success = false, message = "Yetkisiz silme işlemi engellendi!" });
            }

            db.Rooms.Remove(rooms);
            db.SaveChanges();

            return Json(new { success = true });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    
    [HttpGet]
        public JsonResult GetRoomData(int id)
        {
            var oda = db.Rooms.Where(r => r.Id == id).Select(r => new {
                r.Id,
                r.Name,
                r.Capacity,
                r.HasProjector
            }).FirstOrDefault();

            return Json(oda, JsonRequestBehavior.AllowGet);
        }
    }
}