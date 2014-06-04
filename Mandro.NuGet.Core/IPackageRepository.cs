using System.Linq;
using System.Threading.Tasks;

using NuGet;

namespace Mandro.NuGet.Core
{
    public interface IPackageRepository
    {
        IQueryable<IPackage> GetPackages();
        IQueryable<IWebPackage> GetWebPackages();
        Task AddPackageAsync(IPackage package);
        Task RemovePackageAsync(string packageId, string packageVersion);

        IWebPackage GetPackage(string id, string version);
    }
}