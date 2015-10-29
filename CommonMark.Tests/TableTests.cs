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
            Assert.AreEqual("<table><thead><tr><th>First Header</th><th>Second Header</th></tr></thead><tbody><tr><td>Content Cell</td><td>Content Cell</td></tr><tr><td>Content Cell</td><td>Content Cell</td></tr></tbody></table>\r\n", html);

            var firstChild = ast.FirstChild;
            Assert.AreEqual(BlockTag.Table, firstChild.Tag);
            Assert.AreEqual(markdown, firstChild.EquivalentMarkdown);
            Assert.IsNotNull(firstChild.TableHeaderAlignments);
            Assert.AreEqual(2, firstChild.TableHeaderAlignments.Count);
            Assert.AreEqual(TableHeaderAlignment.None, firstChild.TableHeaderAlignments[0]);
            Assert.AreEqual(TableHeaderAlignment.None, firstChild.TableHeaderAlignments[1]);

            var headerRow = firstChild.FirstChild;
            Assert.AreEqual(BlockTag.TableRow, headerRow.Tag);
            Assert.AreEqual("First Header | Second Header\n", headerRow.EquivalentMarkdown);

            var headerCell1 = headerRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, headerCell1.Tag);
            Assert.AreEqual("First Header", headerCell1.EquivalentMarkdown);

            var headerCell2 = headerCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, headerCell2.Tag);
            Assert.AreEqual("Second Header", headerCell2.EquivalentMarkdown);
            Assert.IsNull(headerCell2.NextSibling);

            var firstRow = headerRow.NextSibling;
            Assert.AreEqual(BlockTag.TableRow, firstRow.Tag);
            Assert.AreEqual("Content Cell | Content Cell\n", firstRow.EquivalentMarkdown);

            var firstRowCell1 = firstRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, firstRowCell1.Tag);
            Assert.AreEqual("Content Cell", firstRowCell1.EquivalentMarkdown);

            var firstRowCell2 = firstRowCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, firstRowCell2.Tag);
            Assert.AreEqual("Content Cell", firstRowCell2.EquivalentMarkdown);
            Assert.IsNull(firstRowCell2.NextSibling);

            var secondRow = firstRow.NextSibling;
            Assert.AreEqual(BlockTag.TableRow, secondRow.Tag);
            Assert.AreEqual("Content Cell | Content Cell\n", secondRow.EquivalentMarkdown);
            Assert.IsNull(secondRow.NextSibling);

            var secondRowCell1 = secondRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, secondRowCell1.Tag);
            Assert.AreEqual("Content Cell", secondRowCell1.EquivalentMarkdown);

            var secondRowCell2 = secondRowCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, secondRowCell2.Tag);
            Assert.AreEqual("Content Cell", secondRowCell2.EquivalentMarkdown);
            Assert.IsNull(secondRowCell2.NextSibling);
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
            Assert.AreEqual("<table><thead><tr><th>First Header</th><th>Second Header</th></tr></thead><tbody><tr><td>Content Cell</td><td>Content Cell</td></tr><tr><td>Content Cell</td><td>Content Cell</td></tr></tbody></table>\r\n<p>Hello world</p>\r\n\r\n", html);

            var firstChild = ast.FirstChild;
            var secondChild = firstChild.NextSibling;
            Assert.AreEqual(BlockTag.Table, firstChild.Tag);
            var firstMarkdown = firstChild.EquivalentMarkdown;
            var shouldMatch = @"First Header  | Second Header
