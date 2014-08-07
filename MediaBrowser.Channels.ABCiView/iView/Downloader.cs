using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
//using System.Runtime.Serialization.Json;
using System.Threading;
using System.IO;
using MediaBrowser.Channels.ABCiView.iViewEntities;

namespace MediaBrowser.Channels.ABCiView.iView
{
    public static class Downloader
    {
        const string ConfigUrl = "http://www.abc.net.au/iview/xml/config.xml";

        internal static async Task<ConfigInfo> GetConfig(IHttpClient httpClient,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            using (var stream = await MakeHttpRequest(httpClient, logger, cancellationToken, ConfigUrl).ConfigureAwait(false))
            {
                return GetConfigInfo(stream);
            }
        }

        private static ConfigInfo GetConfigInfo(Stream stream)
        {
            XDocument confDoc = XDocument.Load(stream);
            ConfigInfo result = new ConfigInfo();


            result.ApiUrl = GetConfigValueVale(confDoc, "api");
            result.AuthUrl = GetConfigValueVale(confDoc, "auth");
            result.CategoriesUrl = "http://www.abc.net.au/iview/" + GetConfigValueVale(confDoc, "categories");
            result.ServerStreamngUrl = GetConfigValueVale(confDoc, "server_streaming");
            result.ServerFallbackUrl = GetConfigValueVale(confDoc, "server_fallback");

            return result;
        }

        private static string GetConfigValueVale(XDocument confDoc,
                                                 string attributeNameValue)
        {
            IEnumerable<XElement> matchingEls = confDoc.Descendants("param").Where(el => el.Attributes("name").Where(att => att.Value == attributeNameValue).Any());
            return matchingEls.First().Attribute("value").Value;
        }

        internal static async Task<List<Category>> GetCategories(IHttpClient httpClient,
            ILogger logger,
            CancellationToken cancellationToken,
            ConfigInfo config)
        {
            using (var stream = await MakeHttpRequest(httpClient, logger, cancellationToken, config.CategoriesUrl).ConfigureAwait(false))
            {
                return GetCategories( stream);
            }
        }
        private static List<Category> GetCategories(Stream stream)
        {
            ////return (RootCategories)xmlSerializer.DeserializeFromStream(typeof(RootCategories), stream);
            ////List<Category> result = new List<Category>();

            ////http://www.abc.net.au/iview/xml/config.xml
            //XDocument doc = XDocument.Load(stream);
            ////IEnumerable<XElement> matchingEls = doc.Descendants("category").Where(el => el.Attributes("genre").Where(att => att.Value == "true").Any());
            //var result = new List<Category>();
            //IEnumerable<XElement> matchingEls = doc.Element("categories").Elements("category");
            //foreach (var item in matchingEls)
            //{
            //    result.Add(new Category() { id = item.Attribute("id").Value, name = item.Element("name").Value });
            //}
            //return result;

            //http://www.abc.net.au/iview/xml/config.xml
            XDocument doc = XDocument.Load(stream);
            XElement rootCategories = doc.Element("categories");
            return GetCategories(rootCategories);
        }
        private static List<Category> GetCategories(XElement item)
        {
            if (item == null) return null;

            var result = new List<Category>();
            IEnumerable<XElement> matchingEls = item.Elements("category");
            foreach (var i in matchingEls)
            {
                if (i != null) 
                    result.Add(GetCategory(i));
            }
            return result;
        }
        private static Category GetCategory(XElement item)
        {
            if (item == null) return null;

            var result = new Category() { id = item.Attribute("id").Value, name = item.Element("name").Value };
            result.genre = GetOptionalBooleanAttribute(item, "genre");
            result.index = GetOptionalBooleanAttribute(item, "index");

            result.Categories = GetCategories(item);
            return result;
        }
        private static bool? GetOptionalBooleanAttribute(XElement item, String attributeName) 
        {
            var attribute = item.Attribute(attributeName);
            if (attribute != null)
            {
                var attributeValue = attribute.Value;
                bool attributeBool;
                if (bool.TryParse(attributeValue, out attributeBool))
                    return attributeBool;
            }
            return null;
        }

