using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sdl.Web.Common;
using Tridion.ContentDelivery.Meta;

namespace Sdl.Web.Tridion.TridionDocs.Providers
{
    /// <summary>
    /// Condition Provider
    /// </summary>
    public class ConditionProvider
    {
        private static readonly string ConditionUsed = "conditionsused.generated.value";
        private static readonly string ConditionMetadata = "conditionmetadata.generated.value";
        private static readonly string ConditionValues = "values";

        private class Condition
        {
            [JsonProperty("datatype")]
            public string Datatype { get; set; }
            [JsonProperty("range")]
            public bool Range { get; set; }
            [JsonProperty("values")]
            public string[] Values { get; set; }
        }

        public string GetConditions(int publicationId)
        {
            var conditionUsed = GetMetadata(publicationId, ConditionUsed);
            var conditionMetadata = GetMetadata(publicationId, ConditionMetadata);
            Dictionary<string, string[]> d1 =
                JsonConvert.DeserializeObject<Dictionary<string, string[]>>(conditionUsed);
            Dictionary<string, Condition> d2 =
                JsonConvert.DeserializeObject<Dictionary<string, Condition>>(conditionMetadata);
            foreach (var v in d1)
            {
                d2[v.Key].Values = v.Value;
            }
            return JsonConvert.SerializeObject(d2);
        }

        private string GetMetadata(int publicationId, string metadataName)
        {
            try
            {
                PublicationMetaFactory factory = new PublicationMetaFactory();
                PublicationMeta meta = factory.GetMeta(publicationId);
                if (meta?.CustomMeta == null)
                {
                    throw new DxaItemNotFoundException(
                        $"Metadata '{metadataName}' is not found for publication {publicationId}.");
                }

                object metadata = meta.CustomMeta.GetFirstValue(metadataName);
                string metadataString = metadata != null ? (string)metadata : "{}";
                return metadataString;
            }
            catch (Exception)
            {
                throw new DxaItemNotFoundException(
                    $"Metadata '{metadataName}' is not found for publication {publicationId}.");
            }
        }
    }
}
