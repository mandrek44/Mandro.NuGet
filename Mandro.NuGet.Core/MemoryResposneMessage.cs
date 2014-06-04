using System.Collections.Generic;
using System.IO;

using Microsoft.Data.OData;

namespace Mandro.NuGet.Core
{
    internal class MemoryResposneMessage : IODataResponseMessage
    {
        readonly Dictionary<string, string> _headers = new Dictionary<string, string>();

        private readonly MemoryStream _memoryStream;

        public MemoryResposneMessage()
        {
            _memoryStream = new MemoryStream();
        }

        public string GetHeader(string headerName)
        {
            if (_headers.ContainsKey(headerName)) return _headers[headerName];
            return string.Empty;
        }

        public void SetHeader(string headerName, string headerValue)
        {
            _headers[headerName] = headerValue;
        }

        public Stream GetStream()
        {
            return _memoryStream;
        }

        public IEnumerable<KeyValuePair<string, string>> Headers
        {
            get
            {
                return _headers;
            }
        }

        public int StatusCode { get; set; }
    }
}