        internal static async Task<List<SeriesIndex>> GetAllSeriesIndexes(IHttpClient httpClient,
            IJsonSerializer jsonSerializer,
            ILogger logger,
            CancellationToken cancellationToken,
            ConfigInfo config)
        {
            string seriesIndexUrl = config.ApiUrl + "seriesIndex";
            using (var stream = await MakeHttpRequest(httpClient, logger, cancellationToken, seriesIndexUrl).ConfigureAwait(false))
            {
                return GetSeriesIndex(jsonSerializer, stream);
            }
        }
        private static List<SeriesIndex> GetSeriesIndex(IJsonSerializer jsonSerializer, Stream stream)
        {
            return jsonSerializer.DeserializeFromStream<List<SeriesIndex>>(stream);
        }

        internal static async Task<List<Series>> GetSeriesDetail(IHttpClient httpClient,
            IJsonSerializer jsonSerializer,
            ILogger logger,
            CancellationToken cancellationToken,
            ConfigInfo config,
            BaseSeries series)
        {
            string seriesUrl = config.ApiUrl + "series=" + series.a;
            using (var stream = await MakeHttpRequest(httpClient, logger, cancellationToken, seriesUrl).ConfigureAwait(false))
            {
                return GetSeriesDetail(jsonSerializer, stream);
            }
        }

        private static List<Series> GetSeriesDetail(IJsonSerializer jsonSerializer, Stream stream)
        {
            return jsonSerializer.DeserializeFromStream<List<Series>>(stream);
        }

        internal static async Task<Stream> MakeHttpRequest(IHttpClient httpClient,
            ILogger logger,
            CancellationToken cancellationToken,
            string url)
        {
            return await httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                ResourcePool = Plugin.Instance.ABCConfig,
                UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.28 Safari/537.36"

            });
        }


        internal static async Task<AuthInfo> GetAuthToken(IHttpClient httpClient,
            IJsonSerializer jsonSerializer,
            ILogger logger,
            CancellationToken cancellationToken,
            ConfigInfo config)
        {
            using (var stream = await MakeHttpRequest(httpClient, logger, cancellationToken, config.AuthUrl).ConfigureAwait(false))
            {
                XDocument doc = XDocument.Load(stream);
                XNamespace xmlns = "http://www.abc.net.au/iView/Services/iViewHandshaker";
                return new AuthInfo() 
                { 
                    Token = doc.Descendants(xmlns + "token").First().Value,
                    TokenHD = doc.Descendants(xmlns + "tokenhd").First().Value,
                    Server = doc.Descendants(xmlns + "server").First().Value,
                    Path = doc.Descendants(xmlns + "path").First().Value,
                    Host = doc.Descendants(xmlns + "host").First().Value
                };
            }
        }

        //public static void DownloadProgram(ConfigInfo config,
        //                                    Program program)
        //{
        //    HostworksMP4Download("", GetAuthToken(config), System.IO.Path.GetFileNameWithoutExtension(program.n), program.n);

        //}

        //private static void HostworksMP4Download(String serverStreaming,
        //                                 String authToken,
        //                                 String programID,
        //                                 String outputFileName)
        //{
        //    string rtmpDumpFullPath = "\"C:\\_Source Code\\Uncontrolled\\iViewDownloader\\iViewDownloader\\iViewDownloader\\Libs\\rtmpdump.exe\"";
        //    string downloadPath = "C:\\_Source Code\\Uncontrolled\\iViewDownloader\\iViewDownloader\\iViewDownloader\\Downloads\\" + outputFileName + ".mp4";

        //    string args = String.Format(" -r rtmp://203.18.195.10/ -a ondemand?auth=\"{2}&mp4:{3}\" -y \"mp4:{3}\" -o \"{4}\" -W \"http://www.abc.net.au/iview/images/iview.jpg\" -e -V",
        //                            rtmpDumpFullPath,
        //                            serverStreaming,
        //                            authToken,
        //                            programID,
        //                            downloadPath);

        //    using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
        //    {
        //        proc.StartInfo = new System.Diagnostics.ProcessStartInfo(rtmpDumpFullPath, args)
        //        {
        //            UseShellExecute = false,
        //            RedirectStandardOutput = true,
        //            RedirectStandardError = true
        //        };
        //        if (proc.Start())
        //        {

        //        }
        //        proc.WaitForExit();
        //    }
        //}
    }

}
