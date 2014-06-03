using System;

using Microsoft.Owin.Hosting;

using Owin;

namespace Mandro.NuGet
{
    public class Startup
    {
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>("http://localhost:8080"))
            {
                Console.ReadLine();
            }
        }

        public void Configuration(IAppBuilder app)
        {
            app.Use(
                async (context, next) =>
                {
                    Console.WriteLine("{0} {1}", context.Request.Method, context.Request.Path);
                    await next();
                });

            var repository = new AzureBlobPackageRepository(
                "nugetfiles",
                "DefaultEndpointsProtocol=https;AccountName=mandrostorage;AccountKey=Qvbyl+ZI1Sz8b06vNvD2FfwvTewF8TOJI6i0zKUbNa5QDnFf6fw6t9kasoI8FO7hghRyOfBUjPPAEu5g1x9voQ==");

            var nuGetServer = new NuGetServerMiddleware(repository, "MandroNuGetApiKey");
            app.Use(nuGetServer.Invoke);

#if DEBUG
            app.UseErrorPage();
#endif
            app.UseWelcomePage("/");
        }
    }
}