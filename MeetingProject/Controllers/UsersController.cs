using System.Linq;
using System.Web.Mvc;
using MeetingProject.Models; 

namespace MeetingProject.Controllers
{
    public class UsersController : Controller
    {
        private MeetingAppEntities1 db = new MeetingAppEntities1();

        public ActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            return View();
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            return View();
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetUsers()
        {
            var userList = db.Users.Select(u => new {
                u.Id,
                u.Name,
                u.Surname,
                u.Email,
                u.Department
            }).ToList();

            return Json(userList, JsonRequestBehavior.AllowGet);
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
        public JsonResult Create(Users model) 
        {
            try
            {
                db.Users.Add(model);
                db.SaveChanges(); 
                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public JsonResult Edit(Users model)
        {
            try
            {
                var guncellenecekKullanici = db.Users.Find(model.Id);

                if (guncellenecekKullanici != null)
                {
                    guncellenecekKullanici.Name = model.Name;
                    guncellenecekKullanici.Surname = model.Surname;
                    guncellenecekKullanici.Email = model.Email;
                    guncellenecekKullanici.Department = model.Department;

                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost, ActionName("Delete")]
        public JsonResult DeleteConfirmed(int id)
        {
            try
            {
                var silinecekKullanici = db.Users.Find(id);

                if (silinecekKullanici != null)
                {
                    db.Users.Remove(silinecekKullanici);
                    db.SaveChanges();

                    return Json(new { success = true });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }
    }
}