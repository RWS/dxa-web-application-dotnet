using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Sdl.Web.Delivery.Core;

namespace Sdl.Web.Tridion.ContentManager
{
    /// <summary>
    /// Handles CmUris (that can be of any namespace such as tcm or ish)
    /// </summary>
    [Serializable]
    public class CmUri
    {
        private const string Regex = "^(?<namespace>[a-zA-z]+):(?<pubId>[0-9]+)-(?<itemId>[0-9]+)(-(?<itemType>[0-9]+))?(-v(?<version>[0-9]+))?$";
        private static readonly Regex UriRegEx = new Regex(Regex, RegexOptions.Compiled);

        public CmUri(string uriNamespace, int publicationId, int itemId, ItemType itemType, int version)
        {
            Namespace = uriNamespace;
            PublicationId = publicationId;
            ItemId = itemId;
            ItemType = itemType;
            Version = version;
        }

        public CmUri(string uriNamespace, int publicationId, int itemId, ItemType itemType)
        {
            Namespace = uriNamespace;
            PublicationId = publicationId;
            ItemId = itemId;
            ItemType = itemType;
            Version = -1;
        }

        public CmUri(string uri)
        {
            Namespace = null;
            ItemType = ItemType.None;
            ItemId = -1;
            PublicationId = -1;
            Version = -1;
            Parse(uri);
        }

        public CmUri(CmUri uri)
            : this(uri.Namespace, uri.PublicationId, uri.ItemId, uri.ItemType, uri.Version)
        { }

        public static CmUri Create(string namespaceId, int publicationId, int itemId, ItemType itemType, int version) 
            => new CmUri(namespaceId, publicationId, itemId, itemType, version);

        public static CmUri Create(string namespaceId, int publicationId, int itemId, ItemType itemType)
            => new CmUri(namespaceId, publicationId, itemId, itemType);

        public string Namespace { get; set; }
        public int ItemId { get; set; }
        public ItemType ItemType { get; set; }
        public int PublicationId { get; set; }
        public int Version { get; set; }

        [JsonIgnore]
        public bool IsNullUri => ItemId == 0 && ItemType == 0 && PublicationId == 0;

        public static bool TryParse(string uri, out CmUri cmUri)
        {
            cmUri = null;
            if (string.IsNullOrEmpty(uri)) return false;
            string ns;
            int version = -1;
            try
            {
                Match match = UriRegEx.Match(uri);
                if (!match.Success)
                {
                    return false;
                }
                ns = match.Groups["namespace"].Value;
                var publicationId = int.Parse(match.Groups["pubId"].Value);
                var itemId = int.Parse(match.Groups["itemId"].Value);
                ItemType itemType;
                if (match.Groups["itemType"].Captures.Count > 0)
                {
                    itemType = (ItemType)Enum.Parse(typeof(ItemType), match.Groups["itemType"].Value);
                }
                else
                {
                    itemType = ItemType.Component;
                }
                if (match.Groups["version"].Captures.Count > 0)
                {
                    version = int.Parse(match.Groups["version"].Value);
                }

                cmUri = new CmUri(ns, publicationId, itemId, itemType, version);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void Parse(string uri)
        {
            try
            {
                Match match = UriRegEx.Match(uri);
                if (!match.Success)
                {
                    throw new FormatException("Invalid CmUri: " + uri);
                }
                Namespace = match.Groups["namespace"].Value;
                PublicationId = int.Parse(match.Groups["pubId"].Value);
                ItemId = int.Parse(match.Groups["itemId"].Value);
                if (match.Groups["itemType"].Captures.Count > 0)
                {
                    ItemType = (ItemType)Enum.Parse(typeof(ItemType), match.Groups["itemType"].Value);
                }
                else
                {
                    ItemType = ItemType.Component;
                }
                if (match.Groups["version"].Captures.Count > 0)
                {
                    Version = int.Parse(match.Groups["version"].Value);
                }
            }
            catch (Exception exception)
            {
                throw new FormatException(exception.ToString());
            }
        }

        #region Helping Operators and Stuff

        public static CmUri FromString(string uriString)
        {
            CmUri uri = null;
            if (string.IsNullOrEmpty(uriString))
            {
                Logger.Warn("Trying to create CmUri from empty string.");
            }
            else
            {
                try
                {
                    uri = new CmUri(uriString);
                }
                catch (FormatException e)
                {
                    Logger.Error($"CmUri '{uriString}' cannot be parsed.", e);
                }
            }
            return uri;
        }

        public override string ToString() => $"{Namespace}:{PublicationId}-{ItemId}-{(int)ItemType}";

        public override int GetHashCode() => ToString().GetHashCode();

        protected static bool IsNull(object o) => o == null;

        public override bool Equals(object obj) => this == (obj as CmUri);

        public new static bool Equals(object objA, object objB) => !IsNull(objA) ? objA.Equals(objB) : IsNull(objB);

        public static bool operator ==(CmUri objA, string objB)
        {
            if (IsNull(objA) && IsNull(objB))
                return true;

            if (IsNull(objA)) return false;
            return objB != null && objA == new CmUri(objB);
        }

        public static bool operator ==(CmUri objA, CmUri objB)
        {
            if (IsNull(objA) || IsNull(objB))
            {
                return IsNull(objA) && IsNull(objB);
            }

            return objA.ItemType == objB.ItemType && objA.ItemId == objB.ItemId &&
                     objA.PublicationId == objB.PublicationId && objA.Version == objB.Version && objA.Namespace == objB.Namespace;
        }

        public static implicit operator string(CmUri uri) => uri?.ToString();

        public static bool operator !=(CmUri objA, string objB)
        {
            if (!IsNull(objA) && !IsNull(objB))
            {
                return (objA != new CmUri(objB));
            }
            return !IsNull(objA) || !IsNull(objB);
        }

        public static bool operator !=(CmUri objA, CmUri objB)
        {
            if (IsNull(objA) || IsNull(objB))
            {
                if (IsNull(objA))
                {
                    return !IsNull(objB);
                }
                return true;
            }
            if (((objA.ItemType == objB.ItemType) && (objA.ItemId == objB.ItemId)) &&
                (objA.PublicationId == objB.PublicationId) && (objA.Namespace == objB.Namespace))
            {
                return (objA.Version != objB.Version);
            }
            return true;
        }

        public object Clone() => MemberwiseClone();

        #endregion 
    }
}
