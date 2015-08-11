using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Sdl.Web.Tridion.ContentManager
{
    /// <summary>
    /// Represents a native Tridion Content Manager URI which uniquely identifies a resource in the system.
    /// </summary>
    /// <remarks>The structure of a TCM URI is <c>PublicationID-ItemID[-ItemType][-vVersion]</c>.</remarks>
    // TODO: this is a copy of class TcmUri from Tridion.Common (stripped to remove dependencies). We should just use Tridion.Common.
    [Serializable]
    public sealed class TcmUri : ICloneable
    {
        /// <summary>
        ///  RegEx with a pattern of a <see cref="TcmUri"/>, which has a structure of 
        ///  <c>{PublicationID-ItemID}[-ItemType][-vVersion]</c>
        /// </summary>
        /// <remarks>
        /// Explanation of the optional <see cref="ItemType"/> group: (-(?&lt;itemType&gt;[0-9]+))?
        /// <list type="bullet">
        ///   <item>- : start with a '-'</item>
        ///   <item>?&lt;nameOfTheGroup&gt; : give the group defined by '()' a name, which can be used to retrieve it.</item>
        ///   <item>[0-9] : any decimal digit</item>
        ///   <item>+  : there must be one or more of the preceding item (the decimal digit)</item>
        ///   <item>?  : there must be zero or more of the preceding item (here the group defined by '()'</item>
        /// </list>
        /// </remarks>
        private static readonly Regex _tcmUriRegEx =
            new Regex("^tcm:(?<pubId>[0-9]+)-(?<itemId>[0-9]+)(-(?<itemType>[0-9]+))?(-v(?<version>[0-9]+))?$", RegexOptions.Compiled);

        private static readonly TcmUri _uriNull = new TcmUri();
        private const int _editableVersion = 0;
        internal const ItemType SystemRepositoryItemType = (ItemType)7;
        private const int _idNull = -1;
        private const string _tcmPrefix = "tcm";
        private const string _versionSeparator = "v";
        
        /// <summary>
        /// A <see cref="TcmUri"/> identifying the system Repository.
        /// </summary>
        public static readonly TcmUri SystemRepositoryUri = new TcmUri(0, SystemRepositoryItemType);
        
        private uint _publicationId;
        private uint _itemId;
        private ItemType _itemType = ItemType.None;
        private uint? _version;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the TcmUri class representing tcm:0-0-0.
        /// </summary>
        /// <remarks>
        /// This is a private constructor. Use the static <see cref="TcmUri.UriNull"/> property instead.
        /// </remarks>
        private TcmUri()
        {
        }

        /// <summary>
        /// Initializes a new instance of the TcmUri class with the specified itemID and itemType.
        /// </summary>
        /// <param name="itemId">The unique identifier for the item.</param>
        /// <param name="itemType">The type of item.</param>
        /// <exception cref="ArgumentOutOfRangeException"><c>itemId</c> is negative. This behavior has changed in version 3.0.</exception>
        public TcmUri(int itemId, ItemType itemType)
        {
            if (itemId < 0)
            {
                throw new ArgumentOutOfRangeException("itemId");
            }

            _itemId = (uint) itemId;
            _itemType = itemType;
        }

        /// <summary>
        /// Initializes a new instance of the TcmUri class with the specified <paramref name="itemId"/>, <paramref name="itemType"/> and <paramref name="publicationId"/>.
        /// </summary>
        /// <param name="itemId">The unique identifier for the item.</param>
        /// <param name="itemType">The type of item.</param>
        /// <param name="publicationId">The unique identifier for the publication.</param>
        /// <exception cref="ArgumentOutOfRangeException"><c>itemId</c> is negative, or <c>publicationId</c> is negative and not -1. This behavior has changed in version 3.0.</exception>
        public TcmUri(int itemId, ItemType itemType, int publicationId)
        {
            if (itemId < 0)
            {
                throw new ArgumentOutOfRangeException("itemId");
            }

            if (publicationId < 0 && publicationId != _idNull)
            {
                throw new ArgumentOutOfRangeException("publicationId");
            }

            _itemId = (uint) itemId;
            _itemType = itemType;
            _publicationId = publicationId == _idNull ? 0 : (uint)publicationId;
        }

        /// <summary>
        /// Initializes a new instance of the TcmUri class with the specified <paramref name="itemId"/>, <paramref name="itemType"/>,
        /// <paramref name="publicationId"/> and <paramref name="version"/>.
        /// </summary>
        /// <param name="itemId">The unique identifier for the item.</param>
        /// <param name="itemType">The type of item.</param>
        /// <param name="publicationId">The unique identifier for the publication.</param>
        /// <param name="version">The version number.</param>
        /// <exception cref="ArgumentOutOfRangeException"><c>itemId</c> is negative, or <c>publicationId</c> is negative and not -1. This behavior has changed in version 3.0.</exception>
        public TcmUri(int itemId, ItemType itemType, int publicationId, int version)
        {
            if (itemId < 0)
            {
                throw new ArgumentOutOfRangeException("itemId");
            }

            if (publicationId < 0 && publicationId != _idNull)
            {
                throw new ArgumentOutOfRangeException("publicationId");
            }

            _itemId = (uint) itemId;
            _itemType = itemType;
            _publicationId = publicationId == _idNull ? 0 : (uint)publicationId;
            _version = version < 0 ? null : (uint?)version;
        }

        /// <summary>
        /// Initializes a new instance of the TcmUri class with the itemID, itemType, publicationID, and version extracted from the URI.
        /// </summary>
        /// <param name="uri">The URI containing the itemID, itemType, publicationID, and version.</param>
        /// <exception cref="InvalidTcmUriException">
        ///     Thrown if <paramref name="uri"/> is an invalid <see cref="String"/> representation of <see cref="TcmUri"/>. This behavior has changed in version 3.0.
        /// </exception>
        public TcmUri(string uri)
        {
            Parse(uri);
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Gets a <see cref="TcmUri"/> instance that represents a URI <see langword="null"/> (tcm:0-0-0).
        /// </summary>
        /// <value>
        /// A <see cref="TcmUri"/> instance that represents a URI <see langword="null"/> (tcm:0-0-0).
        /// </value>
        public static TcmUri UriNull
        {
            get
            {
                return _uriNull;
            }
        }

        /// <summary>
        /// Gets the Item ID of the item represented by this instance of <see cref="TcmUri"/>.
        /// </summary>
        /// <value>
        /// Either a non-negative ID or <see cref="_idNull"/> when item ID is not specified.
        /// </value>
        public int ItemId
        {
            get
            {
                if (_itemType == ItemType.ApprovalStatus || _itemId != 0)
                {
                    return (int) _itemId;
                }
                else
                {
                    return _idNull;
                }
            }
        }

        /// <summary>
        /// Gets the Item Type of the item represented by this instance of <see cref="TcmUri"/>.
        /// </summary>
        public ItemType ItemType
        {
            get
            {
                return _itemType;
            }
        }

        /// <summary>
        /// Gets the Publication ID of the item represented by this instance of <see cref="TcmUri"/>.
        /// </summary>
        /// <value>
        /// Either a non-negative publication ID or <see cref="_idNull"/> when publication ID is not specified.
        /// </value>
        public int PublicationId
        {
            get
            {
                return _publicationId == 0 ? _idNull : (int)_publicationId;
            }
        }

        /// <summary>
        /// Gets the version of the item represented by this instance of <see cref="TcmUri"/>.
        /// </summary>
        /// <value>
        /// Either a non-negative version number or <see cref="_idNull"/> when version is not specified.
        /// </value>
        public int Version
        {
            get
            {
                if (_version != null)
                {
                    return (int) _version;
                }
                else
                {
                    return _idNull;
                }
            }
        }

        /// <summary>
        /// Gets the id of the context Repository
        /// </summary>
        /// <value>
        /// The identifier of the context Repository.
        /// 0 if the context Repository Id is <see cref="_idNull"/>.
        /// </value>
        public int ContextRepositoryId
        {
            get
            {
                return GetContextRepositoryId(true);
            }
        }

        /// <summary>
        /// Gets a value indicating whether it is the editable version.
        /// </summary>
        public bool IsEditableVersion
        {
            get
            {
                return (_itemId == 0 && _itemType == 0 && _publicationId == 0) || _version == _editableVersion;
            }
        }

        /// <summary>
        /// Gets a value indicating whether it identifies a System Wide object.
        /// </summary>
        public bool IsSystemWide
        {
            get
            {
                bool isSytemWide;

                if (((int)_itemType & 0x30000) == 0x10000)
                {
                    isSytemWide = true;
                }
                else
                {
                    isSytemWide = _itemType == ItemType.ApprovalStatus || _itemType == ItemType.Publication;
                }

                return isSytemWide;
            }
        }

        /// <summary>
        /// Gets a value indicating whether it identifies a Repository local object.
        /// </summary>
        public bool IsRepositoryLocal
        {
            get
            {
                bool isRepositoryLocal;

                if (((int)_itemType & 0x30000) == 0x00000)
                {
                    isRepositoryLocal = _itemType != ItemType.Publication;
                }
                else
                {
                    isRepositoryLocal = _itemType == ItemType.ProcessDefinition;
                }

                return isRepositoryLocal;
            }
        }

        /// <summary>
        /// Gets a value indicating whether it contains a version.
        /// </summary>
        /// <returns><c>true</c> if it contains a version otherwise <c>false</c>.</returns>
        public bool IsVersionless
        {
            get
            {
                return !_version.HasValue;
            }
        }

        /// <summary>
        /// Gets a value indicating whether it is the System Repository.
        /// </summary>
        /// <returns><c>true</c> if it is a System Repository otherwise <c>false</c>.</returns>
        public bool IsSystemRepository
        {
            get
            {
                return _itemId == 0 && _publicationId == 0 && ((_itemType & SystemRepositoryUri.ItemType) == _itemType);
            }
        }

        /// <summary>
        /// Gets a value indicating whether it is <see cref="TcmUri.UriNull"/>.
        /// </summary>
        /// <returns><c>true</c> if the <see cref="TcmUri"/> is <see cref="TcmUri.UriNull"/>, otherwise <c>false</c>.</returns>
        public bool IsUriNull
        {
            get
            {
                return _itemId == 0 && _itemType == 0 && _publicationId == 0;
            }
        }

        /// <summary>
        /// Gets a value indicating the Repository.
        /// </summary>
        /// <param name="expectSystemRepository">In case the PublicationID is null then return the expectedSystemRepository or null.</param>
        /// <returns>
        /// The identifier of the context Repository.
        /// 0 if the context Repository Id is <see cref="_idNull"/> and expecting the System Repository.
        /// <see cref="_idNull"/> if the context Repository is <see cref="_idNull"/> and not expecting the system Repository.
        /// </returns>
        public int GetContextRepositoryId(bool expectSystemRepository)
        {
            if (_itemType == ItemType.Publication)
            {
                return (int)_itemId;
            }
            else
            {
                if (_publicationId == 0)
                {
                    return expectSystemRepository ? 0 : _idNull;
                }
                else
                {
                    return (int)_publicationId;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating the Repository.
        /// </summary>
        /// <returns>A <see cref="TcmUri"/> containing the Repository.</returns>
        public TcmUri GetContextRepositoryUri()
        {
            if (_itemType == ItemType.Publication)
            {
                return this;
            }
            else
            {
                return _publicationId == 0 ? SystemRepositoryUri : new TcmUri((int)_publicationId, ItemType.Publication);
            }
        }

        /// <summary>
        /// Gets a value indicating a <see cref="TcmUri"/> without a version.
        /// </summary>
        /// <returns>A <see cref="TcmUri"/> without a version</returns>
        public TcmUri GetVersionlessUri()
        {
            return new TcmUri((int)_itemId, _itemType, (int)_publicationId);
        }

        /// <summary>
        /// Gets a value indicating whether it is <c>null</c> or <see cref="TcmUri.UriNull"/>.
        /// </summary>
        /// <param name="id">The <see cref="TcmUri"/> to check.</param>
        /// <returns><c>true</c> if the <see cref="TcmUri"/> is <c>null</c> or <see cref="TcmUri.UriNull"/> otherwise <c>false</c>.</returns>
        public static bool IsNullOrUriNull(TcmUri id)
        {
            return id == null || id.IsUriNull;
        }

        /// <summary>
        /// Returns whether the given value is valid for this type.
        /// </summary>
        /// <param name="uri">The <see cref="String"/> to test for validity.</param>
        /// <returns><see langword="true"/> if the specified value is valid for this object; otherwise, <see langword="false"/>.</returns>
        /// <remarks>This property is <see langword="true"/> if the string that was passed into the method 
        /// can be parsed as a <see cref="TcmUri"/> instance, which has a structure of 
        /// <c>{PublicationID-ItemID}[-ItemType][-vVersion]</c>. Otherwise, the property is <see langword="false"/>.
        /// </remarks>
        public static bool IsValid(string uri)
        {
            return uri != null && _tcmUriRegEx.IsMatch(uri);
        }

        #endregion
        
        #region Overrides

        /// <summary>
        /// Returns a string that represents the current <see cref="TcmUri"/>.
        /// </summary>
        /// <returns>A string that represents the current <see cref="TcmUri"/></returns>
        public override string ToString()
        {
            StringBuilder formattedUri = new StringBuilder();

            formattedUri.Append(_tcmPrefix);
            formattedUri.Append(":");
            formattedUri.Append(_publicationId);
            formattedUri.Append("-");
            formattedUri.Append(_itemId);

            if (_itemType != ItemType.Component)
            {
                formattedUri.Append("-");
                formattedUri.Append((int) _itemType);
            }

            if (_version != null)
            {
                formattedUri.Append("-");
                formattedUri.Append(_versionSeparator);
                formattedUri.Append(_version);
            }

            return formattedUri.ToString();
        }

        /// <summary>
        /// Serves as a hash function for a particular type, suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>A hash code for the current <see cref="TcmUri"/>.</returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="TcmUri"/> is equal to the current <see cref="TcmUri"/>.  
        /// </summary>
        /// <param name="obj">The <see cref="TcmUri"/> to compare with the current <see cref="TcmUri"/>.</param>
        /// <returns><see langword="true"/> if the specified <see cref="TcmUri"/> is equal to the current <see cref="TcmUri"/>; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is TcmUri)
            {
                TcmUri anotherUri = (TcmUri) obj;

                return (this == anotherUri);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="TcmUri"/> instances are considered equal.
        /// </summary>
        /// <param name="objA">The first <see cref="TcmUri"/> to compare.</param>
        /// <param name="objB">>The second <see cref="TcmUri"/> to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="objA"/> is the same instance as <paramref name="objB"/> or if both are <see langword="null"/> 
        /// references or if <c>objA.Equals(objB)</c> returns <see langword="true"/>; otherwise, <see langword="false"/>. </returns>
        public static new bool Equals(object objA, object objB)
        {
            if (objA != null)
            {
                return objA.Equals(objB);
            }
            else
            {
                return (objB == null);
            }
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        #region Overloaded Operators

        /// <summary>
        /// Supports (implicit) cast to string.
        /// </summary>
        /// <param name="source">The <see cref="TcmUri"/> object to cast to a string.</param>
        /// <returns>
        /// A string representation of the TCM URI. See <see cref="ToString()"/>.
        /// Returns <see langword="null"/> if <paramref name="source"/> is <see langword="null"/>.</returns>
        /// <remarks>
        /// <example><code>
        /// TcmUri myTcmUri = new TcmUri("tcm:1-2")
        /// string uri = myTcmUri;
        /// </code></example>
        /// </remarks>
        public static implicit operator string(TcmUri source)
        {
            if (source != null)
            {
                return source.ToString();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="TcmUri"/> instances are considered equal (equality operator overload).
        /// </summary>
        /// <param name="objA">The first instance to compare.</param>
        /// <param name="objB">The second instance to compare.</param>
        /// <returns><see langword="true"/> if both instances represent the same TCM URI.</returns>
        public static bool operator ==(TcmUri objA, TcmUri objB)
        {
            if (IsNull(objA) || IsNull(objB))
            {
                // If either is null, both must be null
                return (IsNull(objA) && IsNull(objB));
            }
            return (objA.ItemType == objB.ItemType && objA.ItemId == objB.ItemId && objA.PublicationId == objB.PublicationId &&
                objA.Version == objB.Version);
        }

        /// <summary>
        /// Determines whether the specified <see cref="TcmUri"/> instances are considered different (inequality operator overload).
        /// </summary>
        /// <param name="objA">The first instance to compare.</param>
        /// <param name="objB">The second instance to compare.</param>
        /// <returns><see langword="false"/> if both instances represent the same TCM URI.</returns>
        public static bool operator !=(TcmUri objA, TcmUri objB)
        {
            if (IsNull(objA) || IsNull(objB))
            {
                // If either is null, other cannot be null
                return (!IsNull(objA) || !IsNull(objB));
            }
            return (objA.ItemType != objB.ItemType || objA.ItemId != objB.ItemId || objA.PublicationId != objB.PublicationId ||
                objA.Version != objB.Version);
        }

        /// <summary>
        /// Determines whether the specified <see cref="TcmUri"/> instance and string are considered equal (equality operator overload).
        /// </summary>
        /// <param name="objA">The <see cref="TcmUri"/> instance to compare.</param>
        /// <param name="objB">The string to compare against.</param>
        /// <returns><see langword="true"/> if the <see cref="TcmUri"/> instance represent the same TCM URI as the string.</returns>
        public static bool operator ==(TcmUri objA, string objB)
        {
            if (IsNull(objA) || IsNull(objB))
            {
                // If either is null, both must be null
                return (IsNull(objA) && IsNull(objB));
            }
            return (objA == new TcmUri(objB));
        }

        /// <summary>
        /// Determines whether the specified <see cref="TcmUri"/> instance and string are considered different (inequality operator overload).
        /// </summary>
        /// <param name="objA">The <see cref="TcmUri"/> instance to compare.</param>
        /// <param name="objB">The string to compare against.</param>
        /// <returns><see langword="false"/> if the <see cref="TcmUri"/> instance represent the same TCM URI as the string.</returns>
        public static bool operator !=(TcmUri objA, string objB)
        {
            if (IsNull(objA) || IsNull(objB))
            {
                // If either is null, other cannot be null
                return (!IsNull(objA) || !IsNull(objB));
            }
            return (objA != new TcmUri(objB));
        }

        #endregion

        #region Private members

        /// <summary>
        /// Helper method to check whether an object is <see langword="null"/>, without the influence
        /// of operator overloading.
        /// </summary>
        /// <param name="obj">The object to check for being <see langword="null"/></param>
        /// <returns><c>obj==null</c></returns>
        private static bool IsNull(object obj)
        {
            return obj == null;
        }

        /// <summary>
        /// Converts a string that represents a TCM URI into an actual <see cref="TcmUri"/> type.
        /// </summary>
        /// <param name="uri">The string representation of a <see cref="TcmUri"/>.</param>
        /// <exception cref="InvalidTcmUriException">
        ///     Thrown if <paramref name="uri"/> is an invalid <see cref="String"/> representation of <see cref="TcmUri"/>.
        /// </exception>
        private void Parse(string uri)
        {
            try
            {
                Match match = _tcmUriRegEx.Match(uri);
                if (!match.Success)
                {
                    throw new InvalidTcmUriException(uri);
                }

                // Getting the values from the RexEx groups, if they exist.
                _publicationId = (uint)int.Parse(match.Groups["pubId"].Value);

                _itemId = (uint)int.Parse(match.Groups["itemId"].Value);

                if (match.Groups["itemType"].Captures.Count > 0)
                {
                    _itemType = (ItemType)int.Parse(match.Groups["itemType"].Value);
                }
                else
                {
                    // When item type is not specified, this implies ItemType.Component.
                    _itemType = ItemType.Component;
                }

                if (match.Groups["version"].Captures.Count > 0)
                {
                    _version = (uint)int.Parse(match.Groups["version"].Value);
                }
            }
            catch (Exception ex)
            {
                // NOTE: integer parse errors will be wrapped here
                throw new InvalidTcmUriException(uri, ex);
            }
        }

        #endregion
    }
}
