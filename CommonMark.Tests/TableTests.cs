using CommonMark.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace CommonMark.Tests
{
    [TestClass]
    public class TableTests
    {
        static CommonMarkSettings ReadSettings;
        static CommonMarkSettings WriteSettings;

        static TableTests()
        {
            ReadSettings = CommonMarkSettings.Default.Clone();
            ReadSettings.AdditionalFeatures = CommonMarkAdditionalFeatures.GithubStyleTables;
            ReadSettings.TrackSourcePosition = true;

            WriteSettings = CommonMarkSettings.Default.Clone();
            WriteSettings.AdditionalFeatures = CommonMarkAdditionalFeatures.GithubStyleTables;
        }

        [TestMethod]
        public void SimpleTable()
        {
            var markdown = "First Header | Second Header\n------------- | -------------\nContent Cell | Content Cell\nContent Cell | Content Cell\n";

            var ast = 
                CommonMarkConverter.Parse(
                    markdown,
                    ReadSettings
                );

            string html;
            using (var str = new StringWriter())
            {
                CommonMarkConverter.ProcessStage3(ast, str, WriteSettings);
                html = str.ToString();
            }
            Assert.AreEqual("<table><thead><tr><th>First Header</th><th>Second Header</th></tr></thead><tbody><tr><td>Content Cell</td><td>Content Cell</td></tr><tr><td>Content Cell</td><td>Content Cell</td></tr><tr></tr></tbody></table>\r\n", html);

            var firstChild = ast.FirstChild;
            Assert.AreEqual(BlockTag.Table, firstChild.Tag);
            Assert.AreEqual(markdown, markdown.Substring(firstChild.SourcePosition, firstChild.SourceLength));

            var headerRow = firstChild.FirstChild;
            Assert.AreEqual(BlockTag.TableRow, headerRow.Tag);
            Assert.AreEqual("First Header | Second Header\n", markdown.Substring(headerRow.SourcePosition, headerRow.SourceLength));

            var headerCell1 = headerRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, headerCell1.Tag);
            Assert.AreEqual("First Header", markdown.Substring(headerCell1.SourcePosition, headerCell1.SourceLength));

            var headerCell2 = headerCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, headerCell2.Tag);
            Assert.AreEqual("Second Header", markdown.Substring(headerCell2.SourcePosition, headerCell2.SourceLength));

            var firstRow = headerRow.NextSibling;
            Assert.AreEqual(BlockTag.TableRow, firstRow.Tag);
            Assert.AreEqual("Content Cell | Content Cell\n", markdown.Substring(firstRow.SourcePosition, firstRow.SourceLength));

            var firstRowCell1 = firstRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, firstRowCell1.Tag);
            Assert.AreEqual("Content Cell", markdown.Substring(firstRowCell1.SourcePosition, firstRowCell1.SourceLength));

            var firstRowCell2 = firstRowCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, firstRowCell2.Tag);
            Assert.AreEqual("Content Cell", markdown.Substring(firstRowCell2.SourcePosition, firstRowCell2.SourceLength));

            var secondRow = firstRow.NextSibling;
            Assert.AreEqual(BlockTag.TableRow, secondRow.Tag);
            Assert.AreEqual("Content Cell | Content Cell\n", markdown.Substring(secondRow.SourcePosition, secondRow.SourceLength));

            var secondRowCell1 = secondRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, secondRowCell1.Tag);
            Assert.AreEqual("Content Cell", markdown.Substring(secondRowCell1.SourcePosition, secondRowCell1.SourceLength));

            var secondRowCell2 = secondRowCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, secondRowCell2.Tag);
            Assert.AreEqual("Content Cell", markdown.Substring(secondRowCell2.SourcePosition, secondRowCell2.SourceLength));
        }

        [TestMethod]
        public void SplitTable()
        {
            var markdown =
@"First Header  | Second Header
------------- | -------------
Content Cell  | Content Cell
Content Cell  | Content Cell
Hello world
";

            var ast =
                CommonMarkConverter.Parse(
                    markdown,
                    ReadSettings
                );

            string html;
            using (var str = new StringWriter())
            {
                CommonMarkConverter.ProcessStage3(ast, str, WriteSettings);
                html = str.ToString();
            }
            Assert.AreEqual("<table><thead><tr><th>First Header</th><th>Second Header</th></tr></thead><tbody><tr><td>Content Cell</td><td>Content Cell</td></tr><tr><td>Content Cell</td><td>Content Cell</td></tr><tr></tr></tbody></table>\r\n<p>Hello world</p>\r\n\r\n", html);

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

            var headerRow = firstChild.FirstChild;
            Assert.AreEqual(BlockTag.TableRow, headerRow.Tag);
            Assert.AreEqual("First Header  | Second Header\n", markdown.Substring(headerRow.SourcePosition, headerRow.SourceLength));

            var headerCell1 = headerRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, headerCell1.Tag);
            Assert.AreEqual("First Header", markdown.Substring(headerCell1.SourcePosition, headerCell1.SourceLength));

            var headerCell2 = headerCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, headerCell2.Tag);
            Assert.AreEqual("Second Header", markdown.Substring(headerCell2.SourcePosition, headerCell2.SourceLength));

            var firstRow = headerRow.NextSibling;
            Assert.AreEqual(BlockTag.TableRow, firstRow.Tag);
            Assert.AreEqual("Content Cell  | Content Cell\n", markdown.Substring(firstRow.SourcePosition, firstRow.SourceLength));

            var firstRowCell1 = firstRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, firstRowCell1.Tag);
            Assert.AreEqual("Content Cell", markdown.Substring(firstRowCell1.SourcePosition, firstRowCell1.SourceLength));

            var firstRowCell2 = firstRowCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, firstRowCell2.Tag);
            Assert.AreEqual("Content Cell", markdown.Substring(firstRowCell2.SourcePosition, firstRowCell2.SourceLength));

            var secondRow = firstRow.NextSibling;
            Assert.AreEqual(BlockTag.TableRow, secondRow.Tag);
            Assert.AreEqual("Content Cell  | Content Cell\n", markdown.Substring(secondRow.SourcePosition, secondRow.SourceLength));

            var secondRowCell1 = secondRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, secondRowCell1.Tag);
            Assert.AreEqual("Content Cell", markdown.Substring(secondRowCell1.SourcePosition, secondRowCell1.SourceLength));

            var secondRowCell2 = secondRowCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, secondRowCell2.Tag);
            Assert.AreEqual("Content Cell", markdown.Substring(secondRowCell2.SourcePosition, secondRowCell2.SourceLength));

            Assert.AreEqual(BlockTag.Paragraph, secondChild.Tag);
            var secondMarkdown = markdown.Substring(secondChild.SourcePosition, secondChild.SourceLength);
            Assert.AreEqual("Hello world\n", secondMarkdown);
        }

        [TestMethod]
        public void WrappedTable()
        {
            var markdown =
@"Nope nope.

First Header  | Second Header
------------- | -------------
Content Cell  | Content Cell
Content Cell  | Content Cell
Hello world
";

            var ast =
                CommonMarkConverter.Parse(
                    markdown,
                    ReadSettings
                );

            string html;
            using (var str = new StringWriter())
            {
                CommonMarkConverter.ProcessStage3(ast, str, WriteSettings);
                html = str.ToString();
            }
            Assert.AreEqual("<p>Nope nope.</p>\r\n<table><thead><tr><th>First Header</th><th>Second Header</th></tr></thead><tbody><tr><td>Content Cell</td><td>Content Cell</td></tr><tr><td>Content Cell</td><td>Content Cell</td></tr><tr></tr></tbody></table>\r\n<p>Hello world</p>\r\n\r\n", html);

            Assert.AreEqual(BlockTag.Paragraph, ast.FirstChild.Tag);
            Assert.AreEqual(BlockTag.Table, ast.FirstChild.NextSibling.Tag);
            Assert.AreEqual(BlockTag.Paragraph, ast.FirstChild.NextSibling.NextSibling.Tag);
        }

        [TestMethod]
        public void TableWithInlines()
        {
            var markdown =
@" Name | Description          
 ------------- | ----------- 
 Help      | **Display the** [help](/help) window.
 Close     | _Closes_ a window     ";

            var ast =
                CommonMarkConverter.Parse(
                    markdown,
                    ReadSettings
                );
            string html;
            using (var str = new StringWriter())
            {
                CommonMarkConverter.ProcessStage3(ast, str, WriteSettings);
                html = str.ToString();
            }
            Assert.AreEqual("<table><thead><tr><th>Name</th><th>Description</th></tr></thead><tbody><tr><td>Help</td><td><strong>Display the</strong> <a href=\"/help\">help</a> window.</td></tr><tr><td>Close</td><td><em>Closes</em> a window</td></tr><tr></tr></tbody></table>\r\n", html);
        }
    }
}
