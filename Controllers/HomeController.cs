using Hackathon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Hackathon.Controllers
{
    public class HomeController : Controller
    {
        private SatoriDataExtract satoriDataExtract;
        public ActionResult Index()
        {
            return View();
        }
        public HomeController() : this(new SatoriDataExtract())
        {

        }
        public HomeController(SatoriDataExtract repository)
        {
            satoriDataExtract = repository;
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        [HttpGet]
        public ActionResult GetTestRuleResults()
        {
            
            // TODO: take care of escape characters in the result
            return Json(satoriDataExtract.ExtractFileData(), JsonRequestBehavior.AllowGet);
        }
    }
}
