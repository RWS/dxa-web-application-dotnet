using Sdl.Web.Common.Logging;
using System;

namespace Sdl.Web.Common.Models
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
    [SemanticEntity("Topic")]
    public class GenericTopic : EntityModel
    {
        /// <summary>
        /// Gets or sets the topic title.
        /// </summary>
        [SemanticProperty("topicTitle")]
        public string TopicTitle { get; set; }

        /// <summary>
        /// Gets or sets the topic body.
        /// </summary>
        /// <remarks>
        /// The topic body is an XHTML fragment which contains _all_ DITA properties (incl. title, body, related-links, nested topics)
        /// </remarks>
        [SemanticProperty("topicBody")]
        public string TopicBody { get; set; }

        /// <summary>
        /// Registers this View Model Type.
        /// </summary>
        /// <remarks>
        /// Although this View Model Type is part of the DXA Framework, it has to be registered like any other View Model Type.
        /// In order to work with Tridion Docs content, it will be associated with specific MVC data.
        /// A DXA Web Application/Module that wants to work with Tridion Docs content should call this method
        /// unless it defines its own View Model Type for generic Topics.
        /// </remarks>
        public static void Register()
        {
            using (new Tracer())
            {
                ModelTypeRegistry.RegisterViewModel(new MvcData("Ish:Entity:Topic"), typeof(GenericTopic));
            }
        }
    }
}
