using System;

using NuGet;

namespace Mandro.NuGet
{
    public interface IWebPackage : IPackage
    {
        Uri Uri { get; }
    }
}