using System.Web.Http;
using WebActivatorEx;
using SZR_Production_API;
using Swashbuckle.Application;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace SZR_Production_API
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "SZR_Production_API");

                    // РАСКОММЕНТИРУЙТЕ ЭТУ СТРОКУ
                    c.UseFullTypeNameInSchemaIds();

                    // ДОБАВЬТЕ ЭТО - НАСТРОЙКА JWT
                    c.ApiKey("Bearer")
                        .Description("JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token")
                        .Name("Authorization")
                        .In("header");
                })
                .EnableSwaggerUi(c =>
                {
                    // ДОБАВЬТЕ ЭТО - ВКЛЮЧАЕМ API KEY SUPPORT
                    c.EnableApiKeySupport("Authorization", "header");
                });
        }
    }
}