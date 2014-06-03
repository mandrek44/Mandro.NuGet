using System.Linq;
using System.Threading.Tasks;

using NuGet;

namespace Mandro.NuGet
{
    public interface IPackageRepository
    {
        IQueryable<IPackage> GetPackages();
        IQueryable<IWebPackage> GetWebPackages();
        Task AddPackageAsync(IPackage package);
        Task RemovePackageAsync(string packageId, string packageVersion);
    }
}