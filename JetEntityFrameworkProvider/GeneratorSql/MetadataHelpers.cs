using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data.Entity.Core.Metadata.Edm;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// A set of static helpers for type metadata
    /// </summary>
    static class MetadataHelpers
    {
        #region Type Helpers

        /// <summary>
        /// Cast the EdmType of the given type usage to the given TEdmType
        /// </summary>
        /// <typeparam name="TEdmType"></typeparam>
        /// <param name="typeUsage"></param>
        /// <returns></returns>
        internal static TEdmType GetEdmType<TEdmType>(TypeUsage typeUsage)
            where TEdmType : EdmType
        {
            return (TEdmType)typeUsage.EdmType;
        }

        /// <summary>
        /// Gets the TypeUsage of the elment if the given type is a collection type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static TypeUsage GetElementTypeUsage(TypeUsage type)
        {
            if (MetadataHelpers.IsCollectionType(type))
            {
                return ((CollectionType)type.EdmType).TypeUsage;
            }
            return null;
        }

        /// <summary>
        /// Retrieves the properties of in the EdmType underlying the input type usage, 
        ///  if that EdmType is a structured type (EntityType, RowType). 
        /// </summary>
        /// <param name="typeUsage"></param>
        /// <returns></returns>
        internal static IList<EdmProperty> GetProperties(TypeUsage typeUsage)
        {
            return MetadataHelpers.GetProperties(typeUsage.EdmType);
        }

        /// <summary>
        /// Retrieves the properties of the given EdmType, if it is
        ///  a structured type (EntityType, RowType). 
        /// </summary>
        /// <param name="edmType"></param>
        /// <returns></returns>
        internal static IList<EdmProperty> GetProperties(EdmType edmType)
        {
            switch (edmType.BuiltInTypeKind)
            {
                case BuiltInTypeKind.ComplexType:
                    return ((ComplexType)edmType).Properties;
                case BuiltInTypeKind.EntityType:
                    return ((EntityType)edmType).Properties;
                case BuiltInTypeKind.RowType:
                    return ((RowType)edmType).Properties;
                default:
                    return new List<EdmProperty>();
            }
        }

        /// <summary>
        /// Is the given type usage over a collection type
        /// </summary>
        /// <param name="typeUsage"></param>
        /// <returns></returns>
        internal static bool IsCollectionType(TypeUsage typeUsage)
        {
            return MetadataHelpers.IsCollectionType(typeUsage.EdmType);
        }

        /// <summary>
        /// Is the given type a collection type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsCollectionType(EdmType type)
        {
            return (BuiltInTypeKind.CollectionType == type.BuiltInTypeKind);
        }

        /// <summary>
        /// Is the given type usage over a primitive type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsPrimitiveType(TypeUsage type)
        {
            return MetadataHelpers.IsPrimitiveType(type.EdmType);
        }

        /// <summary>
        /// Is the given type a primitive type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsPrimitiveType(EdmType type)
        {
            return (BuiltInTypeKind.PrimitiveType == type.BuiltInTypeKind);
        }

        /// <summary>
        /// Is the given type usage over a row type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsRowType(TypeUsage type)
        {
            return MetadataHelpers.IsRowType(type.EdmType);
        }

        /// <summary>
        /// Is the given type usage over an entity type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsEntityType(TypeUsage type)
        {
            return MetadataHelpers.IsEntityType(type.EdmType);
        }

        /// <summary>
        /// Is the given type a row type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsRowType(EdmType type)
        {
            return (BuiltInTypeKind.RowType == type.BuiltInTypeKind);
        }

        /// <summary>
        /// Is the given type an Enity Type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsEntityType(EdmType type)
        {
            return (BuiltInTypeKind.EntityType == type.BuiltInTypeKind);
        }


        /// <summary>
        /// Gets the value for the metadata property with the given name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        internal static T TryGetValueForMetadataProperty<T>(MetadataItem item, string propertyName)
        {
            MetadataProperty property;
             if (!item.MetadataProperties.TryGetValue(propertyName, true, out property))
             {
                 return default(T);
             }

             return (T)property.Value;
        }

        internal static DbType GetDbType(PrimitiveTypeKind primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveTypeKind.Binary: return DbType.Binary;
                case PrimitiveTypeKind.Boolean: return DbType.Boolean;
                case PrimitiveTypeKind.Byte: return DbType.Byte;
                case PrimitiveTypeKind.DateTime: return DbType.DateTime;
                case PrimitiveTypeKind.Decimal: return DbType.Decimal;
                case PrimitiveTypeKind.Double: return DbType.Double;
                case PrimitiveTypeKind.Single: return DbType.Single;
                case PrimitiveTypeKind.Guid: return DbType.Guid;
                case PrimitiveTypeKind.Int16: return DbType.Int16;
                case PrimitiveTypeKind.Int32: return DbType.Int32;
                case PrimitiveTypeKind.Int64: return DbType.Int64;
                //case PrimitiveTypeKind.Money: return DbType.Decimal;
                case PrimitiveTypeKind.SByte: return DbType.SByte;
                case PrimitiveTypeKind.String: return DbType.String;
                //case PrimitiveTypeKind.UInt16: return DbType.UInt16;
                //case PrimitiveTypeKind.UInt32: return DbType.UInt32;
                //case PrimitiveTypeKind.UInt64: return DbType.UInt64;
                //case PrimitiveTypeKind.Xml: return DbType.Xml;
                default:
                    throw new InvalidOperationException(string.Format("Unknown PrimitiveTypeKind {0}", primitiveType));
            }
        }

        #endregion


        internal static bool IsCanonicalFunction(EdmFunction function)
        {
            return (function.NamespaceName == "Edm");
        }

        internal static bool IsStoreFunction(EdmFunction function)
        {
            return !IsCanonicalFunction(function);
        }

        // Returns ParameterDirection corresponding to given ParameterMode
        internal static ParameterDirection ParameterModeToParameterDirection(ParameterMode mode)
        {
            switch (mode)
            {
                case ParameterMode.In:
                    return ParameterDirection.Input;

                case ParameterMode.InOut:
                    return ParameterDirection.InputOutput;

                case ParameterMode.Out:
                    return ParameterDirection.Output;

                case ParameterMode.ReturnValue:
                    return ParameterDirection.ReturnValue;

                default:
                    throw new ArgumentException("Unrecognized parameter mode", "mode");
            }
        }
    }
}
