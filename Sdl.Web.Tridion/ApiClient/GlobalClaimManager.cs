using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sdl.Tridion.Api.Client.ContentModel;
using Sdl.Web.Common.Utils;

namespace Sdl.Web.Tridion.ApiClient
{
    /// <summary>
    /// Handles adding and automatically removing global claims. This class is used so you can
    /// execute client Api methods with claims and once you finish the global claims can be 
    /// automatically removed allowing usage of the same client without the global claims being
    /// forwarded
    /// 
    /// <example>
    ///     var client = ApiClientFactory.Instance.CreateClient()
    ///     using(var mgr = GlobalClaimManager.Create)
    ///     {
    ///         mgr.AddClaim(myClaim)
    ///         client.Execute(...)
    ///     }
    ///     // client.Execute will not pass myClaim
    /// </example>
    /// </summary>
    public sealed class GlobalClaimManager : IDisposable
    {
        private readonly List<ClaimValue> _claims;

        private GlobalClaimManager()
        {
            _claims = new List<ClaimValue>();
        }

        public static GlobalClaimManager Create => new GlobalClaimManager();

        public static ClaimValue CreateClaim(Uri uri, object value) => new ClaimValue
        {
            Uri = uri.ToString(),
            Type = ClaimValueType.STRING,
            Value = JsonConvert.SerializeObject(value),
        };

        public void AddClaim(ClaimValue claim)
        {
            _claims.Add(claim);
            ApiClientFactory.Instance.AddGlobalClaim(claim);
        }

        public void AddClaim(Uri uri, object value)
        {
            var claim = CreateClaim(uri, value);
            _claims.Add(claim);
            ApiClientFactory.Instance.AddGlobalClaim(claim);
        }

        public override int GetHashCode() => _claims.Aggregate(0, (current, claim) => Hash.CombineHashCodes(current, claim.Uri.GetHashCode(), claim.Type.GetHashCode(), claim.Value.GetHashCode()));

        public void Dispose()
        {
            foreach (var claim in _claims)
            {
                ApiClientFactory.Instance.RemoveGlobalClaim(claim);
            }
            _claims.Clear();
        }
    }
}
