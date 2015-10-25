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

        static void Remove(Block block)
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
        }

        static void Remove(Inline inline)
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
        }

        static void Replace(Block old, Block with)
        {
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
        }

        static void Replace(Inline old, Inline with)
        {
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
        }

        public void Visit(Block root)
        {
            var toVisitBlocks = new Stack<Block>();
            toVisitBlocks.Push(root);
            
            while (toVisitBlocks.Count > 0)
            {
                var visiting = toVisitBlocks.Pop();
                var newVisiting = OnBlock(visiting);

                if (newVisiting == null)
                {
                    Remove(visiting);
                }
                else
                {
                    if (!object.ReferenceEquals(visiting, newVisiting))
                    {
                        Replace(visiting, newVisiting);
                    }

                    if (newVisiting.NextSibling != null)
                    {
                        toVisitBlocks.Push(newVisiting.NextSibling);
                    }

                    var childBlock = newVisiting.FirstChild;
                    while (childBlock != null)
                    {
                        toVisitBlocks.Push(childBlock);
                        childBlock = childBlock.NextSibling;
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
                            Remove(currentInline);
                        }
                        else
                        {
                            if (!object.ReferenceEquals(currentInline, newInline))
                            {
                                Replace(currentInline, newInline);
                            }

                            if (newInline.NextSibling != null)
                            {
                                toVisitInlines.Push(newInline.NextSibling);
                            }

                            var childInline = newInline.FirstChild;
                            while (childInline != null)
                            {
                                toVisitInlines.Push(childInline);
                                childInline = childInline.NextSibling;
                            }
                        }
                    }
                }
            }
        }
    }
}
