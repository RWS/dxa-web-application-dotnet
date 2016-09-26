using System.Collections.Generic;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests
{
    public class MockConditionalEntityEvaluator : IConditionalEntityEvaluator
    {
        private static readonly IList<EntityModel> _evaluatedEntities = new List<EntityModel>();
        private static readonly IList<string> _excludeEntityIds = new List<string>();

        public static IList<EntityModel> EvaluatedEntities
        {
            get { return _evaluatedEntities; }
        }

        public static IList<string> ExcludeEntityIds
        {
            get { return _excludeEntityIds; }
        }

        public bool IncludeEntity(EntityModel entity)
        {
            _evaluatedEntities.Add(entity);
            return !ExcludeEntityIds.Contains(entity.Id);
        }
    }
}
