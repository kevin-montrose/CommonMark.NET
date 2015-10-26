using CommonMark.Syntax;
using System;
using System.Collections.Generic;

namespace CommonMark.Transformers
{
    public abstract class ASTVisitorBase
    {
        protected virtual Block OnBlock(Block block)
        {
            return block;
        }

        protected virtual Inline OnInline(Inline inline)
        {
            return inline;
        }

        static void ReplacementRelative(int curStart, int curEnd, int removeStart, int removeEnd, out bool isBeforeReplacement, out bool isAfterReplacement, out bool isOverlappingReplacement, out bool replaceIsInside)
        {
            isBeforeReplacement = curEnd < removeStart;
            isAfterReplacement = curStart > removeEnd;
            isOverlappingReplacement =
                (removeStart < curStart && removeEnd > curStart && removeEnd < curEnd) ||
                (removeStart > curStart && curEnd > removeStart && removeEnd > curEnd);
            replaceIsInside = curStart <= removeStart && curEnd >= removeEnd;

#if DEBUG
            var numHits =
                (isBeforeReplacement ? 1 : 0) +
                (isAfterReplacement ? 1 : 0) +
                (isOverlappingReplacement ? 1 : 0) +
                (replaceIsInside ? 1 : 0);

            if (numHits != 1) throw new Exception("Math is dodge");
#endif
        }

        static void ReplaceMarkdown(Block root, int removeStart,int removeEnd, string replaceWithMarkdown)
        {
            var adjustmentSize = replaceWithMarkdown.Length - (removeEnd - removeStart);

            var pendingBlocks = new Stack<Block>();
            pendingBlocks.Push(root);

            while(pendingBlocks.Count > 0)
            {
                var curBlock = pendingBlocks.Pop();
                var curBlockStart = curBlock.SourcePosition;
                var curBlockEnd = curBlock.SourcePosition + curBlock.SourceLength;
                
                bool isBeforeReplacementBlock, isAfterReplacementBlock, isOverlappingReplacementBlock, replaceIsInsideBlock;
                ReplacementRelative(curBlockStart, curBlockEnd, removeStart, removeEnd, out isBeforeReplacementBlock, out isAfterReplacementBlock, out isOverlappingReplacementBlock, out replaceIsInsideBlock);

                if (isBeforeReplacementBlock)
                {
                    // nothing to do, even with inlines and children
                    continue;
                }

                if (isAfterReplacementBlock)
                {
                    curBlock.SourcePosition += adjustmentSize;
                    goto handleInlinesAndChildren;
                }

                if (replaceIsInsideBlock)
                {
                    curBlock.SourceLength += adjustmentSize;
                    goto handleInlinesAndChildren;
                }

                if (isOverlappingReplacementBlock)
                {
                    throw new Exception("This shouldn't be possible?!");
                }

                handleInlinesAndChildren:

                // push the next blocks we need to handle, if any
                if (curBlock.NextSibling != null)
                {
                    pendingBlocks.Push(curBlock.NextSibling);
                }

                if(curBlock.FirstChild != null)
                {
                    pendingBlocks.Push(curBlock.FirstChild);
                }

                // time to handle inlines
                var pendingInlines = new Stack<Inline>();
                if (curBlock.InlineContent != null)
                {
                    pendingInlines.Push(curBlock.InlineContent);
                }

                while(pendingInlines.Count > 0)
                {
                    var curInline = pendingInlines.Pop();
                    var curInlineStart = curInline.SourcePosition;
                    var curInlineEnd = curInline.SourcePosition + curInline.SourceLength;

                    bool isBeforeReplacementInline, isAfterReplacementInline, isOverlappingReplacementInline, replaceIsInsideInline;
                    ReplacementRelative(curBlockStart, curBlockEnd, removeStart, removeEnd, out isBeforeReplacementInline, out isAfterReplacementInline, out isOverlappingReplacementInline, out replaceIsInsideInline);

                    if (isBeforeReplacementInline)
                    {
                        // nothing to do, even with inlines and children
                        continue;
                    }

                    if (isAfterReplacementInline)
                    {
                        curBlock.SourcePosition += adjustmentSize;
                    }

                    if (replaceIsInsideInline)
                    {
                        curBlock.SourceLength += adjustmentSize;
                    }

                    if (isOverlappingReplacementInline)
                    {
                        throw new Exception("This shouldn't be possible?!");
                    }

                    if(curInline.NextSibling != null)
                    {
                        pendingInlines.Push(curInline.NextSibling);
                    }

                    if(curInline.FirstChild != null)
                    {
                        pendingInlines.Push(curInline.FirstChild);
                    }
                }
            }
            
            // update the actual markdown string, now that all the references are cleaned up
            root.OriginalMarkdown = root.OriginalMarkdown.Substring(0, removeStart) + replaceWithMarkdown + root.OriginalMarkdown.Substring(removeEnd);
        }

        static void Remove(Block root, Block block)
        {
            var parent = block.Parent;

            // we just removed the root, there's nothing to do
            if (parent == null) return;

            Block prevSibling = null;
            var curSibling = parent.FirstChild;
            while (curSibling != block)
            {
                prevSibling = curSibling;
                curSibling = curSibling.NextSibling;
            }
            
            // remove this once Previous is removed
            if(block.NextSibling != null)
            {
                block.NextSibling.Previous = block.Previous;
            }

            // we removed the first element
            if(prevSibling == null)
            {
                parent.FirstChild = block.NextSibling;
            }
            else
            {
                // skip right over this
                prevSibling.NextSibling = block.NextSibling;
            }

            block.Parent = block.NextSibling = null;

            // remove once Previous is removed
            block.Previous = null;

            // update the underlying markdown
            var startRemoval = block.SourcePosition;
            var stopRemoval = block.SourcePosition + block.SourceLength;
            ReplaceMarkdown(root, startRemoval, stopRemoval, "");
        }

