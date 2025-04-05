using System;

namespace Singularity.Core
{
    public static class SourceResolver
    {
        public static string GetSource(string url)
        {
            var host = new Uri(url).Host.ToLower();

            if (host.Contains("youtube.com") || host.Contains("youtu.be"))
                return "YouTube";
            if (host.Contains("tiktok.com"))
                return "TikTok";
            if (host.Contains("pornhub.com"))
                return "Pornhub";

            return "Unknown";
        }
    }
}