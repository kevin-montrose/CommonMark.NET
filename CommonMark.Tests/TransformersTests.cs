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
        class RemoveVisitor : ASTVisitor
        {
            protected override Block OnBlockQuote(Block block)
            {
                return null;
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

            var ast = CommonMarkConverter.Parse(markdown);
            string originalHtml;
            using (var str = new StringWriter())
            {
                CommonMarkConverter.ProcessStage3(ast, str);
                originalHtml = str.ToString();
            }

            (new RemoveVisitor()).Visit(ast);

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

            var settings = CommonMarkSettings.Default.Clone();
            settings.TrackSourcePosition = true;
            var ast = CommonMarkConverter.Parse(markdown, settings);
            var roundtripMarkdown = CommonMarkConverter.ToMarkdown(ast);
            Assert.AreEqual(markdown, roundtripMarkdown);

            (new RemoveVisitor()).Visit(ast);

            var withoutBlockQuoteMarkdown = CommonMarkConverter.ToMarkdown(ast);
            Assert.AreEqual(@"
foo


bar
",
                withoutBlockQuoteMarkdown
            );
        }
    }
}
