using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Wipro;
using static Wipro.Program;

namespace WiproTest
{
    [TestClass]
    public class ProgramTest
    {
        [TestMethod]
        public void TestCrawl()
        {
            XmlDocument doc = Wipro.Program.Crawl("https://www.wipro.com", new Dictionary<string, string>(), 5);
            Assert.IsTrue(doc.DocumentElement.SelectNodes("//LOC").Count > 0);
        }

        [TestMethod]
        public void TestCrawlRecursively()
        {
            var domain = "https://www.bbc.com";
            XmlDocument doc = new XmlDocument();
            string xml =
                "<SITEMAP>" +
                    "<URL>" +
                    "   <LOC>https://www.bbc.com</LOC>" +
                    "</URL>" +
                    "<URL>" +
                    "   <LOC>https://www.bbc.com/news</LOC>" +
                    "</URL>" +
                    "<URL>" +
                    "   <LOC>https://www.bbc.com/sport</LOC>" +
                    "</URL>" +
                    "<URL>" +
                    "   <LOC>https://www.bbc.com/sport/football</LOC>" +
                    "</URL>" +
                "</SITEMAP>";
            doc.LoadXml(xml);

            List<PageLink> pageLinks = new List<PageLink>();
            pageLinks.Add(new PageLink { Link = "https://www.bbc.com", MatchType = MatchType.URL, CanBeCrawled = true });
            pageLinks.Add(new PageLink { Link = "https://www.bbc.com/news", MatchType = MatchType.URL, CanBeCrawled = true });
            pageLinks.Add(new PageLink { Link = "https://www.bbc.com/sport", MatchType = MatchType.URL, CanBeCrawled = true });
            pageLinks.Add(new PageLink { Link = "https://www.bbc.com/sport/football", MatchType = MatchType.URL, CanBeCrawled = true });
            var elemNode = doc.DocumentElement;
            Wipro.Program.CrawlRecursively(pageLinks, elemNode, domain, domain, new Dictionary<string, string>(), 10);

            //at least one of the links should be nexted
            Assert.IsTrue(doc.DocumentElement.SelectNodes("//URL[LOC]/URL[LOC]").Count > 0);
        }

        [TestMethod]
        public void TestReadPage()
        {
            Dictionary<string, string> processedLinks = new Dictionary<string, string>();
            string url = "www.someurl.com";
            string buffer = "test data";
            processedLinks.Add(url, buffer);
            string pageData = Wipro.Program.ReadPage(url, processedLinks);
            Assert.AreEqual(pageData, buffer);
        }

        [TestMethod]
        public void TestGetPageLinks()
        {
            string url = "www.wiprodigital.com";
            string domain = "www.wiprodigital.com";
            string validXHTMLPageData = "<html>" +
                                "<head>" +
                                "<script src = 'file.js' />" +
                                "<base href = 'www.wipro.com' />" +
                                "</head>" +
                                "<body style = \"background: url('text.png');\" >" +
                                "<h1> Hello World </h1>" +
                                "<a href = '#clickme' > Click Me </a>" +
                                "<div>" +
                                "<a href = '/relative-link/get' > Relative link </a>" +
                                "</div>" +
                                "</body>" +
                                "</html>";

            string invalidXHTMLPageData = "<html>" +
                                "<head>" +
                                "<script src = 'file.js' />" +
                                "<base href = 'www.wipro.com' />" +
                                "</_________FAIL________head>" +
                                "<body style = \"background: url('text.png');\" >" +
                                "<h1> Hello World </h1>" +
                                "<a href = '#clickme' > Click Me </a>" +
                                "<div>" +
                                "<a href = '/relative-link/get' > Relative link </a>" +
                                "</div>" +
                                "</body>" +
                                "</html>";

            var pageLinksFromValidXHTML = Wipro.Program.GetPageLinks(validXHTMLPageData, url, domain);
            Assert.IsTrue(pageLinksFromValidXHTML.AsEnumerable().Count(p => p.MatchType == MatchType.CSSBACKGROUND) == 1);
            Assert.IsTrue(pageLinksFromValidXHTML.AsEnumerable().Count(p => p.MatchType == MatchType.URL) == 2);
            Assert.IsTrue(pageLinksFromValidXHTML.AsEnumerable().Count(p => p.MatchType == MatchType.SCRIPT) == 1);
            Assert.IsTrue(pageLinksFromValidXHTML.AsEnumerable().Count(p => p.Link.StartsWith("www.wipro.com")) > 0);

            var pageLinksFromInvalidXHTML = Wipro.Program.GetPageLinks(invalidXHTMLPageData, url, domain);
            Assert.AreEqual
                (
                    pageLinksFromValidXHTML.AsEnumerable().Count(p => p.MatchType == MatchType.CSSBACKGROUND),
                    pageLinksFromInvalidXHTML.AsEnumerable().Count(p => p.MatchType == MatchType.CSSBACKGROUND)
                );
            Assert.AreEqual
                (
                    pageLinksFromValidXHTML.AsEnumerable().Count(p => p.MatchType == MatchType.URL),
                    pageLinksFromInvalidXHTML.AsEnumerable().Count(p => p.MatchType == MatchType.URL)
                );

            Assert.AreEqual
                (
                    pageLinksFromValidXHTML.AsEnumerable().Count(p => p.MatchType == MatchType.SCRIPT),
                    pageLinksFromInvalidXHTML.AsEnumerable().Count(p => p.MatchType == MatchType.SCRIPT)
                );

            Assert.AreEqual
                (
                    pageLinksFromValidXHTML.AsEnumerable().Count(p => p.Link.StartsWith("www.wipro.com")),
                    pageLinksFromInvalidXHTML.AsEnumerable().Count(p => p.Link.StartsWith("www.wipro.com"))
                );
        }

