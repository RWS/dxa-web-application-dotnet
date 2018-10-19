using Newtonsoft.Json;
using System;

namespace Sdl.Web.Common.Models.Entity
{

    /// <summary>
    /// Represents a generic Tridion Docs Topic.
    /// </summary>
    /// <remarks>
    /// This is the result of default DXA semantic mapping.
    /// Since all the Topic data is rendered as HTML, it may not be the most practical to work with in an MVC Web Application.
    /// This generic Topic can be transformed into a user-defined, Strongly Typed Topic Model using an additional Model Builder: the "StronglyTypedTopicBuilder".
    /// </remarks>
    [Serializable]
    public class Topic : EntityModel
    {
        /// <summary>
        /// Gets or sets the topic title.
        /// </summary>
        [SemanticProperty("topicTitle")]
        [JsonProperty(PropertyName = "topicTitle")] // DDWebApp expects camel case
        public string TopicTitle { get; set; }

        /// <summary>
        /// Gets or sets the topic body.
        /// </summary>
        /// <remarks>
        /// The topic body is an XHTML fragment which contains _all_ DITA properties (incl. title, body, related-links, nested topics)
        /// </remarks>
        [SemanticProperty("topicBody")]
        [JsonProperty(PropertyName = "topicBody")] // DDWebApp expects camel case
        public string TopicBody { get; set; }
    }
}