        static void Remove(Block root, Inline inline)
        {
            var parent = inline.Parent;
            
            Inline prevSibling = null;
            var curSibling = parent.InlineContent;
            while (curSibling != inline)
            {
                prevSibling = curSibling;
                curSibling = curSibling.NextSibling;
            }
            
            // we removed the first element
            if (prevSibling == null)
            {
                parent.InlineContent = inline.NextSibling;
            }
            else
            {
                // skip right over this
                prevSibling.NextSibling = inline.NextSibling;
            }

            inline.Parent = null;
            inline.NextSibling = null;

            // update the underlying markdown
            var startRemoval = inline.SourcePosition;
            var stopRemoval = inline.SourcePosition + inline.SourceLength;
            ReplaceMarkdown(root, startRemoval, stopRemoval, "");
        }

        static void Replace(Block root, Block old, Block with)
        {
            var withMarkdown = with.Top.OriginalMarkdown.Substring(with.SourcePosition, with.SourceLength);

            var parent = old.Parent;
            if (parent == null) return; // nothing to do

            // with's parent is now old's parent
            with.Parent = parent;

            Block prevSibling = null;
            var curSibling = parent.FirstChild;
            while(curSibling != old)
            {
                prevSibling = curSibling;
                curSibling = curSibling.NextSibling;
            }

            // it's the first child, so we need to update the parent
            if (prevSibling == null)
            {
                parent.FirstChild = with;
            }
            else
            {
                // it's in the middle or end of the list, so just update the previous
                prevSibling.NextSibling = with;
            }

            with.NextSibling = old.NextSibling;

            // Remove once Previous is removed
            with.Previous = prevSibling;

            // update markdown
            var startRemoval = old.SourcePosition;
            var stopRemoval = old.SourcePosition + old.SourceLength;
            ReplaceMarkdown(root, startRemoval, stopRemoval, withMarkdown);

            with.Top = root;
            with.SourcePosition = startRemoval;
            with.SourceLength = withMarkdown.Length;
        }

        static void Replace(Block root, Inline old, Inline with)
        {
            var withMarkdown = with.Parent.Top.OriginalMarkdown.Substring(with.SourcePosition, with.SourceLength);
            with.Parent = old.Parent;

            Inline prev = null;
            var cur = old.Parent.InlineContent;
            while(cur != old)
            {
                prev = cur;
                cur = cur.NextSibling;
            }

            // it's the first child, so we need to update the parent
            if (prev == null)
            {
                old.Parent.InlineContent = with;
            }
            else
            {
                prev.NextSibling = with;
            }

            with.NextSibling = old.NextSibling;

            var startRemoval = old.SourcePosition;
            var stopRemoval = old.SourcePosition + old.SourceLength;
            ReplaceMarkdown(root, startRemoval, stopRemoval, withMarkdown);

            with.SourcePosition = startRemoval;
            with.SourceLength = withMarkdown.Length;

#if DEBUG
            if(with.EquivalentMarkdown != withMarkdown)
            {
                throw new Exception("uhhhh, replacement failed");
            }
#endif
        }

        public void Visit(Block root)
        {
            if (root == null) throw new ArgumentNullException("root");
            if (root.OriginalMarkdown == null) throw new InvalidOperationException("AST must have been generated with TrackSourcePosition = true to be manipulable");

            var toVisitBlocks = new Stack<Block>();
            toVisitBlocks.Push(root);
            
            while (toVisitBlocks.Count > 0)
            {
                var visiting = toVisitBlocks.Pop();
                var newVisiting = OnBlock(visiting);

                if (newVisiting == null)
                {
                    Remove(root, visiting);
                }
                else
                {
                    if (!object.ReferenceEquals(visiting, newVisiting))
                    {
                        Replace(root, visiting, newVisiting);
                    }

                    if (newVisiting.NextSibling != null)
                    {
                        toVisitBlocks.Push(newVisiting.NextSibling);
                    }

                    var childBlock = newVisiting.FirstChild;
                    if (childBlock != null)
                    {
                        toVisitBlocks.Push(childBlock);
                    }

                    var toVisitInlines = new Stack<Inline>();

                    if (newVisiting.InlineContent != null)
                    {
                        toVisitInlines.Push(newVisiting.InlineContent);
                    }

                    while (toVisitInlines.Count > 0)
                    {
                        var currentInline = toVisitInlines.Pop();
                        var newInline = OnInline(currentInline);

                        if (newInline == null)
                        {
                            Remove(root, currentInline);
                        }
                        else
                        {
                            if (!object.ReferenceEquals(currentInline, newInline))
                            {
                                Replace(root, currentInline, newInline);
                            }

                            if (newInline.NextSibling != null)
                            {
                                toVisitInlines.Push(newInline.NextSibling);
                            }

                            var childInline = newInline.FirstChild;
                            if (childInline != null)
                            {
                                toVisitInlines.Push(childInline);
                            }
                        }
                    }
                }
            }
        }
    }
}
