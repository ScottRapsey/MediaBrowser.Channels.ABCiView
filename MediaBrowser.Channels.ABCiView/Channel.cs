using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Channels.ABCiView
{
    class Channel : IChannel, IHasCacheKey, IRequiresMediaInfoCallback
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IXmlSerializer _xmlSerializer;

        public Channel(IHttpClient httpClient, IJsonSerializer jsonSerializer, IXmlSerializer xmlSerializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;
            _xmlSerializer = xmlSerializer;
        }

        public string DataVersion
        {
            get
            {
                // Increment as needed to invalidate all caches
                return "1";
            }
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            _logger.Debug("cat ID : " + query.FolderId);

            var items = new List<ChannelItemInfo>();

            //TODO cache config for 4-6 hrs instead of loading it each time
            var config = await iView.Downloader.GetConfig(this._httpClient, this._logger, cancellationToken);
            //TODO cache categories for 2-4 hrs instead of loading them each time
            var categories = await iView.Downloader.GetCategories(this._httpClient, this._logger, cancellationToken, config);

            if (string.IsNullOrWhiteSpace(query.FolderId))
            {
                //top/root level
                items.AddRange(GetChannelItemInfo(categories));
                //there is no series stuff to do in root
            }
            else
            {
                //could be a category or a series 

                var category = categories.FirstOrDefaultDecenant(i => i.id == query.FolderId);
                if (category != null)
                {
                    items.AddRange(GetChannelItemInfo(category.Categories));
                    var allSeriesIndexes = await iView.Downloader.GetAllSeriesIndexes(this._httpClient, this._jsonSerializer, this._logger, cancellationToken, config);
                    var matchingSeries = GetSeriesIndexes(category, allSeriesIndexes);
                    items.AddRange(GetChannelItemInfo(matchingSeries));
                }
                else
                {
                    //probably a series Index

                    //todo cache this somehow for 15-30mins
                    var allSeriesIndexes = await iView.Downloader.GetAllSeriesIndexes(this._httpClient, this._jsonSerializer, this._logger, cancellationToken, config);
                    var seriesIndex = allSeriesIndexes.FirstOrDefault(i => i.a == query.FolderId);
                    if (seriesIndex != null)
                    {
                        var matchingSeries = await iView.Downloader.GetSeriesDetail(this._httpClient, this._jsonSerializer, this._logger, cancellationToken, config, seriesIndex);
                        items.AddRange(GetChannelItemInfo(matchingSeries, config));
                    }
                    else
                    {
                        //probably a series
                        

                    }
                }

            }
            //foreach (var s in Plugin.Instance.Configuration.streams)
            //{
            //    var item = new ChannelItemInfo
            //    {
            //        Name = s.Name,
            //        ImageUrl = s.Image,
            //        Id = s.Name,
            //        Type = ChannelItemType.Media,
            //        ContentType = ChannelMediaContentType.Clip,
            //        MediaType = ChannelMediaType.Video,

            //        MediaSources = new List<ChannelMediaInfo>
            //        {
            //            new ChannelMediaInfo
            //            {
            //                Path = s.URL,
            //                Protocol = (s.Type == "RTMP" ? MediaProtocol.Rtmp : MediaProtocol.Http) 
            //            }  
            //        }
            //    };

            //    items.Add(item);
            //}
            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        private IEnumerable<ChannelItemInfo> GetChildrenChannelItemInfo(iViewEntities.Category item)
        {
            return GetChannelItemInfo(item.Categories);
        }
        private IEnumerable<ChannelItemInfo> GetChannelItemInfo( IEnumerable<iViewEntities.Category> items)
        {
            return items.Select(i => GetChannelItemInfo(i));
        }
        private ChannelItemInfo GetChannelItemInfo(iViewEntities.Category item)
        {
            return new ChannelItemInfo()
            {
                Id = item.id,
                Name = item.name,
                Type = ChannelItemType.Folder
            };
        }
        private IEnumerable<ChannelItemInfo> GetChannelItemInfo(IEnumerable<iViewEntities.SeriesIndex> items)
        {
            return items.Select(i => GetChannelItemInfo(i));
        }
        private ChannelItemInfo GetChannelItemInfo(iViewEntities.SeriesIndex item)
        {
            return new ChannelItemInfo()
            {
                Id = item.a,
                Name = item.b,
                Tags = (item.e == null) ? null : item.e.Split(' ').ToList(),
                Type = ChannelItemType.Folder
            };
        }
        private IEnumerable<ChannelItemInfo> GetChannelItemInfo(IEnumerable<iViewEntities.Series> items, iViewEntities.ConfigInfo config)
        {
            return items.SelectMany(i => GetChannelItemInfo(i, config));
        }
        private IEnumerable<ChannelItemInfo> GetChannelItemInfo(iViewEntities.Series item, iViewEntities.ConfigInfo config)
        {
            return item.f.Select(i => GetChannelItemInfo(item, i, config));
            //return new ChannelItemInfo()
            //{
            //    Id = item.a,
            //    Name = item.b,
            //    Overview = item.c,
            //    Tags = item.e.Split(' ').ToList(),
            //    Type = ChannelItemType.Folder
            //};
        }
        private IEnumerable<ChannelItemInfo> GetChannelItemInfo(iViewEntities.Series parent, IEnumerable<iViewEntities.Program> items, iViewEntities.ConfigInfo config)
        {
            return items.Select(i => GetChannelItemInfo(parent, i, config));
        }
        private ChannelItemInfo GetChannelItemInfo(iViewEntities.Series parent, iViewEntities.Program item, iViewEntities.ConfigInfo config)
        {
            return new ChannelItemInfo()
            {
                Id = item.GetFolderId(parent),
                Name = item.b,
                Overview = item.d,
                Tags = (item.e == null) ? null : item.e.Split(' ').ToList(),
                RunTimeTicks = GetRuntimeTicksFromSeconds(item.j),
                OfficialRating = item.m,
                ImageUrl = item.s,
                IsInfiniteStream = (!string.IsNullOrWhiteSpace(item.t) && (item.t =="1")),
                Type = ChannelItemType.Media,
                MediaType = ChannelMediaType.Video
                //MediaSources = GetChannelMediaInfo(config, item)
            };
        }
        private long? GetRuntimeTicksFromSeconds(string sec)
        {
            double seconds;
            if (double.TryParse(sec, out seconds))
            {
                var time = TimeSpan.FromSeconds(seconds);
                return time.Ticks;
            }
            return null;
        }
        private List<ChannelMediaInfo> GetChannelMediaInfo(iViewEntities.ConfigInfo config, iViewEntities.Program item)
        {
            var results = new List<ChannelMediaInfo>();

            //***********************
            //standard def
            //this set works
            //***********************
            var baseUrl = config.ServerStreamngUrl.Replace("/ondemand", "////flash/playback/_definst_");
            var mainUrl = Uri.EscapeUriString(string.Format("{0}/{1}", baseUrl, item.n));
            var auth = iView.Downloader.GetAuthToken(this._httpClient, this._jsonSerializer, this._logger, CancellationToken.None, config).Result;
            var cmdParams = string.Format("{0} tcUrl={1}?auth={2} swfUrl={3} swfVfy=1 ", mainUrl, config.ServerStreamngUrl, auth.Token, "http://www.abc.net.au/iview/images/iview.jpg");
            results.Add(new ChannelMediaInfo() { Path = cmdParams, Protocol = MediaProtocol.Rtmp });

            //fallback - probably unnecessary and will go unused
            baseUrl = config.ServerFallbackUrl.Replace("/ondemand", "////flash/playback/_definst_");
            mainUrl = Uri.EscapeUriString(string.Format("{0}/{1}", baseUrl, item.n));
            auth = iView.Downloader.GetAuthToken(this._httpClient, this._jsonSerializer, this._logger, CancellationToken.None, config).Result;
            cmdParams = string.Format("{0} tcUrl={1}?auth={2} swfUrl={3} swfVfy=1 ", mainUrl, config.ServerStreamngUrl, auth.Token, "http://www.abc.net.au/iview/images/iview.jpg");
            results.Add(new ChannelMediaInfo() { Path = cmdParams, Protocol = MediaProtocol.Rtmp });
            //***********************

            ////trying for high def, this doesn't work but its got some useful bits and bobs
            //var auth = iView.Downloader.GetAuthToken(this._httpClient, this._jsonSerializer, this._logger, CancellationToken.None, config).Result;
            //var baseUrl = string.Format("{0}SMIL/", auth.Server, auth.Path);
            //var mainUrl = Uri.EscapeUriString(string.Format("{0}{1}", baseUrl, item.n));
            //var cmdParams = string.Format("{0} tcUrl={1}hdcore=true&hdnea= swfUrl={3} swfVfy=1 ", mainUrl, auth.Server, auth.TokenHD, "http://www.abc.net.au/iview/images/iview.jpg");
            //results.Add(new ChannelMediaInfo() { Path = cmdParams, Protocol = MediaProtocol.Rtmp });

            ////fallback - probably unnecessary and will go unused
            //baseUrl = config.ServerFallbackUrl.Replace("/ondemand", "////flash/playback/_definst_");
            //mainUrl = Uri.EscapeUriString(string.Format("{0}/{1}", baseUrl, item.n));
            //cmdParams = string.Format("{0}/{1}{2}", config.ServerFallbackUrl, item.f, item.n, "http://www.abc.net.au/iview/images/iview.jpg");
            //results.Add(new ChannelMediaInfo() { Path = cmdParams, Protocol = MediaProtocol.Rtmp });

            return results;
        }

        private async Task<IEnumerable<iViewEntities.SeriesIndex>> GetSeriesForCategory(iViewEntities.ConfigInfo config, iViewEntities.Category category, CancellationToken cancellationToken)
        {
            //todo cache seriesIndex
            var wholeSeriesIndex = await iView.Downloader.GetAllSeriesIndexes(this._httpClient, this._jsonSerializer, this._logger, cancellationToken, config);
            return GetSeriesIndexes(category, wholeSeriesIndex);
        }
        private IEnumerable<iViewEntities.SeriesIndex> GetSeriesIndexes(iViewEntities.Category category, IEnumerable<iViewEntities.SeriesIndex> allSeriesIndexes)
        {
            //TODO -goto do our own A-Z checking
            return allSeriesIndexes.Where(i => i.e.Contains(category.id));
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            //we know it's a program id

            //id will look something like
            //"Series_12345||Program_6789"
            string[] delimeter = { "||" };
            var s = id.Split(delimeter, StringSplitOptions.RemoveEmptyEntries );
            var seriesId = s[0].Replace("Series_", "");
            var programId = s[1].Replace("Program_", "");
            //TODO cache config instead of loading it each time
            var config = await iView.Downloader.GetConfig(this._httpClient, this._logger, cancellationToken);

            var series = await iView.Downloader.GetSeriesDetail(this._httpClient, this._jsonSerializer, this._logger, cancellationToken, config, seriesId);
            var program = series.First().f.First(i => i.a == programId);

            return GetChannelMediaInfo(config, program);
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Backdrop
            };
        }

        public string HomePageUrl
        {
            get { return ""; }
        }

        public string Name
        {
            get { return "ABC iView"; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Episode, 
                    ChannelMediaContentType.Movie,
                    ChannelMediaContentType.Trailer
                },

                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                },
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Backdrop:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".png";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Png,
                            HasImage = true,
                            Stream = GetType().Assembly.GetManifestResourceStream(path)
                        });
                    }
                default:
                    throw new ArgumentException("Unsupported image type: " + type);
            }
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public string GetCacheKey(string userId)
        {
            //return string.Join(",", Plugin.Instance.Configuration.streams.ToList());
            return string.Empty;
        }

        public string Description
        {
            get { return string.Empty; }
        }
    }


    public static class Extensions
    {
        //public static string GetHierarchicalFolderId(this iViewEntities.Category item, string parentFolderId)
        //{
        //    return string.Format("{0}||{2}", parentFolderId, GetIndividualFolderId(item));
        //}
        //public static string GetIndividualFolderId(this iViewEntities.Category item)
        //{
        //    return string.Format("Category_{0}", item.id);
        //}
        //public static string GetHierarchicalFolderId(this iViewEntities.SeriesIndex item, string parentFolderId)
        //{
        //    return string.Format("{0}||{2}", parentFolderId, GetIndividualFolderId(item));
        //}
        //public static string GetIndividualFolderId(this iViewEntities.SeriesIndex item)
        //{
        //    return string.Format("SeriesIndex_{0}", item.a);
        //}
        //public static string GetHierarchicalFolderId(this iViewEntities.Series item, string parentFolderId)
        //{
        //    return string.Format("{0}||{2}", parentFolderId, GetIndividualFolderId(item));
        //}
        //public static string GetIndividualFolderId(this iViewEntities.Series item)
        //{
        //    return string.Format("Series_{0}", item.a);
        //}
        //public static string GetHierarchicalFolderId(this iViewEntities.ProgramIndex item, string parentFolderId)
        //{
        //    return string.Format("{0}||{2}", parentFolderId, GetIndividualFolderId(item));
        //}
        //public static string GetIndividualFolderId(this iViewEntities.ProgramIndex item)
        //{
        //    return string.Format("ProgramIndex_{0}", item.a);
        //}
        //public static string GetHierarchicalFolderId(this iViewEntities.Program item, string parentFolderId)
        //{
        //    return string.Format("{0}||{2}", parentFolderId, GetIndividualFolderId(item));
        //}
        //public static string GetIndividualFolderId(this iViewEntities.Program item)
        //{
        //    return string.Format("Program_{0}", item.a);
        //}
        public static string GetFolderId(this iViewEntities.Program item, iViewEntities.Series parent)
        {
            return string.Format("Series_{0}||Program_{1}", parent.a, item.a);
        }

        public static iViewEntities.Category FirstOrDefaultDecenant(this IEnumerable<iViewEntities.Category> items, Func<iViewEntities.Category, bool> predicate)
        {
            return items.FirstOrDefaultDecenant(i => i.Categories, predicate);
        }
        public static T FirstOrDefaultDecenant<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> childrenSelector, Func<T, bool> predicate)
        {
            var flatItems = items.Flatten(childrenSelector);
            return flatItems.FirstOrDefault(predicate);
        }
        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> source,
            Func<T, IEnumerable<T>> childrenSelector)
        {
            // Do standard error checking here.

            // Cycle through all of the items.
            foreach (T item in source)
            {
                // Yield the item.
                yield return item;

                // Yield all of the children.
                foreach (T child in childrenSelector(item).Flatten(childrenSelector))
                {
                    // Yield the item.
                    yield return child;
                }
            }
        }
    }
}
