using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Chronos.Application.Authentication;
using Chronos.Application.Authentication.Dto;
using Chronos.Web.Common;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Chronos.Web.Authentication
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoginMemoryCache _loginMemoryCache;

        public AuthenticationMiddleware(
            RequestDelegate next, 
            ILoginMemoryCache loginMemoryCache)
        {
            _next = next;
            _loginMemoryCache = loginMemoryCache;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path == "/login" && context.Request.Query.ContainsKey("key"))
            {
                var key = Guid.Parse(context.Request.Query["key"]);
                if (!_loginMemoryCache.TryGetValue(key, out var login))
                {
                    context.Response.Redirect("/error");
                    return;
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, login.Username)
                };
                if (login.Password != null)
                {
                    claims.Add(new Claim(ClaimTypes.UserData, login.Password));
                }
                if (login.PersonalAccessToken != null)
                {
                    claims.Add(new Claim(nameof(LoginDto.PersonalAccessToken), login.PersonalAccessToken));
                }
                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);
                
                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        ExpiresUtc = DateTime.UtcNow.AddDays(30),
                        IsPersistent = true,
                        AllowRefresh = true
                    });

                _loginMemoryCache.TryRemove(key, out _);
                context.Response.Redirect(WebConstants.Pages.Home);
                return;

            }
            else if (context.Request.Path == "/logout")
            {
                await context.SignOutAsync();
                context.Response.Redirect("/");
                return;
            }
            else
            {
                await _next.Invoke(context);
            }
        }
    }
}
