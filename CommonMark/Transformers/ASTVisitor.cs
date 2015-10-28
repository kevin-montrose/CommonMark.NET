using System;
using System.Collections.Generic;
using CommonMark.Syntax;

namespace CommonMark.Transformers
{
    public abstract class ASTVisitor : ASTVisitorBase
    {
        protected Inline CreateInline(string markdown, CommonMarkSettings settings)
        {
            if (settings == null || !settings.TrackSourcePosition) throw new InvalidOperationException("settings must have TrackSourcePosition = true to manipulate an AST");

            var doc = CommonMarkConverter.Parse(markdown, settings);

            if(doc.FirstChild == null) throw new InvalidOperationException("Couldn't create a singular inline from [" + markdown + "]");

            var inline = doc.FirstChild.InlineContent;
            if(inline == null) throw new InvalidOperationException("Couldn't create a singular inline from [" + markdown + "]");

            return inline;
        }

        protected Block CreateBlock(string markdown, CommonMarkSettings settings)
        {
            if (settings == null || !settings.TrackSourcePosition) throw new InvalidOperationException("settings must have TrackSourcePosition = true to manipulate an AST");

            var doc = CommonMarkConverter.Parse(markdown, settings);

            var block = doc.FirstChild;

            if (block == null) throw new InvalidOperationException("Couldn't create a singular block from [" + markdown + "]");

            if (!object.ReferenceEquals(doc.LastChild, block)) throw new InvalidOperationException("Couldn't create a singular block from [" + markdown + "]");

            return block;
        }

        protected override Inline OnInline(Inline inline)
        {
            switch (inline.Tag)
            {
                case InlineTag.Code: return OnFencedCode(inline);
                case InlineTag.Emphasis: return OnEmphasis(inline);
                case InlineTag.Image: return OnImage(inline);
                case InlineTag.LineBreak: return OnLineBreak(inline);
                case InlineTag.Link: return OnLink(inline);
                case InlineTag.RawHtml: return OnRawHtml(inline);
                case InlineTag.SoftBreak: return OnSoftBreak(inline);
                case InlineTag.Strikethrough: return OnStrikethrough(inline);
                case InlineTag.String: return OnString(inline);
                case InlineTag.Strong: return OnStrong(inline);
                default: throw new CommonMarkException("Unexpected InlineTag [" + inline.Tag + "]");
            }
        }

        protected virtual Inline OnFencedCode(Inline inline)
        {
            return base.OnInline(inline);
        }

        protected virtual Inline OnEmphasis(Inline inline)
        {
            return base.OnInline(inline);
        }

        protected virtual Inline OnImage(Inline inline)
        {
            return base.OnInline(inline);
        }

        protected virtual Inline OnLineBreak(Inline inline)
        {
            return base.OnInline(inline);
        }

        protected virtual Inline OnLink(Inline inline)
        {
            return base.OnInline(inline);
        }

        protected virtual Inline OnRawHtml(Inline inline)
        {
            return base.OnInline(inline);
        }

        protected virtual Inline OnSoftBreak(Inline inline)
        {
            return base.OnInline(inline);
        }

        protected virtual Inline OnStrikethrough(Inline inline)
        {
            return base.OnInline(inline);
        }

        protected virtual Inline OnString(Inline inline)
        {
            return base.OnInline(inline);
        }

        protected virtual Inline OnStrong(Inline inline)
        {
            return base.OnInline(inline);
        }

        protected override Block OnBlock(Block block)
        {
            switch (block.Tag)
            {
                case BlockTag.AtxHeader: return OnAtxHeader(block);
                case BlockTag.BlockQuote: return OnBlockQuote(block);
                case BlockTag.Document: return OnDocument(block);
                case BlockTag.FencedCode: return OnFencedCode(block);
                case BlockTag.HorizontalRuler: return OnHorizontalRuler(block);
                case BlockTag.HtmlBlock: return OnHtmlBlock(block);
                case BlockTag.IndentedCode: return OnIndentedCode(block);
                case BlockTag.List: return OnList(block);
                case BlockTag.ListItem: return OnListItem(block);
                case BlockTag.Paragraph: return OnParagraph(block);
                case BlockTag.ReferenceDefinition: return OnReferenceDefinition(block);
                case BlockTag.SETextHeader: return OnSETextHeader(block);
                case BlockTag.Table: return OnTable(block);
                case BlockTag.TableRow: return OnTableRow(block);
                case BlockTag.TableCell: return OnTableCell(block);
                default: throw new CommonMarkException("Unexpected BlockTag [" + block.Tag + "]");
            }
        }

        protected virtual Block OnAtxHeader(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnBlockQuote(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnDocument(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnFencedCode(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnHorizontalRuler(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnHtmlBlock(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnIndentedCode(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnList(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnListItem(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnParagraph(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnReferenceDefinition(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnSETextHeader(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnTable(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnTableRow(Block block)
        {
            return base.OnBlock(block);
        }

        protected virtual Block OnTableCell(Block block)
        {
            return base.OnBlock(block);
        }
    }
}
