using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using NuGet;

using MemoryCache = System.Runtime.Caching.MemoryCache;

namespace Mandro.NuGet
{
    public class AzureBlobPackageRepository : IPackageRepository
    {
        private readonly Lazy<MemoryCache> _lazyMemoryCache;
        private readonly Lazy<CloudBlobContainer> _blogFilesContainer;

        private readonly string _packagesBlob;
        private readonly string _connectionString;

        public AzureBlobPackageRepository(string containerName, string connectionString)
        {
            _packagesBlob = containerName;
            _connectionString = connectionString;

            _blogFilesContainer = new Lazy<CloudBlobContainer>(
                () =>
                {
                    var container = CloudStorageAccount.Parse(_connectionString)
                        .CreateCloudBlobClient()
                        .GetContainerReference(_packagesBlob);

                    if (container.CreateIfNotExists())
                    {
                        var permissions = container.GetPermissions();
                        permissions.PublicAccess = BlobContainerPublicAccessType.Container;

                        container.SetPermissions(permissions);
                    }

                    return container;
                });


            _lazyMemoryCache = new Lazy<MemoryCache>(
                () =>
                {
                    var cache = MemoryCache.Default;
                    var packages = BlogFilesContainer.ListBlobs().OfType<CloudBlockBlob>().Select(SafeReadZipPackage).Where(package => package != null);

                    foreach (var package in packages)
                    {
                        cache.Add(package.Id, package, DateTimeOffset.Now.AddYears(1));
                    }

                    return cache;
                });
        }

        public string Source
        {
            get
            {
                return _packagesBlob;
            }
        }

        public PackageSaveModes PackageSaveMode { get; set; }

        public bool SupportsPrereleasePackages
        {
            get
            {
                return false;
            }
        }

        public IQueryable<IPackage> GetPackages()
        {
            return Cache.Select(keyValue => keyValue.Value as IPackage).AsQueryable();
        }

        public IQueryable<IWebPackage> GetWebPackages()
        {
            return GetPackages().OfType<IWebPackage>();
        }

        public async Task AddPackageAsync(IPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            if (package is ZipPackage)
                (package as ZipPackage).Published = DateTimeOffset.Now;

            var blockBlobReference = BlogFilesContainer.GetBlockBlobReference(GetPackageKey(package));
            await blockBlobReference.UploadFromStreamAsync(package.GetStream());

            UpdateCacheAfterAdd(new WebZipPackage(package.GetStream(), blockBlobReference.Uri));
        }

        public async Task RemovePackageAsync(string packageId, string packageVersion)
        {
            if (packageId == null)
            {
                throw new ArgumentNullException("packageId");
            }
            else if (packageVersion == null)
            {
                throw new ArgumentNullException("packageVersion");
            }

            Cache.Remove(packageId);
            await BlogFilesContainer.GetBlockBlobReference(GetPackageKey(packageId, packageVersion)).DeleteAsync();
        }

        private MemoryCache Cache
        {
            get
            {
                return _lazyMemoryCache.Value;
            }
        }

        private CloudBlobContainer BlogFilesContainer
        {
            get
            {
                return _blogFilesContainer.Value;
            }
        }

        private void UpdateCacheAfterAdd(IPackageName package)
        {
            var cachedObject = Cache[package.Id];
            if (cachedObject != null)
            {
                if (((IPackage)cachedObject).Version < package.Version)
                {
                    Cache[package.Id] = package;
                }
            }
            else
            {
                Cache[package.Id] = package;
            }
        }

        private static string GetPackageKey(IPackageName package)
        {
            return GetPackageKey(package.Id, package.Version.ToString());
        }

        private static string GetPackageKey(string packageId, string packageVersion)
        {
            return packageId + "-" + packageVersion + ".nupkg";
        }

        private static WebZipPackage SafeReadZipPackage(CloudBlockBlob blob)
        {
            try
            {
                return new WebZipPackage(blob.OpenRead(), blob.Uri);
            }
            catch (Exception)
            {
                return null;
            }
        }

        //public async Task<string>  TEst
        //{
        //    get
        //    {
        //        await Task.FromResult("Ble");
        //    }
        //}
    }
}