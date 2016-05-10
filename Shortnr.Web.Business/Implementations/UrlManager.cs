using Shortnr.Web.Data;
using Shortnr.Web.Exceptions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shortnr.Web.Business.Implementations
{
    public class UrlManager : IUrlManager
    {
        public Task<ShortUrl> ShortenUrl(string longUrl, string ip, string segment = "")
        {
            return Task.Run(() =>
            {
                using (var ctx = new ShortnrContext())
                {
                    ShortUrl url;

                    url = ctx.ShortUrls.Where(u => u.LongUrl == longUrl).FirstOrDefault();
                    if (url != null)
                    {
                        return url;
                    }

                    if (!longUrl.StartsWith("http://") && !longUrl.StartsWith("https://"))
                    {
                        throw new ArgumentException("Invalid URL format");
                    }

                    bool validateUrl = false;
                    bool.TryParse(ConfigurationManager.AppSettings["ValidateRemoteUrl"], out validateUrl);
                    if (validateUrl)
                    {
                        Uri urlCheck = new Uri(longUrl);
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlCheck);
                        request.Timeout = 10000;
                        try
                        {
                            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        }
                        catch (Exception)
                        {
                            throw new ShortnrNotFoundException();
                        }
                    }

                    int cap = 0;
                    int.TryParse(ConfigurationManager.AppSettings["MaxNumberShortUrlsPerHour"], out cap);
                    if (cap > 0)
                    {
                        DateTime dateToCheck = DateTime.Now.Subtract(new TimeSpan(1, 0, 0));
                        int count = ctx.ShortUrls.Where(u => u.Ip == ip && u.Added >= dateToCheck).Count();
                        if (cap != 0 && count > cap)
                        {
                            throw new ArgumentException("Your hourly limit has exceeded");
                        }
                    }

                    if (!string.IsNullOrEmpty(segment))
                    {
                        if (ctx.ShortUrls.Where(u => u.Segment == segment).Any())
                        {
                            throw new ShortnrConflictException();
                        }
                        if (segment.Length > 20 || !Regex.IsMatch(segment, @"^[A-Za-z\d_-]+$"))
                        {
                            throw new ArgumentException("Malformed or too long segment");
                        }
                    }
                    else
                    {
                        segment = this.NewSegment();
                    }

                    if (string.IsNullOrEmpty(segment))
                    {
                        throw new ArgumentException("Segment is empty");
                    }

                    url = new ShortUrl()
                    {
                        Id = Guid.NewGuid(),
                        Added = DateTime.Now,
                        Ip = ip,
                        LongUrl = longUrl,
                        NumOfClicks = 0,
                        Segment = segment
                    };

                    ctx.ShortUrls.Add(url);

                    ctx.SaveChanges();

                    return url;
                }
            });
        }

        public Task<Status> Click(string segment, string referer, string ip)
        {
            return Task.Run(() =>
            {
                using (var ctx = new ShortnrContext())
                {
                    ShortUrl url = ctx.ShortUrls.Where(u => u.Segment == segment).FirstOrDefault();
                    if (url == null)
                    {
                        throw new ShortnrNotFoundException();
                    }

                    url.NumOfClicks = url.NumOfClicks + 1;

                    Status stat = new Status()
                    {
                        Id = Guid.NewGuid(),
                        ClickDate = DateTime.Now,
                        Ip = ip,
                        Referer = referer,
                        ShortUrl = url
                    };

                    ctx.Statuses.Add(stat);

                    ctx.SaveChanges();

                    return stat;
                }
            });
        }

        private string NewSegment()
        {
            using (var ctx = new ShortnrContext())
            {
                int i = 0;

                int segmentSize = 0;
                if (!int.TryParse(ConfigurationManager.AppSettings["SegmentSize"], out segmentSize))
                {
                    segmentSize = 6;
                }

                while (i < 30)
                {
                    string segment = Guid.NewGuid().ToString().Replace("-", "").Substring(0, segmentSize);
                    if (!ctx.ShortUrls.Where(u => u.Segment == segment).Any())
                    {
                        return segment;
                    }
                    i++;
                }

                return string.Empty;
            }
        }
    }
}
