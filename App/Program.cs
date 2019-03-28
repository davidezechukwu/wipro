using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace Wipro
{
    public class Program
    {
        public class PageLink : IEquatable<PageLink>
        {
            public string Link { get; set; }
            public MatchType MatchType { get; set; }
            public bool CanBeCrawled { get; set; }
            public bool Equals(PageLink other)
            {
                if (Object.ReferenceEquals(other, null))
                {
                    return false;
                }

                if (Object.ReferenceEquals(this, other))
                {
                    return true;
                }

                return Link.Equals(other.Link);
            }
            public override int GetHashCode()
            {
                return Link.GetHashCode();
            }
        }

        public enum MatchType
        {
            URL,
            AREA,
            IMAGE,
            MEDIA,
            CSSBACKGROUND,
            SCRIPT,
            CSSLINK
        }

        public static void Main(string[] args)
        {
            if (args == null || args.Length == 0 || args[0].Trim().Length == 0)
            {
                Console.WriteLine("Usage: Wipro.exe [DNS | IPV4 || IPV6]  MaxNumberOfPagesToCrawl(default is 50)");
                Console.WriteLine("for example: Wipro.exe https://www.wiprodigital.com  20");
                Console.WriteLine("for example: Wipro.exe https://www.wiprodigital.com/ ");
                Console.WriteLine("for example: Wipro.exe http://www.wiprodigital.com ");
                Console.WriteLine("for example: Wipro.exe https://wiprodigital.com 40");
                Console.WriteLine("for example: Wipro.exe http://wiprodigital.com 40");
                Console.WriteLine("for example: Wipro.exe http://wipro.digital 40");
                Console.WriteLine("for example: Wipro.exe wiprodigital.com 40");
                Console.WriteLine("for example: Wipro.exe 52.7.121.233 40");
                Console.WriteLine("for example: Wipro.exe https://52.7.121.233:443/");
                Console.WriteLine("Output is written to ./Crawl output for [domain] on [long utc date] [long utc time].xml");
            }

            if (!(args[0] != null && args[0].Trim().Length > 0 && ((Uri.CheckHostName(args[0]) != UriHostNameType.Unknown || Uri.IsWellFormedUriString(args[0], UriKind.Absolute)))))
            {
                Console.WriteLine("The URL provided is not valid, please provide a URL in the form of wiprodigital.com, wipro.com, http://wiprodigital.com, https://wiprodigital.com, https://www.wiprodigital.com, wipro.org, wipro.digital");
                return;
            }

            int maxIndexedPageSize = int.TryParse(args[1], out maxIndexedPageSize) ? maxIndexedPageSize : 30;

            Console.WriteLine("Crawling ..." + args[0] + " with maxIndexedPageSize set to " + maxIndexedPageSize.ToString());
            string outputFileName = "Crawl output for " + args[0] + " on " + DateTime.UtcNow.ToLongDateString() + " " + DateTime.UtcNow.ToLongTimeString() + ".xml";
            outputFileName = string.Join("_", outputFileName.Split(Path.GetInvalidFileNameChars()));
            File.WriteAllText(outputFileName, Crawl(args[0], new Dictionary<string, string>(), maxIndexedPageSize).OuterXml);
        }

        public static XmlDocument Crawl(string domain, Dictionary<string, string> processedLinks, int maxIndexedPageSize)
        {
            XmlDocument xmlOutput = new XmlDocument();
            var xmlSitemapNode = xmlOutput.CreateElement("SITEMAP");
            xmlOutput.AppendChild(xmlSitemapNode);
            var pageLinks = GetPageLinks(ReadPage(domain, processedLinks), domain, domain);
            GeneratePageLinksAsXML(pageLinks, xmlSitemapNode, domain, domain);
            CrawlRecursively(pageLinks, xmlSitemapNode, domain, domain, processedLinks, maxIndexedPageSize);
            return xmlOutput;
        }

        public static void CrawlRecursively(IList<PageLink> pageLinks, XmlElement parentNode, string url, string domain, Dictionary<string, string> processedLinks, int maxIndexedPageSize)
        {
            if (pageLinks == null)
            {
                return;
            }
            foreach (var pageLink in pageLinks)
            {
                //MaxIndexedPageSize is the recursive break;
                if (processedLinks.Count < maxIndexedPageSize && pageLink.CanBeCrawled)
                {
                    XmlElement thisNode = parentNode.SelectSingleNode("./*[LOC/text()=\"" + HttpUtility.HtmlEncode(pageLink.Link) + "\"]") as XmlElement;
                    if (thisNode != null)
                    {
                        if (!processedLinks.ContainsKey(pageLink.Link))
                        {
                            Console.WriteLine("Crawling ... " + pageLink.Link);
                            var pageData = ReadPage(pageLink.Link, processedLinks);
                            var thisPageLinks = GetPageLinks(pageData, url, domain);
                            GeneratePageLinksAsXML(thisPageLinks, thisNode, url, domain);
                            CrawlRecursively(thisPageLinks, thisNode, pageLink.Link, domain, processedLinks, maxIndexedPageSize);
                        }
                        else
                        {
                            Console.WriteLine("Skipped HTTP Request for ... " + pageLink.Link);
                            var pageData = processedLinks[pageLink.Link];
                            var thisPageLinks = GetPageLinks(pageData, url, domain);
                            GeneratePageLinksAsXML(thisPageLinks, thisNode, url, domain);

                        }
                    }
                }
            }
        }

        public static string ReadPage(string url, Dictionary<string, string> processedLinks)
        {
            var pageData = string.Empty;
            if (processedLinks.ContainsKey(url))
            {
                return processedLinks[url];
            }
            try
            {
                using (WebClient web = new WebClient())
                {
                    //ideally this should be handled asyc
                    pageData = web.DownloadString(url);
                }                              
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                pageData = string.Empty;
            }
            finally
            {
                //ideally this should be re-tried at a later stage
                processedLinks.Add(url, pageData);
            }
            return processedLinks[url];
        }

        public static IList<PageLink> GetPageLinks(string pageData, string url, string domain)
        {            
            IList<PageLink> pageLinks = null;
            try
            {
                pageLinks = GetPageLinksUsingXPath(pageData, url, domain);                
            }
            catch
            {
                pageLinks = GetPageLinksUsingRegex(pageData, url, domain);
            }

            
            return pageLinks;
        }

        public static IList<PageLink> GetPageLinksUsingXPath(string pageData, string url, string domain)
        {
            IList<PageLink> pageLinks = new List<PageLink>();
            var doc = new XmlDocument();
            doc.LoadXml(pageData.ToLower().Replace("cdata", "CDATA"));            
            var aTags = doc.SelectNodes("//a[@href]");
            var areaTags = doc.SelectNodes("//area[@href]");
            var imgTags = doc.SelectNodes("//img[@src]");
            var mediaTags = doc.SelectNodes("//source[@src]");
            var scriptTags = doc.SelectNodes("//script[@src]");
            var cssLinkTags = doc.SelectNodes("//link[@href]");
            var cssUrlAttrs = doc.SelectNodes("//@style[contains(., 'url')]");
            var baseRef = doc.SelectSingleNode("//base/@href") == null ? string.Empty : doc.SelectSingleNode("//base/@ref").Value;

            foreach (XmlNode node in aTags)
            {
                var href = node.Attributes["href"].Value.Trim();
                bool isBookmark = href.StartsWith("#");
                var link = ExpandLink(href, url, baseRef, domain);
                if (string.IsNullOrEmpty(link)) continue;
                bool canBeCrawled = !isBookmark && DetermineIfLinkCouldBeCrawled(link, domain);
                if (!IsLinkTheSameAsPageUrl(link, url))
                {
                    pageLinks.Add(new PageLink { Link = link, CanBeCrawled = canBeCrawled, MatchType = MatchType.URL });
                }
            }

            foreach (XmlNode node in areaTags)
            {
                var link = ExpandLink(node.Attributes["href"].Value.Trim(), url, baseRef, domain);
                if (string.IsNullOrEmpty(link)) continue;
                bool canBeCrawled = DetermineIfLinkCouldBeCrawled(link, domain);
                if (!IsLinkTheSameAsPageUrl(link, url))
                {
                    pageLinks.Add(new PageLink { Link = link, CanBeCrawled = canBeCrawled, MatchType = MatchType.AREA });
                }
            }

            foreach (XmlNode node in imgTags)
            {
                var link = ExpandLink(node.Attributes["src"].Value.Trim(), url, baseRef, domain);
                if (string.IsNullOrEmpty(link)) continue;
                if (!IsLinkTheSameAsPageUrl(link, url))
                {
                    pageLinks.Add(new PageLink { Link = link, CanBeCrawled = false, MatchType = MatchType.IMAGE });
                }
            }

            foreach (XmlNode node in mediaTags)
            {
                var link = ExpandLink(node.Attributes["src"].Value.Trim(), url, baseRef, domain);
                if (string.IsNullOrEmpty(link)) continue;
                pageLinks.Add(new PageLink { Link = link, CanBeCrawled = false, MatchType = MatchType.MEDIA });
            }

            foreach (XmlNode node in scriptTags)
            {
                var link = ExpandLink(node.Attributes["src"].Value.Trim(), url, baseRef, domain);
                if (string.IsNullOrEmpty(link)) continue;
                pageLinks.Add(new PageLink { Link = link, CanBeCrawled = false, MatchType = MatchType.SCRIPT });
            }

            foreach (XmlNode node in cssLinkTags)
            {
                var link = ExpandLink(node.Attributes["href"].Value.Trim(), url, baseRef, domain);
                if (string.IsNullOrEmpty(link)) continue;
                pageLinks.Add(new PageLink { Link = link, CanBeCrawled = false, MatchType = MatchType.CSSLINK });
            }

            foreach (XmlAttribute node in cssUrlAttrs)
            {
                var link = string.Empty;                
                var cssValue = node.Value.Trim();
                //use regex to pick out url
                Match match = Regex.Match(cssValue, "\"([^\"]*)\"");
                if (match.Success)
                {
                    link = match.Groups[1].Value;
                }
                else
                {
                    match = Regex.Match(cssValue, "'([^\"]*)'");
                    if (match.Success)
                    {
                        link = match.Groups[1].Value;
                    }
                }
                link = ExpandLink(link, url, baseRef, domain);
                if (string.IsNullOrEmpty(link)) continue;
                pageLinks.Add(new PageLink { Link = link, CanBeCrawled = false, MatchType = MatchType.CSSBACKGROUND });
            }

            return pageLinks.Distinct().ToList();
        }

        public static IList<PageLink> GetPageLinksUsingRegex(string pageData, string url, string domain)
        {            
            IList<PageLink> pageLinks = new List<PageLink>();
            List<string> hrefsA = new List<string>();            
            foreach ( Match match in Regex.Matches(pageData, "<a\\s+(?:[^>]*?\\s+)?href\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+))",  RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled))
            {
                if (!match.Groups[1].Value.StartsWith("data:") && !match.Groups[1].Value.StartsWith("javascript:"))
                {
                    hrefsA.Add(match.Groups[1].Value);
                }
            }

            List<string> hrefsArea = new List<string>();
            foreach (Match match in Regex.Matches(pageData, "<area\\s+(?:[^>]*?\\s+)?href\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+))", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled))
            {
                if (!match.Groups[1].Value.StartsWith("data:") && !match.Groups[1].Value.StartsWith("javascript:"))
                {
                    hrefsArea.Add(match.Groups[1].Value);
                }
            }

            List<string> refsImage = new List<string>();
            foreach (Match match in Regex.Matches(pageData, "<img\\s+(?:[^>]*?\\s+)?src\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+))", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled))
            {
                refsImage.Add(match.Groups[1].Value);
            }

            List<string> refsMedia = new List<string>();
            foreach (Match match in Regex.Matches(pageData, "<source\\s+(?:[^>]*?\\s+)?src\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+))", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled))
            {
                if (!match.Groups[1].Value.StartsWith("data:") && !match.Groups[1].Value.StartsWith("javascript:"))
                {
                    refsMedia.Add(match.Groups[1].Value);
                }
            }

            List<string> refsScript = new List<string>();
            foreach (Match match in Regex.Matches(pageData, "<script\\s+(?:[^>]*?\\s+)?src\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+))", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled))
            {
                if (!match.Groups[1].Value.StartsWith("data:") && !match.Groups[1].Value.StartsWith("javascript:"))
                {
                    refsScript.Add(match.Groups[1].Value);
                }
            }


            List<string> hrefsCssLinkTags = new List<string>();
            foreach (Match match in Regex.Matches(pageData, "<link\\s+(?:[^>]*?\\s+)?href\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+))", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled))
            {
                if (!match.Groups[1].Value.StartsWith("data:") && !match.Groups[1].Value.StartsWith("javascript:"))
                {
                    hrefsCssLinkTags.Add(match.Groups[1].Value);
                }
            }

            List<string> cssUrls = new List<string>();
            foreach (Match match in Regex.Matches(pageData, @"(?:background(?:-image)?|list-style):[^\r\n;]*url\((?!['""]? (?: data | http) :)['""]?([^'""\)]*)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled))
            {
                cssUrls.Add(match.Groups[1].Value);
            }

            var baseRef = "";
            MatchCollection matchBaseRef = Regex.Matches(pageData, "<base\\s+(?:[^>]*?\\s+)?href\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+))", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
            if (matchBaseRef.Count > 0)
            {
                baseRef = matchBaseRef[0].Groups[1].Value;
            }

            foreach (var path in hrefsA)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var href = path.Trim();
                bool isBookmark = href.StartsWith("#");
                var link = ExpandLink(href, url, baseRef, domain);
                bool canBeCrawled = !isBookmark && DetermineIfLinkCouldBeCrawled(link, domain);
                if (!IsLinkTheSameAsPageUrl(link, url))
                {
                    pageLinks.Add(new PageLink { Link = link, CanBeCrawled = canBeCrawled, MatchType = MatchType.URL });
                }
            }

            foreach (var path in hrefsArea)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var link = ExpandLink(path.Trim(), url, baseRef, domain);
                bool canBeCrawled = DetermineIfLinkCouldBeCrawled(link, domain);
                if (!IsLinkTheSameAsPageUrl(link, url))
                {
                    pageLinks.Add(new PageLink { Link = link, CanBeCrawled = canBeCrawled, MatchType = MatchType.AREA });
                }
            }

            foreach (var path in refsImage)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var link = ExpandLink(path.Trim(), url, baseRef, domain);
                if (!IsLinkTheSameAsPageUrl(link, url))
                {
                    pageLinks.Add(new PageLink { Link = link, CanBeCrawled = false, MatchType = MatchType.IMAGE });
                }
            }

            foreach (var path in refsMedia)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var link = ExpandLink(path.Trim(), url, baseRef, domain);
                pageLinks.Add(new PageLink { Link = link, CanBeCrawled = false, MatchType = MatchType.MEDIA });
            }

            foreach (var path in refsScript)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var link = ExpandLink(path.Trim(), url, baseRef, domain);
                pageLinks.Add(new PageLink { Link = link, CanBeCrawled = false, MatchType = MatchType.SCRIPT });
            }

            foreach (var path in hrefsCssLinkTags)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var link = ExpandLink(path.Trim(), url, baseRef, domain);
                pageLinks.Add(new PageLink { Link = link, CanBeCrawled = false, MatchType = MatchType.CSSLINK });
            }

            foreach (var path in cssUrls)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var link = ExpandLink(path, url, baseRef, domain);
                pageLinks.Add(new PageLink { Link = link, CanBeCrawled = false, MatchType = MatchType.CSSBACKGROUND });
            }

            return pageLinks.Distinct().ToList();
        }

        public static void GeneratePageLinksAsXML(IList<PageLink> pageLinks, XmlElement parentNode, string url, string domain)
        {
            if (pageLinks == null)
            {
                return;
            }
            foreach (var pageLink in pageLinks)
            {
                XmlElement thisNode = null;
                string elementName = Enum.GetName(typeof(MatchType), pageLink.MatchType);
                thisNode = parentNode.OwnerDocument.CreateElement(elementName);
                var locNode = parentNode.OwnerDocument.CreateElement("LOC");
                locNode.InnerText = HttpUtility.HtmlEncode(pageLink.Link);
                thisNode.AppendChild(locNode);
                parentNode.AppendChild(thisNode);
            }
        }

        public static string ExpandLink(string link, string pageUrl, string baseRef, string domain)
        {
            if (link.Trim().StartsWith("#"))
            {
                return pageUrl.Trim('/') + '/' + link;
            }

            string host = domain;
            if (!string.IsNullOrWhiteSpace(baseRef))
            {
                host = baseRef;
            }
            host = host.TrimEnd('/') + '/';

            try
            {
                if (link.StartsWith("/") && !link.StartsWith("//"))
                {
                    return link = host + link.TrimStart('/');
                }

                var hostType = Uri.CheckHostName(link);
                if (hostType != UriHostNameType.Basic && hostType != UriHostNameType.Unknown)
                {
                    link = host + link;
                }
            }
            catch
            {

            };
            return link;

        }

        public static bool DetermineIfLinkCouldBeCrawled(string link, string domain)
        {
            try
            {
                string _link = link.ToLower().Replace("/www.", "/");
                string _domain = domain.ToLower().Replace("/www.", "/");
                if (_link.StartsWith("https://"))
                {
                    _link = _link.Replace("https://", "");                    
                }
                _link = "http://" + _link.Replace("http://", "");
                return new Uri(_link).DnsSafeHost.ToLower() == new Uri(_domain).DnsSafeHost.ToLower();
            }
            catch
            {
                return false;
            }
        }

        public static bool IsLinkTheSameAsPageUrl(string link, string pageUrl)
        {
            try
            {
                var uriLink = new Uri(link);
                var uriPageUrl = new Uri(pageUrl);
                return
                    uriLink.DnsSafeHost.ToLower() == uriPageUrl.DnsSafeHost.ToLower()
                    && uriLink.PathAndQuery == uriPageUrl.PathAndQuery;
                    //&& uriLink.Fragment == uriPageUrl.Fragment;
            }
            catch
            {
                return false;
            }
        }
    }
}
