using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Data.OData;

using NuGet;

namespace Mandro.NuGet.Core
{
    internal class ODataPackages
    {
        public static Stream CreatePackagesStream(string baseAddress, IEnumerable<IWebPackage> packages)
        {
            var writerSettings = new ODataMessageWriterSettings()
            {
                Indent = true, // pretty printing
                CheckCharacters = false,
                BaseUri = new Uri("http://localhost:12345"),
                Version = ODataVersion.V3 
            };

            writerSettings.SetContentType(ODataFormat.Atom);

            var responseMessage = new MemoryResposneMessage();
            var writer = new ODataMessageWriter(responseMessage, writerSettings);

            var feedWriter = writer.CreateODataFeedWriter();
            feedWriter.WriteStart(new ODataFeed() { Id = "Packages" });

            foreach (var package in packages)
            {
                feedWriter.WriteStart(MapPackageToEntry(baseAddress, package));
                feedWriter.WriteEnd();
            }

            feedWriter.WriteEnd();
            feedWriter.Flush();

            var stream = responseMessage.GetStream();
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        private static ODataEntry MapPackageToEntry(string baseAddress, IWebPackage package)
        {
            var dtoPackage = MapPackageToDto(package);
            var entryId = "Packages(Id='" + package.Id + "',Version='" + package.Version + "')";

            var oDataEntry = new ODataEntry()
                             {
                                 EditLink = new Uri(baseAddress + entryId, UriKind.Absolute),
                                 Id = baseAddress + entryId,
                                 TypeName = "Package",
                                 MediaResource = new ODataStreamReferenceValue()
                                                 {
                                                     ContentType = "application/zip",
                                                     ReadLink = package.Uri,
                                                 },

                                 Properties = GetProperties(dtoPackage)
                             };

            return oDataEntry;
        }

        private static Package MapPackageToDto(IWebPackage package)
        {
            var tempPackage = new Package()
                              {
                                  Id = package.Id,
                                  Version = package.Version.ToString(),
                                  IsPrerelease = false,
                                  DownloadCount = package.DownloadCount,
                                  RequireLicenseAcceptance = package.RequireLicenseAcceptance,
                                  DevelopmentDependency = package.DevelopmentDependency,
                                  Description = package.Description,
                                  Published = package.Published != null ? package.Published.Value.UtcDateTime : new DateTime(2014, 1, 1),
                                  LastUpdated = new DateTime(2014, 1, 1),
                                  PackageHash = package.GetHash("SHA512"),
                                  PackageHashAlgorithm = "SHA512",
                                  PackageSize = package.GetStream().Length,
                                  IsAbsoluteLatestVersion = package.IsAbsoluteLatestVersion,
                                  IsLatestVersion = package.IsLatestVersion,
                                  Listed = package.Listed,
                                  VersionDownloadCount = package.DownloadCount
                              };
            return tempPackage;
        }

        private static IEnumerable<ODataProperty> GetProperties(object obj)
        {
            return obj.GetType().GetProperties().Select(property => new ODataProperty() { Name = property.Name, Value = property.GetValue(obj) }).ToArray();
        }

        public static Stream CreatePackageStream(string baseAddress, IWebPackage package)
        {
            var writerSettings = new ODataMessageWriterSettings()
            {
                Indent = true, // pretty printing
                CheckCharacters = false,
                BaseUri = new Uri("http://localhost:12345"),
                Version = ODataVersion.V3
            };

            writerSettings.SetContentType(ODataFormat.Atom);

            var responseMessage = new MemoryResposneMessage();
            var writer = new ODataMessageWriter(responseMessage, writerSettings);

            var feedWriter = writer.CreateODataEntryWriter();
                feedWriter.WriteStart(MapPackageToEntry(baseAddress, package));
                feedWriter.WriteEnd();
            feedWriter.Flush();

            var stream = responseMessage.GetStream();
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
    }
}