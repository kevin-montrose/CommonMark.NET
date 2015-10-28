using CommonMark.Transformers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using CommonMark.Syntax;
using System.IO;

namespace CommonMark.Tests
{
    [TestClass]
    public class TransformersTests
    {
        static CommonMarkSettings Settings;

        static TransformersTests()
        {
            Settings = CommonMarkSettings.Default.Clone();
            Settings.TrackSourcePosition = true;
        }

        class RemoveBlockQuotesVisitor : ASTVisitor
        {
            protected override Block OnBlockQuote(Block block)
            {
                return null;
            }
        }

        class ReplaceStrongVisitor : ASTVisitor
        {
            protected override Inline OnStrong(Inline inline)
            {
                return CreateInline("hello world", Settings);
            }
        }

        class NumberTableCellsVisitor_1 : ASTVisitor
        {
            public Dictionary<Inline, Inline> Replacements  = new Dictionary<Inline, Inline>();

            protected override Block OnTableCell(Block cell)
            {
                if (cell.InlineContent != null)
                {
                    var row = cell.Parent;
                    var curCell = row.FirstChild;
                    var fromLeft = 1;
                    while (curCell != cell)
                    {
                        curCell = curCell.NextSibling;
                        fromLeft++;
                    }

                    var table = row.Parent;
                    var curRow = table.FirstChild;
                    var fromTop = 1;
                    while (curRow != row)
                    {
                        curRow = curRow.NextSibling;
                        fromTop++;
                    }

                    var newMarkdown = fromTop + "," + fromLeft;

                    var replaceWith = CreateInline(newMarkdown, Settings);

                    Replacements[cell.InlineContent] = replaceWith;
                }

                return base.OnTableCell(cell);
            }
        }

        class NumberTableCellsVisitor_2 : ASTVisitor
        {
            public Dictionary<Inline, Inline> Replacements;

            protected override Inline OnInline(Inline cell)
            {
                Inline replaceWith;
                if (Replacements.TryGetValue(cell, out replaceWith))
                {
                    return replaceWith;
                }

                return base.OnInline(cell);
            }
        }

        class RewriteLinksVisitor : ASTVisitor
        {
            protected override Inline OnLink(Inline inline)
            {
                var markdown = inline.EquivalentMarkdown;
                var startIx = markdown.IndexOf('(');
                if (startIx == -1) return inline;

                var endIx = markdown.LastIndexOf(')');
                if (endIx == -1) return inline;

                var newMarkdown = markdown.Substring(0, startIx + 1) + "http://example.com/" + markdown.Substring(endIx);

                return CreateInline(newMarkdown, Settings);
            }

            protected override Block OnReferenceDefinition(Block block)
            {
                var markdown = block.EquivalentMarkdown;
                var startIx = markdown.IndexOf(": ");
                if (startIx == -1) return block;

                var endIx = markdown.Length - 1;
                while (char.IsWhiteSpace(markdown[endIx])) endIx--;

                var newMarkdown = markdown.Substring(0, startIx + 2) + "http://example.com/" + markdown.Substring(endIx + 1);

                return CreateBlock(newMarkdown, Settings);
            }
        }

        [TestMethod]
        public void Remove()
        {
            var markdown = @"
foo

>**something**
>
> ---
>
> *else*
is it?

bar
";
            var ast = CommonMarkConverter.Parse(markdown, Settings);
            string originalHtml;
            using (var str = new StringWriter())
            {
                CommonMarkConverter.ProcessStage3(ast, str);
                originalHtml = str.ToString();
            }

            (new RemoveBlockQuotesVisitor()).Visit(ast);

            string modifiedHtml;
            using (var str = new StringWriter())
            {
                CommonMarkConverter.ProcessStage3(ast, str);
                modifiedHtml = str.ToString();
            }

            Assert.AreEqual("<p>foo</p>\r\n<blockquote>\r\n<p><strong>something</strong></p>\r\n<hr />\r\n<p><em>else</em>\r\nis it?</p>\r\n</blockquote>\r\n<p>bar</p>\r\n\r\n", originalHtml);
            Assert.AreEqual("<p>foo</p>\r\n<p>bar</p>\r\n\r\n", modifiedHtml);
        }

