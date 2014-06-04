using System;

using NuGet;

namespace Mandro.NuGet.Core
{
    public interface IWebPackage : IPackage
    {
        Uri Uri { get; }
    }
}