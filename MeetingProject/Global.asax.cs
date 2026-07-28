using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Security.Principal;

namespace MeetingProject
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        protected void Application_PostAuthenticateRequest(Object sender, EventArgs e)
        {
            var authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                var authTicket = FormsAuthentication.Decrypt(authCookie.Value);
                if (authTicket != null && !authTicket.Expired)
                {
                    var userDataParts = authTicket.UserData.Split('|');

                    var roles = userDataParts[0].Split(',');

                    if (userDataParts.Length > 2)
                    {
                        HttpContext.Current.Items["UserFullName"] = userDataParts[1];
                        HttpContext.Current.Items["CompanyId"] = userDataParts[2]; 
                    }
                    else
                    {
                        HttpContext.Current.Items["UserFullName"] = authTicket.Name; // Yedeğimiz
                    }

                    HttpContext.Current.User = new GenericPrincipal(new GenericIdentity(authTicket.Name), roles);
                }
            }
        }
    }
}