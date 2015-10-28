﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CommonMark.Syntax
{
    /// <summary>
    /// Represents a parsed inline element in the document.
    /// </summary>
    [DebuggerDisplay("{OriginalMarkdown != null ? EquivalentMarkdown : ToString()}")]
    public sealed class Inline
    {
        public Block ParentBlock { get; set; }

        public Inline ParentInline { get; set; }

        /// <summary>
        /// Gets or sets the markdown that was parsed to generate this document.
        /// 
        /// This is only set if TrackSourcePosition = true.
        /// </summary>
        public string OriginalMarkdown
        {
            get
            {
                return ParentBlock.Top.OriginalMarkdown;
            }
        }

        public string EquivalentMarkdown
        {
            get
            {
                if (OriginalMarkdown == null) return null;

                return OriginalMarkdown.Substring(SourcePosition, SourceLength);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inline"/> class.
        /// </summary>
        public Inline(Block parent)
        {
            ParentBlock = parent;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inline"/> class.
        /// </summary>
        /// <param name="tag">The type of inline element.</param>
        public Inline(Block parent, InlineTag tag) : this(parent)
        {
            this.Tag = tag;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inline"/> class.
        /// </summary>
        /// <param name="tag">The type of inline element. Should be one of the types that require literal content, for example, <see cref="InlineTag.Code"/>.</param>
        /// <param name="content">The literal contents of the inline element.</param>
        public Inline(Block parent, InlineTag tag, string content) : this(parent)
        {
            this.Tag = tag;
            this.LiteralContent = content;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inline"/> class.
        /// </summary>
        internal Inline(Block parent, InlineTag tag, string content, int startIndex, int length) : this(parent)
        {
            this.Tag = tag;
            this.LiteralContentValue.Source = content;
            this.LiteralContentValue.StartIndex = startIndex;
            this.LiteralContentValue.Length = length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inline"/> class. The element type is set to <see cref="InlineTag.String"/>
        /// </summary>
        /// <param name="content">The literal string contents of the inline element.</param>
        public Inline(Block parent, string content) : this(parent)
        {
            // this is not assigned because it is the default value.
            ////this.Tag = InlineTag.String;

            this.LiteralContent = content;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inline"/> class. The element type is set to <see cref="InlineTag.String"/>
        /// </summary>
        internal Inline(Block parent, string content, int sourcePosition, int sourceLastPosition) : this(parent)
        {
            this.LiteralContent = content;
            this.SourcePosition = sourcePosition;
            this.SourceLastPosition = sourceLastPosition;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inline"/> class. The element type is set to <see cref="InlineTag.String"/>
        /// </summary>
        internal Inline(Block parent, string content, int startIndex, int length, int sourcePosition, int sourceLastPosition) : this(parent)
        {
            this.LiteralContentValue.Source = content;
            this.LiteralContentValue.StartIndex = startIndex;
            this.LiteralContentValue.Length = length; 
            this.SourcePosition = sourcePosition;
            this.SourceLastPosition = sourceLastPosition;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inline"/> class.
        /// </summary>
        /// <param name="tag">The type of inline element. Should be one of the types that contain child elements, for example, <see cref="InlineTag.Emphasis"/>.</param>
        /// <param name="content">The first descendant element of the inline that is being created.</param>
        public Inline(Block parent, InlineTag tag, Inline content) : this(parent)
        {
            this.Tag = tag;
            this.FirstChild = content;
        }

        internal static Inline CreateLink(Block parent, Inline label, string url, string title)
        {
            return new Inline(parent)
            {
                Tag = InlineTag.Link,
                FirstChild = label,
                TargetUrl = url,
                LiteralContent = title
            };
        }

        /// <summary>
        /// Gets of sets the type of the inline element this instance represents.
        /// </summary>
        public InlineTag Tag { get; set; }

        /// <summary>
        /// Gets or sets the literal content of this element. This is only used if the <see cref="Tag"/> property specifies
        /// a type that can have literal content.
        /// 
        /// Note that for <see cref="InlineTag.Link"/> this property contains the title of the link.
        /// </summary>
        public string LiteralContent 
        {
            get 
            {
                // since the .ToString() has been called once, cache the value.
                return this.LiteralContent = this.LiteralContentValue.ToString(); 
            }

            set 
            { 
                this.LiteralContentValue.Source = value; 
                this.LiteralContentValue.StartIndex = 0; 
                this.LiteralContentValue.Length = value == null ? 0 : value.Length;
            }
        }

        internal StringPart LiteralContentValue;

        /// <summary>
        /// Gets or sets the target URL for this element. Only used for <see cref="InlineTag.Link"/> and 
        /// <see cref="InlineTag.Image"/>.
        /// </summary>
        public string TargetUrl { get; set; }

        /// <summary>
        /// The label on a reference used to populate TargetUrl and LiteralContent, tracked for
        /// future rewriting.
        /// </summary>
        internal string TargetUrlAndLiteralContentPopulatedFromReferenceLabel { get; set; }

        Inline _firstChild;
        /// <summary>
        /// Gets or sets the first descendant of this element. This is only used if the <see cref="Tag"/> property specifies
        /// a type that can have nested elements. 
        /// </summary>
        public Inline FirstChild
        {
            get
            {
                return _firstChild;   
            }
            set
            {
                _firstChild = value;
                if (_firstChild != null)
                {
                    _firstChild.ParentInline = this;
                }
            }
        }

        /// <summary>
        /// Gets or sets the position of the element within the source data.
        /// Note that if <see cref="CommonMarkSettings.TrackSourcePosition"/> is not enabled, this property will contain
        /// the position relative to the containing block and not the whole document (not accounting for processing done
        /// in earlier parser stage, such as converting tabs to spaces).
        /// </summary>
        /// <seealso cref="SourceLength"/>
        public int SourcePosition { get; set; }

        internal int SourceLastPosition { get; set; }

        /// <summary>
        /// Gets or sets the length of the element within the source data.
        /// Note that if <see cref="CommonMarkSettings.TrackSourcePosition"/> is not enabled, this property will contain
        /// the length within the containing block (not accounting for processing done in earlier parser stage, such as
        /// converting tabs to spaces).
        /// </summary>
        /// <seealso cref="SourcePosition"/>
        public int SourceLength 
        { 
            get { return this.SourceLastPosition - this.SourcePosition; }
            set { this.SourceLastPosition = this.SourcePosition + value; }
        }

        /// <summary>
        /// Move the whole inline, keeping size the same
        /// </summary>
        internal void AdjustOffset(int delta)
        {
            SourcePosition += delta;
            SourceLastPosition += delta;
        }
        
        /// <summary>
        /// Adjust the size of the inline, keeping the origin point the same
        /// </summary>
        internal void AdjustSize(int delta)
        {
            SourceLength += delta;
        }

        /// <summary>
        /// Gets the link details. This is now obsolete in favor of <see cref="TargetUrl"/> and <see cref="LiteralContent"/>
        /// properties and this property will be removed in future.
        /// </summary>
        [Obsolete("The link properties have been moved to TargetUrl and LiteralContent (previously Title) properties to reduce number of objects created. This property will be removed in future versions.", false)]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public InlineContentLinkable Linkable { get { return new InlineContentLinkable() { Url = this.TargetUrl, Title = this.LiteralContent }; } }

        private Inline _nextSibling;

        /// <summary>
        /// Gets the next sibling inline element. Returns <c>null</c> if this is the last element.
        /// </summary>
        public Inline NextSibling
        {
            get
            {
                return _nextSibling;
            }
            set
            {
                _nextSibling = value;
                if (_nextSibling != null)
                {
                    _nextSibling.ParentInline = ParentInline;
                }
            }
        }

        /// <summary>
        /// Gets the last sibling of this inline. If no siblings are defined, returns self.
        /// </summary>
        public Inline LastSibling
        {
            get
            {
                var x = this._nextSibling;
                if (x == null)
                    return this;

                while (x._nextSibling != null)
                    x = x._nextSibling;

                return x;
            }
        }
    }
}
