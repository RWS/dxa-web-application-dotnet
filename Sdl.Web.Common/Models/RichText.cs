using System;
using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents Rich Text which can be a mix of HTML fragments and Entity Models.
    /// </summary>
    /// <remarks>
    /// <see cref="IRichTextProcessor.ProcessRichText"/> converts rich text XHTML into a <see cref="RichText"/> instance.
    /// This may be mapped to a View Model property of type <see cref="string"/>, in which case <see cref="RichText.ToString()"/> is used to render the HTML during the model mapping.
    /// If <see cref="RichText"/> contains any Entity Models, these will be rendered using their <see cref="EntityModel.ToHtml()"/> method.
    /// Preferably, the View Model property is of type <see cref="RichText"/> and the View uses @Html.DxaRichText to render the HTML.
    /// In the latter case, if the <see cref="RichText"/> contains any Entity Models, these will be rendered using an appropriate View.
    /// </remarks>
    [Serializable]
    public class RichText
    {
        /// <summary>
        /// Gets the fragments (HTML fragments or Entity Models) of which the rich text consists.
        /// </summary>
        public IEnumerable<IRichTextFragment> Fragments
        {
            get;
            private set;
        }

        #region Constructors

        internal RichText()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="RichText"/> instance for a given piece of HTML.
        /// </summary>
        /// <param name="html">The piece of HTML.</param>
        public RichText(string html)
        {
            Fragments = new[] { new RichTextFragment(html) };
        }

        /// <summary>
        /// Initializes a new <see cref="RichText"/> instance for a given set of fragments.
        /// </summary>
        /// <param name="fragments"></param>
        public RichText(IEnumerable<IRichTextFragment> fragments)
        {
            Fragments = fragments ?? new IRichTextFragment[0];
        }

        #endregion

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string containing the rich text as HTML.</returns>
        public override string ToString()
        {
            return string.Concat(Fragments.Select(f => f.ToHtml()));
        }

        /// <summary>
        /// Determines whether the <see cref="RichText"/> instance is empty.
        /// </summary>
        /// <returns><c>true</c> if the <see cref="RichText"/> instance is empty.</returns>
        public bool IsEmpty()
        {
            // TODO: implementation is not 100% correct: first fragment could be empty, but there could be more fragments. Good enough for now.
            IRichTextFragment firstFragment = Fragments.FirstOrDefault();
            return (firstFragment == null) || (firstFragment.ToHtml().Length == 0);
        }

        /// <summary>
        /// Determines whether a given <see cref="RichText"/> instance is <c>null</c> or empty.
        /// </summary>
        /// <param name="richText">The <see cref="RichText"/> instance to test.</param>
        /// <returns><c>true</c> if <paramref name="richText"/> is <c>null</c> or empty.</returns>
        /// <remarks>This is a cheaper alternative to <see cref="string.IsNullOrEmpty"/> on a <see cref="RichText"/> instance using implicit string cast.</remarks>
        public static bool IsNullOrEmpty(RichText richText)
        {
            return (richText == null) || richText.IsEmpty();
        }
    }

    /// <summary>
    /// Interface implemented by class <see cref="RichTextFragment"/> and <see cref="EntityModel"/> to allow a mix of these in <see cref="RichText"/>.
    /// </summary>
    public interface IRichTextFragment
    {
        /// <summary>
        /// Renders the rich text fragment as HTML.
        /// </summary>
        /// <returns>The rendered HTML.</returns>
        string ToHtml();
    }

    /// <summary>
    /// Represents a rich text HTML fragment.
    /// </summary>
    [Serializable]
    public class RichTextFragment : IRichTextFragment
    {
        /// <summary>
        /// The piece of HTML which this fragment represents.
        /// </summary>
        public string Html
        {
            get;
            private set;
        }

        #region Constructors

        internal RichTextFragment()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="RichTextFragment"/> instance for a given piece of HTML.
        /// </summary>
        /// <param name="html">The piece of HTML.</param>
        public RichTextFragment(string html)
        {
            Html = html;
        }
        #endregion

        #region IRichTextFragment Members

        /// <summary>
        /// Renders the rich text fragment as HTML.
        /// </summary>
        /// <returns>The HTML which this fragment represents.</returns>
        public string ToHtml()
        {
            return Html;
        }

        #endregion

        #region Overrides
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string containing the rich text fragment.</returns>
        public override string ToString()
        {
            return Html;
        }
        #endregion
    }
}
