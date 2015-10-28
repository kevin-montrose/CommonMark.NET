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

        static void FindNarrowestBlockAndInline(Block root, int removeStart, int removeEnd, out Block narrowestBlock, out Inline narrowestInline, out IEnumerable<Inline> narrowestInlineParentInlines)
        {
            narrowestBlock = root;

            var pendingBlocks = new Stack<Block>();
            pendingBlocks.Push(narrowestBlock);

            while (pendingBlocks.Count > 0)
            {
                var curBlock = pendingBlocks.Pop();

                // we always needs to look at the sibling blocks, no matter what, so go ahead and queue it
                if (curBlock.NextSibling != null)
                {
                    pendingBlocks.Push(curBlock.NextSibling);
                }

                var curBlockStart = curBlock.SourcePosition;
                var curBlockEnd = curBlock.SourcePosition + curBlock.SourceLength;

                bool isBeforeReplacementBlock, isAfterReplacementBlock, isOverlappingReplacementBlock, replaceIsInsideBlock;
                ReplacementRelative(curBlockStart, curBlockEnd, removeStart, removeEnd, out isBeforeReplacementBlock, out isAfterReplacementBlock, out isOverlappingReplacementBlock, out replaceIsInsideBlock);

                if (isBeforeReplacementBlock || isAfterReplacementBlock)
                {
                    continue;
                }

                if (isOverlappingReplacementBlock || !replaceIsInsideBlock)
                {
                    throw new Exception("This shouldn't be possible?!");
                }

                if (curBlock.SourceLength < narrowestBlock.SourceLength)
                {
                    narrowestBlock = curBlock;
                }

                if (curBlock.FirstChild != null)
                {
                    pendingBlocks.Push(curBlock.FirstChild);
                }
            }

            narrowestInline = narrowestBlock.InlineContent;

            var pendingInlines = new Stack<Inline>();
            if (narrowestInline != null)
            {
                pendingInlines.Push(narrowestInline);
            }

            while (pendingInlines.Count > 0)
            {
                var curInline = pendingInlines.Pop();

                // we always needs to look at the sibling blocks, no matter what, so go ahead and queue it
                if (curInline.NextSibling != null)
                {
                    pendingInlines.Push(curInline.NextSibling);
                }

                var curInlineStart = curInline.SourcePosition;
                var curInlineEnd = curInline.SourcePosition + curInline.SourceLength;

                bool isBeforeReplacement, isAfterReplacement, isOverlappingReplacement, replaceIsInside;
                ReplacementRelative(curInlineStart, curInlineEnd, removeStart, removeEnd, out isBeforeReplacement, out isAfterReplacement, out isOverlappingReplacement, out replaceIsInside);

                if (isBeforeReplacement || isAfterReplacement)
                {
                    continue;
                }

                if (isOverlappingReplacement || !replaceIsInside)
                {
                    throw new Exception("This shouldn't be possible?!");
                }

                if (curInline.SourceLength < narrowestBlock.SourceLength)
                {
                    narrowestInline = curInline;

                    if (narrowestInline.FirstChild != null)
                    {
                        pendingInlines.Push(narrowestInline.FirstChild);
                    }
                }
            }

            narrowestInlineParentInlines = null;
            if (narrowestInline != null)
            {
                narrowestInlineParentInlines = FindParentInlines(narrowestBlock, narrowestInline);
            }
        }

        static IEnumerable<Inline> FindParentInlines(Block parentBlock, Inline ofInline)
        {
            var stack = new Stack<Inline>();
            var child = parentBlock.InlineContent;
            while (child != null)
            {
                var ret = Trace(child, ofInline, stack);
                if (ret != null) return ret;

                child = child.NextSibling;
            }

            return stack;
        }

        static IEnumerable<Inline> Trace(Inline from, Inline to, Stack<Inline> path)
        {
            path.Push(from);

            var child = from.FirstChild;

            while (child != null)
            {
                if (child == to)
                {
                    return path;
                }

                var ret = Trace(child, to, path);
                if (ret != null) return ret;

                child = child.NextSibling;
            }

            path.Pop();
            return null;
        }

        static void VisitSelfAndChildren(Inline forInline, Action<Inline> onInline)
        {
            onInline(forInline);

            var pending = new Stack<Inline>();
            if (forInline.FirstChild != null)
            {
                pending.Push(forInline.FirstChild);
            }

            while(pending.Count > 0)
            {
                var cur = pending.Pop();
                onInline(cur);
                if(cur.NextSibling != null)
                {
                    pending.Push(cur.NextSibling);
                }
                if(cur.FirstChild != null)
                {
                    pending.Push(cur.FirstChild);
                }
            }
        }

        static void VisitSelfAndChildren(Block forBlock, Action<Block> onBlock, Action<Inline> onInline)
        {
            Action<Block> visitInlinesOfBlock =
                block =>
                {
                    var curInline = block.InlineContent;
                    while(curInline != null)
                    {
                        VisitSelfAndChildren(curInline, onInline);
                        curInline = curInline.NextSibling;
                    }
                };

            onBlock(forBlock);

            var pendingChildBlocks = new Stack<Block>();
            if(forBlock.FirstChild != null)
            {
                pendingChildBlocks.Push(forBlock.FirstChild);
            }

            while (pendingChildBlocks.Count > 0)
            {
                var curChild = pendingChildBlocks.Pop();
                onBlock(curChild);
                
                if(curChild.NextSibling != null)
                {
                    pendingChildBlocks.Push(curChild.NextSibling);
                }

                if(curChild.FirstChild != null)
                {
                    pendingChildBlocks.Push(curChild.FirstChild);
                }

                visitInlinesOfBlock(curChild);
            }

            visitInlinesOfBlock(forBlock);
        }

        static void AdjustOffsetsAndSizes(Block firstSiblingBlock, Inline firstSiblingInline, Block firstParentBlock, Inline firstParentInline, int adjustmentSize)
        {
            var blocksNeedingOffsetAdjustment = new List<Block>();
            var inlinesNeedingOffsetAdjustment = new List<Inline>();

            var blocksNeedingSizeAdjustment = new List<Block>();
            var inlinesNeedingSizeAdjustment = new List<Inline>();

            // adjust offset of sibling blocks
            //   all of their children must also be offset
            {
                var sibling = firstSiblingBlock;
                while (sibling != null)
                {
                    blocksNeedingOffsetAdjustment.Add(sibling);
                    
                    sibling = sibling.NextSibling;
                }
            }

            // adjust offset of sibling inlines
            //   all of their children must also be offset
            {
                var sibling = firstSiblingInline;
                while (sibling != null)
                {
                    inlinesNeedingOffsetAdjustment.Add(sibling);

                    sibling = sibling.NextSibling;
                }
            }

            // adjust the size of the parent blocks,
            //    *and* the _offsets_ of their siblings
            {
                var parentBlock = firstParentBlock;
                while(parentBlock != null)
                {
                    blocksNeedingSizeAdjustment.Add(parentBlock);

                    var sibling = parentBlock.NextSibling;
                    while(sibling != null)
                    {
                        blocksNeedingOffsetAdjustment.Add(sibling);
                        sibling = sibling.NextSibling;
                    }

                    parentBlock = parentBlock.Parent;
                }
            }

            // adjust the size of the parent inlines,
            //   *and* the _offsets_ of their siblings
            {
                var parentInline = firstParentInline;
                while(parentInline != null)
                {
                    inlinesNeedingSizeAdjustment.Add(parentInline);

                    var sibling = parentInline.NextSibling;
                    while(sibling != null)
                    {
                        inlinesNeedingOffsetAdjustment.Add(sibling);
                        sibling = sibling.NextSibling;
                    }

                    parentInline = parentInline.ParentInline;
                }
            }

            foreach(var block in blocksNeedingOffsetAdjustment)
            {
                VisitSelfAndChildren(
                    block,
                    b =>
                    {
                        b.AdjustOffset(adjustmentSize);
                    },
                    i =>
                    {
                        i.AdjustOffset(adjustmentSize);
                    }
                );
            }

            foreach(var block in blocksNeedingSizeAdjustment)
            {
                VisitSelfAndChildren(
                    block,
                    b =>
                    {
                        b.AdjustSize(adjustmentSize);
                    },
                    i =>
                    {
                        //i.AdjustSize(adjustmentSize);
                    }
                );
            }

            foreach (var inline in inlinesNeedingOffsetAdjustment)
            {
                VisitSelfAndChildren(
                    inline,
                    i =>
                    {
                        i.AdjustOffset(adjustmentSize);
                    }
                );
            }

            foreach (var inline in inlinesNeedingSizeAdjustment)
            {
                VisitSelfAndChildren(
                    inline,
                    i =>
                    {
                        i.AdjustSize(adjustmentSize);
                    }
                );
            }
        }

        static void Remove(Block root, Block block)
        {
            throw new NotImplementedException();

            /*var parent = block.Parent;

            // we just removed the root, there's nothing to do
            if (parent == null) return;

            Block prevSibling = null;
            var curSibling = parent.FirstChild;
            while (curSibling != block)
            {
                prevSibling = curSibling;
                curSibling = curSibling.NextSibling;
            }
            
            // we removed the first element
            if(prevSibling == null)
            {
                parent.FirstChild = block.NextSibling;
            }
            else
            {
                // skip right over block
                prevSibling.NextSibling = block.NextSibling;
            }

            block.Parent = block.NextSibling = null;
            
            // update the underlying markdown
            var startRemoval = block.SourcePosition;
            var stopRemoval = block.SourcePosition + block.SourceLength;
            ReplaceMarkdown(root, startRemoval, stopRemoval, "");

            // remove any link definitions defined
            VisitSelfAndChildren(
                block,
                b =>
                {
                    if(b.Tag == BlockTag.ReferenceDefinition)
                    {
                        if(b.DefinesReferenceLabels != null)
                        {
                            foreach(var label in b.DefinesReferenceLabels)
                            {
                                root.ReferenceMap.Remove(label);
                            }
                        }
                    }
                },
                _ => { }
            );*/
        }

        static void Remove(Block root, Inline inline)
        {
            throw new NotImplementedException();

            /*var parent = inline.ParentBlock;
            
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

            inline.ParentBlock = null;
            inline.NextSibling = null;

            // update the underlying markdown
            var startRemoval = inline.SourcePosition;
            var stopRemoval = inline.SourcePosition + inline.SourceLength;
            ReplaceMarkdown(root, startRemoval, stopRemoval, "");*/
        }

        static void Replace(Block root, Block old, Block with)
        {
            // copy out the reference map in with
            var definedInWith = with.Top.ReferenceMap;

            // get the markdown details
            var oldMarkdown = old.EquivalentMarkdown;
            var startRemoval = old.SourcePosition;
            var stopRemoval = old.SourcePosition + old.SourceLength;
            var withMarkdown = with.Top.OriginalMarkdown.Substring(with.SourcePosition, with.SourceLength);
            
            // find where to insert into the sibling chain
            Block prev = null;
            var cur = old.Parent.FirstChild;
            while (cur != old)
            {
                prev = cur;
                cur = cur.NextSibling;
            }

            // now start update the AST
            with.Parent = old.Parent;
            with.Top = old.Top;

            // it's the first child, so we need to update the parent
            if (prev == null)
            {
                with.Parent.FirstChild = with;
            }
            else
            {
                prev.NextSibling = with;
            }

            with.NextSibling = old.NextSibling;
            with.SourcePosition = old.SourcePosition;
            with.SourceLastPosition = old.SourceLastPosition;

            // fixup the other blocks & inlines that still exist in the document
            AdjustOffsetsAndSizes(with.NextSibling, null, with.Parent, null, withMarkdown.Length - oldMarkdown.Length);

            // update with to match it's real content
            with.SourceLength += withMarkdown.Length - oldMarkdown.Length;
            
            // finally, rewrite the original markdown
            with.Top.OriginalMarkdown = old.Top.OriginalMarkdown.Substring(0, startRemoval) + withMarkdown + with.Top.OriginalMarkdown.Substring(stopRemoval);

            // remove references from the old block
            VisitSelfAndChildren(
                old,
                b =>
                {
                    if (b.Tag == BlockTag.ReferenceDefinition)
                    {
                        if (b.DefinesReferenceLabels != null)
                        {
                            foreach (var label in b.DefinesReferenceLabels)
                            {
                                root.ReferenceMap.Remove(label);
                            }
                        }
                    }
                },
                i => { }
            );

            // add references fromt he new block
            VisitSelfAndChildren(
                with,
                b =>
                {
                    if (b.Tag == BlockTag.ReferenceDefinition)
                    {
                        if (b.DefinesReferenceLabels != null)
                        {
                            foreach (var label in b.DefinesReferenceLabels)
                            {
                                root.ReferenceMap[label] = definedInWith[label];
                            }
                        }
                    }
                },
                i => { }
            );

#if DEBUG
            if(with.EquivalentMarkdown != withMarkdown)
            {
                throw new Exception("uhhhh, replacement failed");
            }
#endif
        }

        static void Replace(Block root, Inline old, Inline with)
        {
            // get the markdown details
            var oldMarkdown = old.EquivalentMarkdown;
            var startRemoval = old.SourcePosition;
            var stopRemoval = old.SourcePosition + old.SourceLength;
            var withMarkdown = with.ParentBlock.Top.OriginalMarkdown.Substring(with.SourcePosition, with.SourceLength);

            // find where to insert into the sibling chain
            Inline prev = null;
            var cur = old.ParentBlock.InlineContent;
            while(cur != old)
            {
                prev = cur;
                cur = cur.NextSibling;
            }

            // now start update the AST
            with.ParentBlock = old.ParentBlock;
            
            // it's the first child, so we need to update the parent
            if (prev == null)
            {
                with.ParentBlock.InlineContent = with;
            }
            else
            {
                prev.NextSibling = with;
            }

            with.NextSibling = old.NextSibling;
            with.SourcePosition = old.SourcePosition;
            with.SourceLastPosition = old.SourceLastPosition;

            var markdownAdjustment = withMarkdown.Length - oldMarkdown.Length;

            // fixup the other blocks & inlines that still exist in the document
            AdjustOffsetsAndSizes(null, with.NextSibling, with.ParentBlock, with.ParentInline, markdownAdjustment);

            with.AdjustSize(markdownAdjustment);

            // finally, rewrite the original markdown
            with.ParentBlock.Top.OriginalMarkdown = with.ParentBlock.Top.OriginalMarkdown.Substring(0, startRemoval) + withMarkdown + with.ParentBlock.Top.OriginalMarkdown.Substring(stopRemoval);

#if DEBUG
            if (with.EquivalentMarkdown != withMarkdown)
            {
                throw new Exception("uhhhh, replacement failed");
            }
#endif
        }

        void Rewrite(Block root)
        {
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

        static void FixupReferences(Block root)
        {
            var refMap = root.ReferenceMap;

            if (refMap == null) return;

            VisitSelfAndChildren(
                root,
                block =>
                {

                },
                inline =>
                {
                    var refd = inline.TargetUrlAndLiteralContentPopulatedFromReferenceLabel;
                    if (refd == null) return;

                    Reference details;
                    if(!refMap.TryGetValue(refd, out details))
                    {
                        throw new CommonMarkException("Rewrite left a dangling reference [" + refd + "]");
                    }

                    inline.TargetUrl = details.Url;
                    inline.LiteralContent = details.Title;
                }
            );
        }

        public void Visit(Block root)
        {
            if (root == null) throw new ArgumentNullException("root");
            if (root.OriginalMarkdown == null) throw new InvalidOperationException("AST must have been generated with TrackSourcePosition = true to be manipulable");
            if (root.NextSibling != null) throw new InvalidOperationException("Visit must start at the root of an AST");

            Rewrite(root);
            FixupReferences(root);
        }
    }
}
