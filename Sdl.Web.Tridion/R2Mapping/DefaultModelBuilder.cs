using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;

namespace Sdl.Web.Tridion.R2Mapping
{
    /// <summary>
    /// Default Page and Entity Model Builder implementation (based on DXA R2 Data Model).
    /// </summary>
    public class DefaultModelBuilder : IPageModelBuilder, IEntityModelBuilder
    {
        private static readonly Regex _tcmUriRegEx = new Regex(@"tcm:\d+-\d+(-\d+)?", RegexOptions.Compiled);

        /// <summary>
        /// Builds a strongly typed Page Model from a given DXA R2 Data Model.
        /// </summary>
        /// <param name="pageModelData">The DXA R2 Data Model.</param>
        /// <param name="includePageRegions">Indicates whether Include Page Regions should be included.</param>
        /// <param name="inputPageModel">Strongly typed Page Model created by preceding Page Model Builder; is <c>null</c> for the first Page Model Builder in the chain.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The strongly typed Page Model.</returns>
        public PageModel BuildPageModel(PageModelData pageModelData, bool includePageRegions, PageModel inputPageModel, Localization localization)
        {
            using (new Tracer(pageModelData, inputPageModel, localization))
            {
                // TODO: instantiate the right type & semantic mappings
                PageModel result = new PageModel(pageModelData.Id)
                {
                    ExtensionData = pageModelData.ExtensionData,
                    HtmlClasses = pageModelData.HtmlClasses,
                    MvcData = CreateMvcData(pageModelData.MvcData, "PageModel"),
                    XpmMetadata = pageModelData.XpmMetadata,
                    Meta = ResolveMetaLinks(pageModelData.Meta), // TODO TSI-1267: Link Resolving should eventually be done in Model Service. 
                    Title = pageModelData.Title
                };

                if (pageModelData.Regions != null)
                {
                    IEnumerable<RegionModelData> regions = includePageRegions ? pageModelData.Regions : pageModelData.Regions.Where(r => r.IncludePageUrl == null);
                    result.Regions.UnionWith(regions.Select(data => CreateRegionModel(data, localization)));
                }

                return result;
            }
        }

        /// <summary>
        /// Builds a strongly typed Entity Model based on a given DXA R2 Data Model.
        /// </summary>
        /// <param name="entityModelData">The DXA R2 Data Model.</param>
        /// <param name="baseModelType">The base type for the Entity Model to build.</param>
        /// <param name="inputEntityModel">Strongly typed Entity Model created by preceding Entity Model Builder; is <c>null</c> for the first Entity Model Builder in the chain.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The strongly typed Entity Model. Will be of type <paramref name="baseModelType"/> or a subclass.</returns>
        public EntityModel BuildEntityModel(EntityModelData entityModelData, Type baseModelType, EntityModel inputEntityModel, Localization localization)
        {
            using (new Tracer(entityModelData, baseModelType, inputEntityModel, localization))
            {
                Common.Models.MvcData mvcData = CreateMvcData(entityModelData.MvcData, "EntityModel");

                Type modelType;
                bool isEmbedded;
                if (mvcData == null)
                {
                    // Embedded Entity Models do not have MvcData and model type should be derived from the Schema.
                    SemanticSchema semanticSchema = SemanticMapping.GetSchema(entityModelData.SchemaId, localization);
                    modelType = semanticSchema.GetModelTypeFromSemanticMapping(baseModelType);
                    isEmbedded = true;
                }
                else
                {
                    modelType = ModelTypeRegistry.GetViewModelType(mvcData);
                    isEmbedded = false;
                }

                EntityModel result = (EntityModel) Activator.CreateInstance(modelType);

                result.ExtensionData = entityModelData.ExtensionData;
                result.HtmlClasses = entityModelData.HtmlClasses;
                result.MvcData = mvcData;
                result.XpmMetadata = entityModelData.XpmMetadata;
                result.Id = entityModelData.Id;

                MediaItem mediaItem = result as MediaItem;
                if (mediaItem != null)
                {
                    BinaryContentData binaryContent = entityModelData.BinaryContent;
                    if (binaryContent == null)
                    {
                        throw new DxaException(
                            $"Unable to create Media Item ('{modelType}') because the Entity Model '{entityModelData.Id}' does not contain Binary Content Data."
                            );
                    }
                    mediaItem.Url = binaryContent.Url;
                    mediaItem.FileName = binaryContent.FileName;
                    mediaItem.MimeType = binaryContent.MimeType;
                    mediaItem.FileSize = binaryContent.FileSize;
                    mediaItem.IsEmbedded = isEmbedded;
                }

                EclItem eclItem = result as EclItem;
                if (eclItem != null)
                {
                    ExternalContentData externalContent = entityModelData.ExternalContent;
                    if (externalContent == null)
                    {
                        throw new DxaException(
                            $"Unable to create ECL Item ('{modelType}') because the Entity Model '{entityModelData.Id}' does not contain External Content Data."
                            );
                    }
                    eclItem.EclDisplayTypeId = externalContent.DisplayTypeId;
                    eclItem.EclExternalMetadata = externalContent.Metadata;
                    eclItem.EclTemplateFragment = "TODO"; // TODO
                    eclItem.EclUri = externalContent.Id;
                }

                // TODO: semantic mapping

                return result;
            }
        }

