using CommonMark.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CommonMark.Tests
{
    [TestClass]
    public class TableTests
    {
        static CommonMarkSettings Settings;

        static TableTests()
        {
            Settings = CommonMarkSettings.Default.Clone();
            Settings.AdditionalFeatures = CommonMarkAdditionalFeatures.GithubStyleTables;
            Settings.TrackSourcePosition = true;
        }

        [TestMethod]
        public void SimpleTable()
        {
            var markdown =
@"First Header  | Second Header
------------- | -------------
Content Cell  | Content Cell
Content Cell  | Content Cell";

            var ast = 
                CommonMarkConverter.Parse(
                    markdown,
                    Settings
                );

            var firstChild = ast.FirstChild;
            Assert.AreEqual(BlockTag.Table, firstChild.Tag);
            Assert.AreEqual(markdown, markdown.Substring(firstChild.SourcePosition, firstChild.SourceLength));
        }

        [TestMethod]
        public void SplitTable()
        {
            var markdown =
@"First Header  | Second Header
------------- | -------------
Content Cell  | Content Cell
Content Cell  | Content Cell
Hello world";

            var ast =
                CommonMarkConverter.Parse(
                    markdown,
                    Settings
                );

            var firstChild = ast.FirstChild;
            var secondChild = firstChild.NextSibling;
            Assert.AreEqual(BlockTag.Table, firstChild.Tag);
            var firstMarkdown = markdown.Substring(firstChild.SourcePosition, firstChild.SourceLength);
            var shouldMatch = @"First Header  | Second Header
------------- | -------------
Content Cell  | Content Cell
Content Cell  | Content Cell
";
            Assert.AreEqual(shouldMatch,firstMarkdown);

            Assert.AreEqual(BlockTag.Paragraph, secondChild.Tag);
            var secondMarkdown = markdown.Substring(secondChild.SourcePosition, secondChild.SourceLength);
            Assert.AreEqual("Hello world", secondMarkdown);
        }
    }
}
