﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using Microsoft.OData;
using Microsoft.OData.Edm;

namespace System.Web.OData.Formatter.Serialization
{
    /// <summary>
    /// The default <see cref="ODataSerializerProvider"/>.
    /// </summary>
    public class DefaultODataSerializerProvider : ODataSerializerProvider
    {
        private static readonly ODataServiceDocumentSerializer _workspaceSerializer = new ODataServiceDocumentSerializer();
        private static readonly ODataEntityReferenceLinkSerializer _entityReferenceLinkSerializer = new ODataEntityReferenceLinkSerializer();
        private static readonly ODataEntityReferenceLinksSerializer _entityReferenceLinksSerializer = new ODataEntityReferenceLinksSerializer();
        private static readonly ODataErrorSerializer _errorSerializer = new ODataErrorSerializer();
        private static readonly ODataMetadataSerializer _metadataSerializer = new ODataMetadataSerializer();
        private static readonly ODataRawValueSerializer _rawValueSerializer = new ODataRawValueSerializer();
        private static readonly ODataPrimitiveSerializer _primitiveSerializer = new ODataPrimitiveSerializer();

        private static readonly DefaultODataSerializerProvider _instance = new DefaultODataSerializerProvider();

        private readonly ODataDeltaFeedSerializer _deltaFeedSerializer;
        private readonly ODataCollectionSerializer _collectionSerializer;

        private readonly ODataResourceSetSerializer _resourceSetSerializer;
        private readonly ODataResourceSerializer _resourceSerializer;

        private readonly ODataEnumSerializer _enumSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultODataSerializerProvider"/> class.
        /// </summary>
        public DefaultODataSerializerProvider()
        {
            _deltaFeedSerializer = new ODataDeltaFeedSerializer(this);
            _collectionSerializer = new ODataCollectionSerializer(this);
            _resourceSetSerializer = new ODataResourceSetSerializer(this);
            _resourceSerializer = new ODataResourceSerializer(this);
            _enumSerializer = new ODataEnumSerializer(this);
        }

        /// <summary>
        /// Gets the default instance of the <see cref="DefaultODataSerializerProvider"/>.
        /// </summary>
        public static DefaultODataSerializerProvider Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <inheritdoc />
        public override ODataEdmTypeSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (edmType == null)
            {
                throw Error.ArgumentNull("edmType");
            }

            switch (edmType.TypeKind())
            {
                case EdmTypeKind.Enum:
                    return _enumSerializer;

                case EdmTypeKind.Primitive:
                    return _primitiveSerializer;

                case EdmTypeKind.Collection:
                    IEdmCollectionTypeReference collectionType = edmType.AsCollection();
                    if (collectionType.Definition.IsDeltaFeed())
                    {
                        return _deltaFeedSerializer;
                    }
                    else if (collectionType.ElementType().IsEntity() || collectionType.ElementType().IsComplex())
                    {
                        return _resourceSetSerializer;
                    }
                    else
                    {
                        return _collectionSerializer;
                    }

                case EdmTypeKind.Complex:
                case EdmTypeKind.Entity:
                    return _resourceSerializer;

                default:
                    return null;
            }
        }

        /// <inheritdoc />
        public override ODataSerializer GetODataPayloadSerializer(IEdmModel model, Type type, HttpRequestMessage request)
        {
            if (model == null)
            {
                throw Error.ArgumentNull("model");
            }
            if (type == null)
            {
                throw Error.ArgumentNull("type");
            }
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            // handle the special types.
            if (type == typeof(ODataServiceDocument))
            {
                return _workspaceSerializer;
            }
            else if (type == typeof(Uri) || type == typeof(ODataEntityReferenceLink))
            {
                return _entityReferenceLinkSerializer;
            }
            else if (typeof(IEnumerable<Uri>).IsAssignableFrom(type) || type == typeof(ODataEntityReferenceLinks))
            {
                return _entityReferenceLinksSerializer;
            }
            else if (type == typeof(ODataError) || type == typeof(HttpError))
            {
                return _errorSerializer;
            }
            else if (typeof(IEdmModel).IsAssignableFrom(type))
            {
                return _metadataSerializer;
            }

            // if it is not a special type, assume it has a corresponding EdmType.
            ClrTypeCache typeMappingCache = model.GetTypeMappingCache();
            IEdmTypeReference edmType = typeMappingCache.GetEdmType(type, model);

            if (edmType != null)
            {
                if (((edmType.IsPrimitive() || edmType.IsEnum()) &&
                    ODataRawValueMediaTypeMapping.IsRawValueRequest(request)) ||
                    ODataCountMediaTypeMapping.IsCountRequest(request))
                {
                    return _rawValueSerializer;
                }
                else
                {
                    return GetEdmTypeSerializer(edmType);
                }
            }
            else
            {
                return null;
            }
        }
    }
}