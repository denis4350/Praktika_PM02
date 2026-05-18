using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Jwt;
using Owin;
using System;
using System.Configuration;
using System.Text;

[assembly: OwinStartup(typeof(SZR_Production_API.Startup))]

namespace SZR_Production_API
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            string secret = ConfigurationManager.AppSettings["JwtSecret"];
            string issuer = ConfigurationManager.AppSettings["JwtIssuer"];
            string audience = ConfigurationManager.AppSettings["JwtAudience"];

            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            {
                throw new InvalidOperationException("JwtSecret должен быть указан в Web.config и быть не короче 32 символов.");
            }

            if (string.IsNullOrWhiteSpace(issuer))
            {
                throw new InvalidOperationException("JwtIssuer должен быть указан в Web.config.");
            }

            if (string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException("JwtAudience должен быть указан в Web.config.");
            }

            byte[] key = Encoding.UTF8.GetBytes(secret);

            app.UseJwtBearerAuthentication(new JwtBearerAuthenticationOptions
            {
                AuthenticationMode = AuthenticationMode.Active,

                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    ValidateAudience = true,
                    ValidAudience = audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }
            });
        }
    }
}