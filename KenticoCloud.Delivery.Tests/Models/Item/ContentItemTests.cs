using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery.Tests
{
	[TestFixture]
	public class ContentItemTests
	{
		[Test]
		public void EmptyConstructor_GetNonexistngElement_ThrowsAnException()
        {
            Assert.Throws<ArgumentException>(() => new ContentItem().GetAssets("nonexisting"));
            Assert.Throws<ArgumentException>(() => new ContentItem().GetDateTime("nonexisting"));
            Assert.Throws<ArgumentException>(() => new ContentItem().GetModularContent("nonexisting"));
            Assert.Throws<ArgumentException>(() => new ContentItem().GetNumber("nonexisting"));
            Assert.Throws<ArgumentException>(() => new ContentItem().GetOptions("nonexisting"));
            Assert.Throws<ArgumentException>(() => new ContentItem().GetString("nonexisting"));
            Assert.Throws<ArgumentException>(() => new ContentItem().GetTaxonomyTerms("nonexisting"));
        }
    }
}
