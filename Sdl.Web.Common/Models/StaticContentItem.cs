using System;
using System.IO;
using System.Text;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents a Static Content Item (binary).
    /// </summary>
    public class StaticContentItem : IDisposable
    {
        private Stream _contentStream;

        /// <summary>
        /// Gets the date/time at which the item was last modified.
        /// </summary>
        public DateTime LastModified
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Encoding of the text content.
        /// </summary>
        /// <remarks>
        /// This property is only relevant if the content represents text and will be <c>null</c> otherwise.
        /// </remarks>
        public Encoding TextEncoding
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Content Type (MIME type).
        /// </summary>
        public string ContentType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets direct access to the underlying (binary) content stream.
        /// </summary>
        /// <returns>The Content Stream.</returns>
        /// <remarks>The client should <see cref="IDisposable.Dispose">Dispose</see> either this content Stream or (preferably) the entire StaticContentItem.</remarks>
        public Stream GetContentStream()
        {
            if (_contentStream == null)
            {
                throw new ObjectDisposedException("Content Stream has already been disposed.");
            }
            return _contentStream;
        }

        /// <summary>
        /// Gets the content as text.
        /// </summary>
        /// <returns>The content as text.</returns>
        /// <remarks>This method automatically disposes the underlying content Stream and can hence only be invoked once.</remarks>
        public string GetText()
        {
            if (_contentStream == null)
            {
                throw new ObjectDisposedException("Content Stream has already been disposed. Cannot invoke StaticContentItem.GetText() more than once.");
            }
            if (TextEncoding == null)
            {
                throw new DxaException("Cannot invoke StaticContentItem.GetText() because the item has no Text Encoding.");
            }
            using (StreamReader streamReader = new StreamReader(_contentStream, TextEncoding))
            {
                string result = streamReader.ReadToEnd(); 
                _contentStream.Dispose();
                _contentStream = null;
                return result;
            }
        }

        #region Constructors
        /// <summary>
        /// Initializes a new instance of <see cref="StaticContentItem"/>.
        /// </summary>
        /// <param name="contentStream">The (binary) content Stream.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <param name="lastModified">The date/time the content was last modified.</param>
        /// <param name="textEncoding">In case the Content Stream contains text: the encoding of the Content Stream. If not specified or <c>null</c>, method <see cref="GetText"/> will throw an exception.</param>
        public StaticContentItem(Stream contentStream, string contentType, DateTime lastModified, Encoding textEncoding = null)
        {
            if (contentStream == null)
            {
                throw new DxaException("Content Stream must be provided when constructing Static Content Item.");
            }
            if (string.IsNullOrEmpty(contentType))
            {
                throw new DxaException("Content Type must be provided when constructing Static Content Item.");
            }

            _contentStream = contentStream;
            ContentType = contentType;
            LastModified = lastModified;
            TextEncoding = textEncoding;
        }
        #endregion

        #region IDisposable members
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_contentStream == null)
            {
                // Already disposed.
                return;
            }
            _contentStream.Dispose();
            _contentStream = null;
        }
        #endregion
    }
}
