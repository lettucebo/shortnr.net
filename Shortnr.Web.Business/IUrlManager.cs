using Shortnr.Web.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shortnr.Web.Business
{
	public interface IUrlManager
	{
		Task<ShortUrl> ShortenUrl(string longUrl, string ip, string segment = "");
		Task<Status> Click(string segment, string referer, string ip);
	}
}
