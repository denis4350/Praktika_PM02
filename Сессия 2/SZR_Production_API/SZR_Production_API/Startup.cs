using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Jwt;
using Newtonsoft.Json.Serialization;
using Owin;
using Swashbuckle.Application;
using System;
using System.Text;
using System.Web.Http;

[assembly: OwinStartup(typeof(SZR_Production_API.Startup))]

namespace SZR_Production_API
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Настройка Web API
            HttpConfiguration config = new HttpConfiguration();

            // Маршруты
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Формат JSON
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver =
                new CamelCasePropertyNamesContractResolver();

            // Настройка JWT
            ConfigureJwt(app);

            // CORS
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);

            // Web API
            app.UseWebApi(config);

            // Swagger
            ConfigureSwagger(config);
        }

        private void ConfigureJwt(IAppBuilder app)
        {
            string secretKey = "super-secret-key-for-szr-production-2024-min-32-characters";
            byte[] key = Encoding.ASCII.GetBytes(secretKey);

            app.UseJwtBearerAuthentication(new JwtBearerAuthenticationOptions
            {
                AuthenticationMode = AuthenticationMode.Active,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }
            });
        }

        private void ConfigureSwagger(HttpConfiguration config)
        {
            config.EnableSwagger(c =>
            {
                c.SingleApiVersion("v1", "SZR Production API");
                c.ApiKey("Bearer")
                    .Description("JWT Authorization header using the Bearer scheme")
                    .Name("Authorization")
                    .In("header");
            }).EnableSwaggerUi();
        }
    }
}