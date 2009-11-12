﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using Orchard.Logging;
using Orchard.Models;
using Orchard.Services;

namespace Orchard.Security.Providers {
    public class FormsAuthenticationService : IAuthenticationService {
        private readonly IClock _clock;
        private readonly IModelManager _modelManager;
        private readonly HttpContextBase _httpContext;

        public FormsAuthenticationService(IClock clock, IModelManager modelManager, HttpContextBase httpContext) {
            _clock = clock;
            _modelManager = modelManager;
            _httpContext = httpContext;
            Logger = NullLogger.Instance;
            
            // TEMP: who can say...
            ExpirationTimeSpan = TimeSpan.FromHours(6);
        }

        public ILogger Logger { get; set; }

        public TimeSpan ExpirationTimeSpan { get; set; }

        public void SignIn(IUser user, bool createPersistentCookie) {
            var now = _clock.UtcNow.ToLocalTime();
            var userData = Convert.ToString(user.Id);
            
            var ticket = new FormsAuthenticationTicket(
                1 /*version*/,
                user.UserName,
                now,
                now.Add(ExpirationTimeSpan),
                createPersistentCookie,
                userData,
                FormsAuthentication.FormsCookiePath);

            var encryptedTicket = FormsAuthentication.Encrypt(ticket);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
            cookie.HttpOnly = true;
            cookie.Secure = FormsAuthentication.RequireSSL;
            cookie.Path = FormsAuthentication.FormsCookiePath;
            if (FormsAuthentication.CookieDomain != null) {
                cookie.Domain = FormsAuthentication.CookieDomain;
            }

            _httpContext.Response.Cookies.Add(cookie);
        }

        public void SignOut() {
            FormsAuthentication.SignOut();
        }

        public IUser Authenticated() {
            if (!_httpContext.Request.IsAuthenticated || !(_httpContext.User.Identity is FormsIdentity)) {
                return null;
            }

            var formsIdentity = (FormsIdentity)_httpContext.User.Identity;
            var userData = formsIdentity.Ticket.UserData;
            int userId;
            if (!int.TryParse(userData, out userId)) {
                Logger.Fatal("User id not a parsable integer");
            }
            return _modelManager.Get(userId).As<IUser>();
        }
    }
}
