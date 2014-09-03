using Mandro.NuGet.Core;

using Microsoft.Owin.Hosting;

using Owin;

namespace Mandro.NuGet.Console
{
    public class Startup
    {
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>("http://localhost:8080"))
            {
                System.Console.ReadLine();
            }
        }

        public void Configuration(IAppBuilder app)
        {
            app.Use(
                async (context, next) =>
                {
                    System.Console.WriteLine("{0} {1}{2}", context.Request.Method, context.Request.Path, context.Request.QueryString);
                    await next();
                });

            var repository = new AzureBlobPackageRepository(
                "nugetfiles",
                "DefaultEndpointsProtocol=https;AccountName=mandrostorage;AccountKey=Qvbyl+ZI1Sz8b06vNvD2FfwvTewF8TOJI6i0zKUbNa5QDnFf6fw6t9kasoI8FO7hghRyOfBUjPPAEu5g1x9voQ==");

            var nuGetServer = new NuGetServerMiddleware(repository, "MandroNuGetApiKey");
            app.Use(nuGetServer.Invoke);
        }
    }
}