        private static IDictionary<string, string> ResolveMetaLinks(IDictionary<string, string> meta)
        {
            ILinkResolver linkResolver = SiteConfiguration.LinkResolver;

            Dictionary<string, string> result = new Dictionary<string, string>(meta.Count);
            foreach (KeyValuePair<string, string> kvp in meta)
            {
                string resolvedValue = _tcmUriRegEx.Replace(kvp.Value, match => linkResolver.ResolveLink(match.Value, resolveToBinary: true));
                result.Add(kvp.Key, resolvedValue);
            }

            return result;
        }

        private static Common.Models.MvcData CreateMvcData(DataModel.MvcData data, string baseModelTypeName)
        {
            if (data == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(data.ViewName))
            {
                throw new DxaException("No View Name specified in MVC Data.");
            }

            string defaultControllerName;
            string defaultActionName;

            switch (baseModelTypeName)
            {
                case "PageModel":
                    defaultControllerName = "Page";
                    defaultActionName = "Page";
                    break;
                case "RegionModel":
                    defaultControllerName = "Region";
                    defaultActionName = "Region";
                    break;
                case "EntityModel":
                    defaultControllerName = "Entity";
                    defaultActionName = "Entity";
                    break;
                default:
                    throw new DxaException($"Unexpected baseModelTypeName '{baseModelTypeName}'");
            }

            return new Common.Models.MvcData
            {
                ControllerName = data.ControllerName ?? defaultControllerName,
                ControllerAreaName = data.ControllerAreaName ?? SiteConfiguration.GetDefaultModuleName(),
                ActionName = data.ActionName ?? defaultActionName,
                ViewName = data.ViewName,
                AreaName = data.AreaName ?? SiteConfiguration.GetDefaultModuleName(),
                RouteValues = data.Parameters
            };
        }

        private static RegionModel CreateRegionModel(RegionModelData regionModelData, Localization localization)
        {
            RegionModel result = new RegionModel(regionModelData.Name)
            {
                ExtensionData = regionModelData.ExtensionData,
                HtmlClasses = regionModelData.HtmlClasses,
                MvcData = CreateMvcData(regionModelData.MvcData, "RegionModel"),
                XpmMetadata = regionModelData.XpmMetadata
            };

            if (regionModelData.Regions != null)
            {
                IEnumerable<RegionModel> nestedRegionModels = regionModelData.Regions.Select(data => CreateRegionModel(data, localization));
                result.Regions.UnionWith(nestedRegionModels);
            }

            if (regionModelData.Entities != null)
            {
                foreach (EntityModelData entityModelData in regionModelData.Entities)
                {
                    EntityModel entityModel;
                    try
                    {
                        entityModel = ModelBuilderPipeline.CreateEntityModel(entityModelData, typeof(EntityModel), localization);
                    }
                    catch (Exception ex)
                    {
                        // If there is a problem mapping an Entity, we replace it with an ExceptionEntity which holds the error details and carry on.
                        Log.Error(ex);
                        entityModel = new ExceptionEntity(ex);
                    }
                    result.Entities.Add(entityModel);
                }
            }

            return result;
        }
    }
}
