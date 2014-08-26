using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Mandro.Utils.Web;

using Microsoft.Owin;

using NuGet;

namespace Mandro.NuGet.Core
{
    public class NuGetServerMiddleware
    {
        private static string _apiKey;
        private readonly IPackageRepository _repository;
        private const string ApiKeyHeader = "X-NUGET-APIKEY";

        public NuGetServerMiddleware(IPackageRepository repository, string apiKey)
        {
            _apiKey = apiKey;
           _repository = repository;
        }

        public async Task Invoke(IOwinContext context, Func<Task> next)
        {
            if (context.Request.Method.ToUpper() == "PUT" && (context.Request.Path.Value == "/api/v2/package/" || context.Request.Path.Value == "/"))
            {
                await HandleUpload(context);
            }
            else if (context.Request.Method.ToUpper() == "GET" && context.Request.Path.Value.StartsWith("/Packages"))
            {
                var match = Regex.Match(context.Request.Path.Value, @"/Packages\(Id='(?<id>.+?)',Version='(?<version>.+?)'\)");
                if (!match.Success)
                {
                    await HandleListing(context);
                }
                else
                {
                    await HandlePackageDetails(context, match.Groups["id"].Value, match.Groups["version"].Value);                    
                }
            }
            else if (context.Request.Method == "GET" && context.Request.Path.Value.StartsWith("/FindPackagesById()"))
            {
                var id = context.Request.Query["id"].Replace("'", string.Empty);
                var package = _repository.GetWebPackages().Where(pkg => pkg.Id == id).OrderByDescending(pkg => pkg.Version).FirstOrDefault();

                if (package != null)
                    await HandlePackageDetails(context, package.Id, package.Version.ToString());
            }
            else if (context.Request.Method.ToUpper() == "DELETE")
            {
                await HandleDelete(context);
            }
        }

        private async Task HandlePackageDetails(IOwinContext context, string id, string version)
        {
            var webPackage = _repository.GetPackage(id, version);
            if (webPackage == null)
            {
                return;
            }
            
            var stream = ODataPackages.CreatePackageStream(context.Request.Uri.Scheme + "://" + context.Request.Uri.Host + ":" + context.Request.Uri.Port + "/", webPackage);
            context.Response.ContentType = "application/atom+xml; charset=utf-8";
            await stream.CopyToAsync(context.Response.Body);
        }

        private async Task HandleDelete(IOwinContext context)
        {
            var request = context.Request.Path.Value;
            if (request.StartsWith("/api/v2/package"))
            {
                request = request.Remove(0, "/api/v2/package".Length);
            }

            var parts = request.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var packageId = parts[0];
                var packageVersion = parts[1];

                if (!await Authenticate(context, packageId))
                {
                    return;
                }

                await _repository.RemovePackageAsync(packageId, packageVersion);
            }
        }

        private async Task HandleUpload(IOwinContext context)
        {
            var dataStream = new MultiPartFormDataStream(context.Request.Body, context.Request.ContentType);
            if (dataStream.SeekNextFile())
            {
                var zipPackage = new ZipPackage(dataStream);

                if (!await Authenticate(context, zipPackage.Id))
                {
                    return;
                }

                await _repository.AddPackageAsync(zipPackage);
            }
        }

        private static async Task<bool> Authenticate(IOwinContext context, string packageId)
        {
            if (context.Request.Headers[ApiKeyHeader] == _apiKey)
            {
                return true;
            }

            context.Response.StatusCode = 403;
            await context.Response.WriteAsync(string.Format("Access denied for package '{0}'.", packageId));
            return false;
        }

        private async Task HandleListing(IOwinContext context)
        {
            var baseAddress = context.Request.Uri.Scheme + "://" + context.Request.Uri.Host + ":" + context.Request.Uri.Port + "/";
            var stream = ODataPackages.CreatePackagesStream(baseAddress, _repository.GetWebPackages());

            context.Response.ContentType = "application/atom+xml; charset=utf-8";
            await stream.CopyToAsync(context.Response.Body);
        }
    }
}