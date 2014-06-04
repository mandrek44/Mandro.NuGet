using System;
using System.IO;

using NuGet;

namespace Mandro.NuGet.Core
{
    public class WebZipPackage : ZipPackage, IWebPackage
    {
        public Uri Uri { get; private set; }

        public WebZipPackage(string filePath)
            : base(filePath)
        {
        }

        public WebZipPackage(Stream stream, Uri uri)
            : base(stream)
        {
            Uri = uri;
        }
    }
}