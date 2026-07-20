using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MeetingProject.Models;

namespace MeetingProject.Controllers
{
    public class RoomsController : Controller
    {
        private MeetingAppEntities1 db = new MeetingAppEntities1();

        public ActionResult Index()
        {
            return View(db.Rooms.ToList());
        }
        
        [HttpGet]
        public JsonResult GetRooms()
        {
            var roomList = db.Rooms.Select(r => new {
                r.Id,
                r.Name,
                r.Capacity,
                r.HasProjector
            }).ToList();

            return Json(roomList, JsonRequestBehavior.AllowGet);
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
            return PartialView(rooms);
        }

        public ActionResult Create()
        {
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,Capacity,HasProjector")] Rooms rooms)
        {
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
                success = false
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
            return PartialView(rooms);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Capacity,HasProjector")] Rooms rooms)
        {
            if (ModelState.IsValid)
            {
                db.Entry(rooms).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true });
            }
            
            return Json(new { success = false });
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
            return PartialView(rooms);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Rooms rooms = db.Rooms.Find(id);
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