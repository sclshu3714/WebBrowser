using CefSharp;
using CefSharp.Handler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JFBrowser.Common
{
    //DefaultResourceHandler的构造可以放在IRequestHandler的实现类的GetResourceRequestHandler方法内
    public class DefaultResourceHandler : ResourceRequestHandler
    {
        protected override IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            if (response.MimeType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return JsonResponseFilter.CreateFilter(request.Identifier.ToString());
            }
            return null;
        }

        protected override void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
            var filter = JsonResponseFilter.GetFileter(request.Identifier.ToString()) as JsonResponseFilter;
            if (filter != null)
            {
                var encode = !string.IsNullOrEmpty(response.Charset)
                    ? Encoding.GetEncoding(response.Charset)
                    : Encoding.UTF8;
                using (var read = new StreamReader(filter.GetStream(), encode))
                {
                    var text = read.ReadToEnd();
                    Debug.WriteLine(text);
                }
            }
        }
    }

    public class JsonResponseFilter : IResponseFilter
    {
        private MemoryStream Stream;

        public JsonResponseFilter()
        {
            Stream = new MemoryStream();
        }

        public FilterStatus Filter(System.IO.Stream dataIn, out long dataInRead, System.IO.Stream dataOut, out long dataOutWritten)
        {
            try
            {
                if (dataIn == null || dataIn.Length == 0)
                {
                    dataInRead = 0;
                    dataOutWritten = 0;

                    return FilterStatus.Done;
                }
                dataInRead = dataIn.Length;
                dataOutWritten = Math.Min(dataInRead, dataOut.Length);

                dataIn.CopyTo(dataOut);
                dataIn.Seek(0, SeekOrigin.Begin);
                byte[] bs = new byte[dataIn.Length];
                dataIn.Read(bs, 0, bs.Length);

                Stream.Write(bs, 0, bs.Length);

                dataInRead = dataIn.Length;
                dataOutWritten = dataIn.Length;

                return FilterStatus.NeedMoreData;
            }
            catch (Exception)
            {
                dataInRead = dataIn.Length;
                dataOutWritten = dataIn.Length;

                return FilterStatus.Done;
            }
        }

        public bool InitFilter()
        {
            return true;
        }

        public Stream GetStream()
        {
            Stream.Seek(0, SeekOrigin.Begin);
            return Stream;
        }

        public void Dispose()
        {
        }

        private static Dictionary<string, IResponseFilter> _dictionary = new Dictionary<string, IResponseFilter>();

        public static IResponseFilter CreateFilter(string id)
        {
            var filter = new JsonResponseFilter();
            _dictionary[id] = filter;
            return filter;
        }

        public static IResponseFilter GetFileter(string id)
        {
            if (_dictionary.ContainsKey(id))
            {
                var filter = _dictionary[id];
                _dictionary.Remove(id);
                return filter;
            }
            return null;
        }
    }
}
