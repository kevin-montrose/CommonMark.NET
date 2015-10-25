using CommonMark.Syntax;
using System;
using System.Collections.Generic;

namespace CommonMark.Transformers
{
    public abstract class ASTVisitor
    {
        public virtual Block OnBlock(Block block)
        {
            return block;
        }

        public virtual Inline OnInline(Inline inline)
        {
            return inline;
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

        public Block Visit(Block root)
        {
            var toVisitBlocks = new Stack<Block>();
            toVisitBlocks.Push(root);

            var ret = root;

            while(toVisitBlocks.Count > 0)
            {
                var visiting = toVisitBlocks.Pop();
                var newVisiting = visiting = OnBlock(visiting);

                if(visiting == root)
                {
                    ret = newVisiting;
                }

                if(!object.ReferenceEquals(visiting, newVisiting))
                {
                    Replace(visiting, newVisiting);
                }
                
                if(newVisiting.NextSibling != null)
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
                
                if(newVisiting.InlineContent != null)
                {
                    toVisitInlines.Push(newVisiting.InlineContent);
                }

                while (toVisitInlines.Count > 0)
                {
                    var currentInline = toVisitInlines.Pop();
                    var newInline = OnInline(currentInline);

                    if (!object.ReferenceEquals(currentInline, newInline))
                    {
                        Replace(currentInline, newInline);
                    }

                    if(newInline.NextSibling != null)
                    {
                        toVisitInlines.Push(newInline.NextSibling);
                    }

                    var childInline = newInline.FirstChild;
                    while(childInline != null)
                    {
                        toVisitInlines.Push(childInline);
                        childInline = childInline.NextSibling;
                    }
                }
            }

            return ret;
        }
    }
}
