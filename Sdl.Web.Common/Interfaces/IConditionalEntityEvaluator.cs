using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface for Conditional Entity Evaluator extension point.
    /// </summary>
    // TODO TSI-789: We currently don't provide conditions in the Entity Model yet, so it will be hard to create an implementation.
    public interface IConditionalEntityEvaluator
    {
        /// <summary>
        /// Determines whether a given Entity Model should be included based on the conditions specified on the Entity Model and the context.
        /// </summary>
        /// <param name="entity">The Entity Model to be evaluated.</param>
        /// <returns><c>true</c> if the Entity should be included.</returns>
        bool IncludeEntity(EntityModel entity);
    }
}
