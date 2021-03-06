﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YAC.Web;

namespace YAC.Tests
{
    [TestClass]
    [TestCategory("Data Extractor")]
    public class ExtractorTests
    {
        [TestMethod]
        public void Host()
        {
            var domain = new Uri("https://domain.com");
            var extracted = DataExtractor.Extract(
                "<a href=\"https://domain.com/area\"></a>" +
                "<img src=\"source.jpg\" />" +
                "<a href=\"/area\"></a>" + 
                "<a href=\"/\"></a>",
                domain, "<img.+?(?<source>src)=\"(?<sourcename>.+?)\"");

            Assert.AreEqual(3, extracted.Links.Count);
        }
    }
}