        [TestMethod]
        public void TestGeneratePageLinksAsXML()
        {
            var url = "https://www.bbc.com";
            var domain = "https://www.bbc.com";
            XmlDocument doc = new XmlDocument();
            string xml = "<SITEMAP></SITEMAP>";
            doc.LoadXml(xml);

            List<PageLink> pageLinks = new List<PageLink>();
            pageLinks.Add(new PageLink { Link = "https://www.bbc.com", MatchType = MatchType.URL, CanBeCrawled = true });
            pageLinks.Add(new PageLink { Link = "https://www.bbc.com/news", MatchType = MatchType.URL, CanBeCrawled = true });
            pageLinks.Add(new PageLink { Link = "https://www.bbc.com/sport", MatchType = MatchType.URL, CanBeCrawled = true });
            pageLinks.Add(new PageLink { Link = "https://www.bbc.com/sport/football", MatchType = MatchType.URL, CanBeCrawled = true });
            var elemNode = doc.DocumentElement;
            Wipro.Program.GeneratePageLinksAsXML(pageLinks, elemNode, url, domain);
            Assert.IsTrue(doc.DocumentElement.SelectNodes("//URL[LOC]").Count == pageLinks.Count());
        }

        [TestMethod]
        public void TestExpandLink()
        {
            string pageUrl = "www.bbc.com";
            string baseRef = "www.bbc.co.uk";
            string domain = "www.bbc.com";
            Assert.IsTrue(Wipro.Program.ExpandLink("#anchor", pageUrl, null, domain) == "www.bbc.com/#anchor");
            Assert.IsTrue(Wipro.Program.ExpandLink("/pagelink", pageUrl, null, domain) == "www.bbc.com/pagelink");
            Assert.IsTrue(Wipro.Program.ExpandLink("/pagelinkfrombasetag", pageUrl, baseRef, domain) == "www.bbc.co.uk/pagelinkfrombasetag");
            Assert.IsTrue(Wipro.Program.ExpandLink("//root", pageUrl, baseRef, domain) == "//root");
            Assert.IsTrue(Wipro.Program.ExpandLink("www.w3schools/root", pageUrl, null, domain) == "www.w3schools/root");
        }

        [TestMethod]
        public void TestDetermineIfLinkCouldBeCrawled()
        {
            Assert.IsTrue(Wipro.Program.DetermineIfLinkCouldBeCrawled("www.bbc.com", "www.abc.com") == false);
            Assert.IsTrue(Wipro.Program.DetermineIfLinkCouldBeCrawled("http://www.bbc.com", "https://www.bbc.com/test") == true);
            Assert.IsTrue(Wipro.Program.DetermineIfLinkCouldBeCrawled("http://www.bbc.com", "http://www.bbc.com/test") == true);
            Assert.IsTrue(Wipro.Program.DetermineIfLinkCouldBeCrawled("http://bbc.com", "http://www.bbc.com/test") == true);
            Assert.IsTrue(Wipro.Program.DetermineIfLinkCouldBeCrawled("bbc.com", "http://www.bbc.com/test") == true);
        }

        //public static bool IsLinkTheSameAsPageUrl(string link, string pageUrl)
        [TestMethod]
        public void TestIsLinkTheSameAsPageUrl()
        {
            Assert.IsTrue(Wipro.Program.IsLinkTheSameAsPageUrl("www.bbc.com", "www.abc.com") == false);
            Assert.IsTrue(Wipro.Program.IsLinkTheSameAsPageUrl("https://www.bbc.com", "https://www.bbc.com/") == true);
            Assert.IsTrue(Wipro.Program.IsLinkTheSameAsPageUrl("https://www.bbc.com", "https://www.bbc.com/#achor") == true);
            Assert.IsTrue(Wipro.Program.IsLinkTheSameAsPageUrl("https://www.bbc.com/", "https://www.bbc.com/#achor") == true);
            Assert.IsTrue(Wipro.Program.IsLinkTheSameAsPageUrl("https://www.bbc.com", "https://www.bbc.com/pagelink") == false);
        }
    }
}
