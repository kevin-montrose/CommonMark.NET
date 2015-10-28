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

            /*{
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
            }*/

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
    }
}