        [TestMethod]
        public void RoundtripMarkdown()
        {
            {
                var markdown = @"
foo

>**something**
>
> ---
>
> *else*
is it?

bar
";
                
                var ast = CommonMarkConverter.Parse(markdown, Settings);
                var roundtripMarkdown = ast.OriginalMarkdown;
                Assert.AreEqual(markdown, roundtripMarkdown);
            }

            {
                var markdown = @"
foo

>**something**
>
> ---
>
> *else*
is it?

bar
";
                
                var ast = CommonMarkConverter.Parse(markdown, Settings);
                (new RemoveBlockQuotesVisitor()).Visit(ast);

                var withoutBlockQuoteMarkdown = ast.OriginalMarkdown;
                Assert.AreEqual(@"
foo


bar
",
                    withoutBlockQuoteMarkdown
                );
            }

            {
                var markdown = @"
foo

>**something**
>
> ---
>
> *else*
is it?

bar
";
                var ast = CommonMarkConverter.Parse(markdown, Settings);
                (new ReplaceStrongVisitor()).Visit(ast);

                var withReplacement = ast.OriginalMarkdown;
                Assert.AreEqual(@"
foo

>hello world
>
> ---
>
> *else*
is it?

bar
",
                    withReplacement
                );
            }

            {
                var markdown = @"
[foo](http://google.com)

>[**something**](http://google.com)
>
> ---
>
> *else*
is it?

[bar][1]

  [1]: http://google.com
";
                var ast = CommonMarkConverter.Parse(markdown, Settings);
                (new RewriteLinksVisitor()).Visit(ast);

                var withReplacement = ast.OriginalMarkdown;
                Assert.AreEqual(@"
[foo](http://example.com/)

>[**something**](http://example.com/)
>
> ---
>
> *else*
is it?

[bar][1]

  [1]: http://example.com/
",
                    withReplacement
                );

                string html;
                using (var str = new StringWriter())
                {
                    CommonMarkConverter.ProcessStage3(ast, str);
                    html = str.ToString();
                }
                Assert.AreEqual(
                    "<p><a href=\"http://example.com/\">foo</a></p>\r\n<blockquote>\r\n<p><a href=\"http://example.com/\"><strong>something</strong></a></p>\r\n<hr />\r\n<p><em>else</em>\r\nis it?</p>\r\n</blockquote>\r\n<p><a href=\"http://example.com/\">bar</a></p>\r\n\r\n", 
                    html
                );
            }
        }

        [TestMethod]
        public void NumberTableCells()
        {
            var markdown = @"
Let's play around with something trickier.

## Table begins

| Column #1 | Column #2 | Column #3 | Etc.
  --------- |   ---     |   ----    | ---:
a | b | c | d
e | f | g | h";

            var settings = Settings.Clone();
            settings.AdditionalFeatures |= CommonMarkAdditionalFeatures.GithubStyleTables;

            var ast = CommonMarkConverter.Parse(markdown, settings);
            var discoverer = new NumberTableCellsVisitor_1();
            discoverer.Visit(ast);
            
            var replacer = new NumberTableCellsVisitor_2();
            replacer.Replacements = discoverer.Replacements;
            replacer.Visit(ast);

            var transformedMarkdown = ast.OriginalMarkdown;

            // this is all fucked atm
            Assert.AreEqual("", transformedMarkdown);

            string html;
            using (var str = new StringWriter())
            {
                var nopos = CommonMarkSettings.Default.Clone();
                nopos.AdditionalFeatures |= CommonMarkAdditionalFeatures.GithubStyleTables;

                CommonMarkConverter.ProcessStage3(ast, str, nopos);
                html = str.ToString();
            }

            Assert.AreEqual("<p>Let's play around with something trickier.</p>\r\n<h2>Table begins</h2>\r\n<table><thead><tr><th>1,1</th><th>1,2</th><th>1,3</th><th align=\"right\">1,4</th></tr></thead><tbody><tr><td>2,1</td><td>2,2</td><td>2,3</td><td align=\"right\">2,4</td></tr><tr><td>3,1</td><td>3,2</td><td>3,3</td><td align=\"right\">3,4</td></tr></tbody></table>\r\n", html);
        }
    }
}
