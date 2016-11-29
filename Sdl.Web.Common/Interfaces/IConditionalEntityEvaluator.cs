using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface for Conditional Entity Evaluator extension point.
    /// </summary>
    public interface IConditionalEntityEvaluator
    {
        /// <summary>
        /// Determines whether a given Entity Model should be included based on the conditions specified on the Entity Model and the context.
        /// </summary>
        /// <param name="entity">The Entity Model to be evaluated.</param>
        /// <param name="localization">The context Localization</param>
        /// <returns><c>true</c> if the Entity should be included.</returns>
        bool IncludeEntity(EntityModel entity, Localization localization);
    }
}
