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
        }
    }
}
