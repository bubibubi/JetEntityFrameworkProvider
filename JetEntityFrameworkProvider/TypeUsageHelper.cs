using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Text;

namespace JetEntityFrameworkProvider
{
    static class TypeUsageHelper
    {
        public static bool GetIsIdentity(this TypeUsage tu, bool defaultValue = false)
        {
            Facet storeGenFacet;
            if (tu.Facets.TryGetValue("StoreGeneratedPattern", false, out storeGenFacet) &&
                storeGenFacet.Value != null)
            {
                StoreGeneratedPattern storeGenPattern = (StoreGeneratedPattern)storeGenFacet.Value;
                return storeGenPattern == StoreGeneratedPattern.Identity;
            }
            return defaultValue;
        }

        public static byte GetPrecision(this TypeUsage tu, byte defaultValue = 18)
        {
            return tu.Facets[DbProviderManifest.PrecisionFacetName] == null ? defaultValue : (byte)tu.Facets[DbProviderManifest.PrecisionFacetName].Value;
        }

        public static byte GetScale(this TypeUsage tu, byte defaultValue = 0)
        {
            return tu.Facets[DbProviderManifest.ScaleFacetName] == null ? defaultValue : (byte)tu.Facets[DbProviderManifest.ScaleFacetName].Value;
        }

        public static int GetMaxLength(this TypeUsage tu, int defaultValue = int.MaxValue)
        {
            return tu.Facets[DbProviderManifest.MaxLengthFacetName].IsUnbounded || tu.Facets[DbProviderManifest.MaxLengthFacetName].Value == null ? defaultValue : (int)tu.Facets[DbProviderManifest.MaxLengthFacetName].Value;
        }

        public static bool GetIsFixedLength(this TypeUsage tu, bool defaultValue = false)
        {
            if (!tu.IsPrimitiveTypeOf(PrimitiveTypeKind.String) && !tu.IsPrimitiveTypeOf(PrimitiveTypeKind.Binary))
                return defaultValue;

            return tu.Facets[DbProviderManifest.FixedLengthFacetName].Value == null ? defaultValue : (bool)tu.Facets[DbProviderManifest.FixedLengthFacetName].Value;
        }

        public static bool IsPrimitiveTypeOf(this TypeUsage tu, PrimitiveTypeKind primitiveType)
        {
            PrimitiveTypeKind typeKind;
            if (TryGetPrimitiveTypeKind(tu, out typeKind))
            {
                return (typeKind == primitiveType);
            }
            return false;
        }

        public static bool TryGetPrimitiveTypeKind(this TypeUsage tu, out PrimitiveTypeKind typeKind)
        {
            if (tu != null && tu.EdmType != null && tu.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType)
            {
                typeKind = ((PrimitiveType)tu.EdmType).PrimitiveTypeKind;
                return true;
            }

            typeKind = default(PrimitiveTypeKind);
            return false;
        }

        public static PrimitiveTypeKind GetPrimitiveTypeKind(this TypeUsage tu)
        {
            PrimitiveTypeKind returnValue;
            if (!TryGetPrimitiveTypeKind(tu, out returnValue))
                throw new ArgumentException("The type is not a primitive type kind", "tu");
            return returnValue;
        }


        public static bool GetIsUnicode(this TypeUsage tu)
        {
            return tu.Facets[DbProviderManifest.UnicodeFacetName].Value == null || (bool)tu.Facets[DbProviderManifest.UnicodeFacetName].Value;
        }

        public static bool GetPreserveSeconds(this TypeUsage tu)
        {
            return tu.Facets["PreserveSeconds"].Value != null && (bool)tu.Facets["PreserveSeconds"].Value;
        }

        public static bool GetIsNullable(this TypeUsage tu)
        {
            return tu.Facets[DbProviderManifest.NullableFacetName].Value != null && (bool)tu.Facets[DbProviderManifest.NullableFacetName].Value;
        }

        public static bool TryGetPrecision(this TypeUsage tu, out byte precision)
        {
            Facet f;

            precision = 0;
            if (tu.Facets.TryGetValue(DbProviderManifest.PrecisionFacetName, false, out f))
            {
                if (!f.IsUnbounded && f.Value != null)
                {
                    precision = (byte)f.Value;
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetMaxLength(this TypeUsage tu, out int maxLength)
        {

            maxLength = 0;

            if (!tu.IsPrimitiveTypeOf(PrimitiveTypeKind.String) && !tu.IsPrimitiveTypeOf(PrimitiveTypeKind.Binary))
                return false;

            Facet f;
            if (tu.Facets.TryGetValue(DbProviderManifest.MaxLengthFacetName, false, out f))
            {
                if (!f.IsUnbounded && f.Value != null)
                {
                    maxLength = (int)f.Value;
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetScale(this TypeUsage tu, out byte scale)
        {
            Facet f;

            scale = 0;
            if (tu.Facets.TryGetValue(DbProviderManifest.ScaleFacetName, false, out f))
            {
                if (!f.IsUnbounded && f.Value != null)
                {
                    scale = (byte)f.Value;
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetIsFixedLength(this TypeUsage tu, out bool isFixedLength)
        {
            isFixedLength = false;

            if (!tu.IsPrimitiveTypeOf(PrimitiveTypeKind.String) &&
                !tu.IsPrimitiveTypeOf(PrimitiveTypeKind.Binary))
            {
                return false;
            }

            Facet f;
            if (!tu.Facets.TryGetValue(DbProviderManifest.FixedLengthFacetName, true, out f))
                return false;
            else
                return f.Value == null ? false : (bool)f.Value;
        }

        /// <summary>
        /// Tries to get the name of a facet starting from an EdmType.
        /// </summary>
        /// <param name="edmType">Type of the edm.</param>
        /// <param name="facetName">Name of the facet.</param>
        /// <param name="facetDescription">The facet description.</param>
        /// <returns>True if the facet was found; otherwise false</returns>
        public static bool TryGetTypeFacetDescriptionByName(this EdmType edmType, string facetName, out FacetDescription facetDescription)
        {
            facetDescription = null;
            if (MetadataHelpers.IsPrimitiveType(edmType))
            {
                PrimitiveType primitiveType = (PrimitiveType)edmType;
                foreach (FacetDescription fd in primitiveType.FacetDescriptions)
                {
                    if (facetName.Equals(fd.FacetName, StringComparison.OrdinalIgnoreCase))
                    {
                        facetDescription = fd;
                        return true;
                    }
                }
            }
            return false;
        }


    }
}