------------- | -------------
Content Cell  | Content Cell
Content Cell  | Content Cell
";
            Assert.AreEqual(shouldMatch, firstMarkdown);
            Assert.IsNotNull(firstChild.TableHeaderAlignments);
            Assert.AreEqual(2, firstChild.TableHeaderAlignments.Count);
            Assert.AreEqual(TableHeaderAlignment.None, firstChild.TableHeaderAlignments[0]);
            Assert.AreEqual(TableHeaderAlignment.None, firstChild.TableHeaderAlignments[1]);

            var headerRow = firstChild.FirstChild;
            Assert.AreEqual(BlockTag.TableRow, headerRow.Tag);
            Assert.AreEqual("First Header  | Second Header\r\n", headerRow.EquivalentMarkdown);

            var headerCell1 = headerRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, headerCell1.Tag);
            Assert.AreEqual("First Header", headerCell1.EquivalentMarkdown);

            var headerCell2 = headerCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, headerCell2.Tag);
            Assert.AreEqual("Second Header", headerCell2.EquivalentMarkdown);
            Assert.IsNull(headerCell2.NextSibling);

            var firstRow = headerRow.NextSibling;
            Assert.AreEqual(BlockTag.TableRow, firstRow.Tag);
            Assert.AreEqual("Content Cell  | Content Cell\r\n", firstRow.EquivalentMarkdown);

            var firstRowCell1 = firstRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, firstRowCell1.Tag);
            Assert.AreEqual("Content Cell", firstRowCell1.EquivalentMarkdown);

            var firstRowCell2 = firstRowCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, firstRowCell2.Tag);
            Assert.AreEqual("Content Cell", firstRowCell2.EquivalentMarkdown);
            Assert.IsNull(firstRowCell2.NextSibling);

            var secondRow = firstRow.NextSibling;
            Assert.AreEqual(BlockTag.TableRow, secondRow.Tag);
            Assert.AreEqual("Content Cell  | Content Cell\r\n", secondRow.EquivalentMarkdown);
            Assert.IsNull(secondRow.NextSibling);

            var secondRowCell1 = secondRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, secondRowCell1.Tag);
            Assert.AreEqual("Content Cell", secondRowCell1.EquivalentMarkdown);

            var secondRowCell2 = secondRowCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, secondRowCell2.Tag);
            Assert.AreEqual("Content Cell", secondRowCell2.EquivalentMarkdown);
            Assert.IsNull(secondRowCell2.NextSibling);

            Assert.AreEqual(BlockTag.Paragraph, secondChild.Tag);
            var secondMarkdown = secondChild.EquivalentMarkdown;
            Assert.AreEqual("Hello world\r\n", secondMarkdown);
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
            Assert.AreEqual("<p>Nope nope.</p>\r\n<table><thead><tr><th>First Header</th><th>Second Header</th></tr></thead><tbody><tr><td>Content Cell</td><td>Content Cell</td></tr><tr><td>Content Cell</td><td>Content Cell</td></tr></tbody></table>\r\n<p>Hello world</p>\r\n\r\n", html);

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
            Assert.AreEqual("<table><thead><tr><th>Name</th><th>Description</th></tr></thead><tbody><tr><td>Help</td><td><strong>Display the</strong> <a href=\"/help\">help</a> window.</td></tr><tr><td>Close</td><td><em>Closes</em> a window</td></tr></tbody></table>\r\n", html);
        }

        [TestMethod]
        public void TableWithExtraPipes()
        {
            var markdown ="| First Header  | Second Header |\n| ------------- | ------------- |\n| cell #11  | cell #12  |\n| cell #21  | cell #22  |\n";

            var ast =
                CommonMarkConverter.Parse(
                    markdown,
                    ReadSettings
                );

            var firstChild = ast.FirstChild;
            Assert.AreEqual(BlockTag.Table, firstChild.Tag);
            Assert.AreEqual(markdown, firstChild.EquivalentMarkdown);

            var headerRow = firstChild.FirstChild;
            Assert.AreEqual(BlockTag.TableRow, headerRow.Tag);
            Assert.AreEqual("| First Header  | Second Header |\n", headerRow.EquivalentMarkdown);

            var headerCell1 = headerRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, headerCell1.Tag);
            Assert.AreEqual("First Header", headerCell1.EquivalentMarkdown);

            var headerCell2 = headerCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, headerCell2.Tag);
            Assert.AreEqual("Second Header", headerCell2.EquivalentMarkdown);
            Assert.IsNull(headerCell2.NextSibling);

            var firstRow = headerRow.NextSibling;
            Assert.AreEqual(BlockTag.TableRow, firstRow.Tag);
            Assert.AreEqual("| cell #11  | cell #12  |\n", firstRow.EquivalentMarkdown);

            var firstRowCell1 = firstRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, firstRowCell1.Tag);
            Assert.AreEqual("cell #11", firstRowCell1.EquivalentMarkdown);

            var firstRowCell2 = firstRowCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, firstRowCell2.Tag);
            Assert.AreEqual("cell #12", firstRowCell2.EquivalentMarkdown);
            Assert.IsNull(firstRowCell2.NextSibling);

            var secondRow = firstRow.NextSibling;
            Assert.AreEqual(BlockTag.TableRow, secondRow.Tag);
            Assert.AreEqual("| cell #21  | cell #22  |\n", secondRow.EquivalentMarkdown);
            Assert.IsNull(secondRow.NextSibling);

            var secondRowCell1 = secondRow.FirstChild;
            Assert.AreEqual(BlockTag.TableCell, secondRowCell1.Tag);
            Assert.AreEqual("cell #21", secondRowCell1.EquivalentMarkdown);

            var secondRowCell2 = secondRowCell1.NextSibling;
            Assert.AreEqual(BlockTag.TableCell, secondRowCell2.Tag);
            Assert.AreEqual("cell #22", secondRowCell2.EquivalentMarkdown);
            Assert.IsNull(secondRowCell2.NextSibling);
        }

        [TestMethod]
        public void TableCellMismatch()
        {
            var markdown =
@"| First Header  | Second Header |
| ------------- | ------------- |
| 11  |
| 21  | 22  | 23
";

            var ast = CommonMarkConverter.Parse(markdown, ReadSettings);
            string html;
            using (var str = new StringWriter())
            {
                CommonMarkConverter.ProcessStage3(ast, str, WriteSettings);
                html = str.ToString();
            }
            Assert.AreEqual("<table><thead><tr><th>First Header</th><th>Second Header</th></tr></thead><tbody><tr><td>11</td><td></td></tr><tr><td>21</td><td>22</td></tr></tbody></table>\r\n", html);
        }

        [TestMethod]
        public void TableAlignment()
        {
            var markdown =
@"| H1  | H2 | H3 |      H4
 ---    | :--   | ---:|   :-: |
|1|2|3|4|
";

            var ast = CommonMarkConverter.Parse(markdown, ReadSettings);
            var table = ast.FirstChild;
            Assert.AreEqual(BlockTag.Table, table.Tag);
            Assert.AreEqual(4, table.TableHeaderAlignments.Count);
            Assert.AreEqual(TableHeaderAlignment.None, table.TableHeaderAlignments[0]);
            Assert.AreEqual(TableHeaderAlignment.Left, table.TableHeaderAlignments[1]);
            Assert.AreEqual(TableHeaderAlignment.Right, table.TableHeaderAlignments[2]);
            Assert.AreEqual(TableHeaderAlignment.Center, table.TableHeaderAlignments[3]);
            string html;
            using (var str = new StringWriter())
            {
                CommonMarkConverter.ProcessStage3(ast, str, WriteSettings);
                html = str.ToString();
            }
            Assert.AreEqual("<table><thead><tr><th>H1</th><th align=\"left\">H2</th><th align=\"right\">H3</th><th align=\"center\">H4</th></tr></thead><tbody><tr><td>1</td><td align=\"left\">2</td><td align=\"right\">3</td><td align=\"center\">4</td></tr></tbody></table>\r\n", html);
        }

        [TestMethod]
        public void TableInBlockQuote()
        {
            var markdown = @"
> Content before
>
> First Header  | Second Header
> ------------- | -------------
> Content+Cell  | Content-Cell
> Content*Cell  | Content/Cell
>
> More content in the blockquote.";

            var ast = CommonMarkConverter.Parse(markdown, ReadSettings);
            var quote = ast.FirstChild;
            Assert.AreEqual(BlockTag.BlockQuote, quote.Tag);
            var p1 = quote.FirstChild;
            Assert.AreEqual(BlockTag.Paragraph, p1.Tag);
            var table = p1.NextSibling;
            Assert.AreEqual(BlockTag.Table, table.Tag);
            var p2 = table.NextSibling;
            Assert.AreEqual(BlockTag.Paragraph, p2.Tag);
            Assert.IsNull(p2.NextSibling);
            Assert.AreEqual(quote.LastChild, p2);

            Assert.AreEqual("Content before\n", p1.EquivalentMarkdown);
            Assert.AreEqual("More content in the blockquote.", p2.EquivalentMarkdown);

            var row1 = table.FirstChild;
            Assert.AreEqual("First Header  | Second Header\n", row1.EquivalentMarkdown);
            var row2 = row1.NextSibling;
            Assert.AreEqual("Content+Cell  | Content-Cell\n", table.FirstChild.NextSibling.EquivalentMarkdown);
            var row3 = row2.NextSibling;
            Assert.AreEqual("Content*Cell  | Content/Cell\r\n", table.FirstChild.NextSibling.NextSibling.EquivalentMarkdown);
            Assert.IsNull(row3.NextSibling);
            Assert.AreEqual(table.LastChild, row3);

            var c11 = row1.FirstChild;
            Assert.AreEqual("First Header", c11.EquivalentMarkdown);
            var c12 = c11.NextSibling;
            Assert.AreEqual("Second Header", c12.EquivalentMarkdown);
            Assert.IsNull(c12.NextSibling);
            Assert.AreEqual(row1.LastChild, c12);

            var c21 = row2.FirstChild;
            Assert.AreEqual("Content+Cell", c21.EquivalentMarkdown);
            var c22 = c21.NextSibling;
            Assert.AreEqual("Content-Cell", c22.EquivalentMarkdown);
            Assert.IsNull(c22.NextSibling);
            Assert.AreEqual(row2.LastChild, c22);

            var c31 = row3.FirstChild;
            Assert.AreEqual("Content*Cell", c31.EquivalentMarkdown);
            var c32 = c31.NextSibling;
            Assert.AreEqual("Content/Cell", c32.EquivalentMarkdown);
            Assert.IsNull(c32.NextSibling);
            Assert.AreEqual(row3.LastChild, c32);

            string html;
            using (var str = new StringWriter())
            {
                CommonMarkConverter.ProcessStage3(ast, str, WriteSettings);
                html = str.ToString();
            }
            Assert.AreEqual("<blockquote>\r\n<p>Content before</p>\r\n<table><thead><tr><th>First Header</th><th>Second Header</th></tr></thead><tbody><tr><td>Content+Cell</td><td>Content-Cell</td></tr><tr><td>Content*Cell</td><td>Content/Cell</td></tr></tbody></table>\r\n<p>More content in the blockquote.</p>\r\n</blockquote>\r\n\r\n", html);
        }
    }
}
