﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.OData.Batch;
using System.Web.OData.Formatter;
using System.Web.OData.Properties;
using System.Web.OData.Query;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using ServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace System.Web.OData.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="HttpConfiguration"/> class. 
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpConfigurationExtensions
    {
        // Maintain the System.Web.OData. prefix in any new properties to avoid conflicts with user properties
        // and those of the v3 assembly.
        private const string ETagHandlerKey = "System.Web.OData.ETagHandler";

        private const string TimeZoneInfoKey = "System.Web.OData.TimeZoneInfo";

        private const string ResolverSettingsKey = "System.Web.OData.ResolverSettingsKey";

        private const string ContinueOnErrorKey = "System.Web.OData.ContinueOnErrorKey";

        private const string NullDynamicPropertyKey = "System.Web.OData.NullDynamicPropertyKey";

        private const string ContainerBuilderFactoryKey = "System.Web.OData.ContainerBuilderFactoryKey";

        private const string RootContainerKey = "System.Web.OData.RootContainerKey";

        /// <summary>
        /// Enables query support for actions with an <see cref="IQueryable" /> or <see cref="IQueryable{T}" /> return
        /// type. To avoid processing unexpected or malicious queries, use the validation settings on
        /// <see cref="EnableQueryAttribute"/> to validate incoming queries. For more information, visit
        /// http://go.microsoft.com/fwlink/?LinkId=279712.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        public static void AddODataQueryFilter(this HttpConfiguration configuration)
        {
            AddODataQueryFilter(configuration, new EnableQueryAttribute());
        }

        /// <summary>
        /// Enables query support for actions with an <see cref="IQueryable" /> or <see cref="IQueryable{T}" /> return
        /// type. To avoid processing unexpected or malicious queries, use the validation settings on
        /// <see cref="EnableQueryAttribute"/> to validate incoming queries. For more information, visit
        /// http://go.microsoft.com/fwlink/?LinkId=279712.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="queryFilter">The action filter that executes the query.</param>
        public static void AddODataQueryFilter(this HttpConfiguration configuration, IActionFilter queryFilter)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            configuration.Services.Add(typeof(IFilterProvider), new QueryFilterProvider(queryFilter));
        }

        /// <summary>
        /// Gets the <see cref="IETagHandler"/> from the configuration.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <returns>The <see cref="IETagHandler"/> for the configuration.</returns>
        public static IETagHandler GetETagHandler(this HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            object handler;
            if (!configuration.Properties.TryGetValue(ETagHandlerKey, out handler))
            {
                IETagHandler defaultETagHandler = new DefaultODataETagHandler();
                configuration.SetETagHandler(defaultETagHandler);
                return defaultETagHandler;
            }

            if (handler == null)
            {
                throw Error.InvalidOperation(SRResources.NullETagHandler);
            }

            IETagHandler etagHandler = handler as IETagHandler;
            if (etagHandler == null)
            {
                throw Error.InvalidOperation(SRResources.InvalidETagHandler, handler.GetType());
            }

            return etagHandler;
        }

        /// <summary>
        /// Sets the <see cref="IETagHandler"/> on the configuration.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="handler">The <see cref="IETagHandler"/> for the configuration.</param>
        public static void SetETagHandler(this HttpConfiguration configuration, IETagHandler handler)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }
            if (handler == null)
            {
                throw Error.ArgumentNull("handler");
            }

            configuration.Properties[ETagHandlerKey] = handler;
        }

        /// <summary>
        /// Gets the <see cref="TimeZoneInfo"/> from the configuration.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <returns>The <see cref="TimeZoneInfo"/> for the configuration.</returns>
        public static TimeZoneInfo GetTimeZoneInfo(this HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            object value;
            TimeZoneInfo timeZoneInfo;
            if (!configuration.Properties.TryGetValue(TimeZoneInfoKey, out value))
            {
                timeZoneInfo = TimeZoneInfo.Local;
                configuration.SetTimeZoneInfo(timeZoneInfo);
                return timeZoneInfo;
            }

            timeZoneInfo = value as TimeZoneInfo;
            if (timeZoneInfo == null)
            {
                throw Error.InvalidOperation(SRResources.InvalidTimeZoneInfo, value.GetType(), typeof(TimeZoneInfo));
            }

            return timeZoneInfo;
        }

        /// <summary>
        /// Sets the <see cref="TimeZoneInfo"/> on the configuration.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="timeZoneInfo">The <see cref="TimeZoneInfo"/> for the configuration.</param>
        /// <returns></returns>
        public static void SetTimeZoneInfo(this HttpConfiguration configuration, TimeZoneInfo timeZoneInfo)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            if (timeZoneInfo == null)
            {
                throw Error.ArgumentNull("timeZoneInfo");
            }

            configuration.Properties[TimeZoneInfoKey] = timeZoneInfo;
            TimeZoneInfoHelper.TimeZone = timeZoneInfo;
        }

        /// <summary>
        /// Enable the continue-on-error header.
        /// </summary>
        public static void EnableContinueOnErrorHeader(this HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            configuration.Properties[ContinueOnErrorKey] = true;
        }

        /// <summary>
        /// Check the continue-on-error header is enable or not.
        /// </summary>
        /// <returns></returns>
        internal static bool HasEnabledContinueOnErrorHeader(this HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            object value;
            if (configuration.Properties.TryGetValue(ContinueOnErrorKey, out value))
            {
                return (bool)value;
            }
            return false;
        }

        /// <summary>
        /// Sets whether or not the null dynamic property to be serialized.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="serialize"><c>true</c> to serialize null dynamic property, <c>false</c> otherwise.</param>
        public static void SetSerializeNullDynamicProperty(this HttpConfiguration configuration, bool serialize)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            configuration.Properties[NullDynamicPropertyKey] = serialize;
        }

        /// <summary>
        /// Set the UrlConventions in DefaultODataPathHandler.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="conventions">The <see cref="ODataUrlConventions"/></param>
        public static void SetUrlConventions(this HttpConfiguration configuration, ODataUrlConventions conventions)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            ODataUriResolverSettings settings = configuration.GetResolverSettings();
            settings.UrlConventions = conventions;
        }

        /// <summary>
        /// Sets the Uri resolver for the Uri parser on the configuration.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="uriResolver">The <see cref="ODataUriResolver"/></param>
        public static void SetUriResolver(this HttpConfiguration configuration, ODataUriResolver uriResolver)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            ODataUriResolverSettings settings = configuration.GetResolverSettings();
            settings.UriResolver = uriResolver;
        }

        /// <summary>
        /// Check the null dynamic property is enable or not.
        /// </summary>
        /// <returns></returns>
        internal static bool HasEnabledNullDynamicProperty(this HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            object value;
            if (configuration.Properties.TryGetValue(NullDynamicPropertyKey, out value))
            {
                return (bool)value;
            }

            return false;
        }

        internal static ODataUriResolverSettings GetResolverSettings(this HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            object value;
            if (configuration.Properties.TryGetValue(ResolverSettingsKey, out value))
            {
                return value as ODataUriResolverSettings;
            }

            ODataUriResolverSettings defaultSettings = new ODataUriResolverSettings();
            configuration.Properties[ResolverSettingsKey] = defaultSettings;
            return defaultSettings;
        }

        internal static IServiceProvider GetRootContainer(this HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            object value;
            if (configuration.Properties.TryGetValue(RootContainerKey, out value))
            {
                return value as IServiceProvider;
            }

            throw Error.InvalidOperation(SRResources.NullContainer);
        }

        internal static void SetRootContainer(this HttpConfiguration configuration, IServiceProvider rootContainer)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            if (rootContainer == null)
            {
                throw Error.ArgumentNull("rootContainer");
            }

            configuration.Properties[RootContainerKey] = rootContainer;
        }

        /// <summary>
        /// Specifies a custom container builder.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="builderFactory">The factory to create a container builder.</param>
        /// <returns>The server configuration.</returns>
        public static HttpConfiguration UseCustomContainerBuilder(this HttpConfiguration configuration, Func<IContainerBuilder> builderFactory)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            if (builderFactory == null)
            {
                throw Error.ArgumentNull("builderFactory");
            }

            configuration.Properties[ContainerBuilderFactoryKey] = builderFactory;

            return configuration;
        }

        /// <summary>
        /// Maps the specified OData route and the OData route attributes.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="setupAction">The setup action to add the services to the root container.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRoute(this HttpConfiguration configuration, string routeName,
            string routePrefix, Action<IContainerBuilder> setupAction)
        {
            if (configuration == null)
            {
                throw Error.ArgumentNull("configuration");
            }

            // 1) Build and configure the root container.
            IContainerBuilder builder = configuration.CreateContainerBuilderWithDefaultServices();
            
            if (setupAction != null)
            {
                setupAction(builder);
            }

            IServiceProvider rootContainer = builder.BuildContainer();
            if (rootContainer == null)
            {
                throw Error.InvalidOperation(SRResources.NullContainer);
            }

            configuration.SetRootContainer(rootContainer);

            // 2) Resolve the path handler and set URI resolver to it.
            IODataPathHandler pathHandler = rootContainer.GetRequiredService<IODataPathHandler>();

            // if settings is not on local, use the global configuration settings.
            ODataUriResolverSettings settings = configuration.GetResolverSettings();
            IODataUriResolver pathResolver = pathHandler as IODataUriResolver;
            if (pathResolver != null && pathResolver.UriResolver == null)
            {
                pathResolver.UriResolver = settings.UriResolver;
            }

            if (pathResolver != null && pathResolver.UrlConventions == null)
            {
                pathResolver.UrlConventions = settings.UrlConventions;
            }

            // 3) Resolve some required services and create the route constraint.
            IEdmModel model = rootContainer.GetRequiredService<IEdmModel>();
            IList<IODataRoutingConvention> routingConventions =
                rootContainer.GetServices<IODataRoutingConvention>().ToList();
            if (routingConventions.Count == 0)
            {
                // CANNOT add the routing conventions within AddDefaultWebApiServices because
                // then user will have no way to replace the whole set of routing conventions
                // with his own ones.
                routingConventions = ODataRoutingConventions.CreateDefaultWithAttributeRouting(configuration, model);
            }

            HttpRouteCollection routes = configuration.Routes;
            routePrefix = RemoveTrailingSlash(routePrefix);
            ODataPathRouteConstraint routeConstraint = new ODataPathRouteConstraint(
                pathHandler, model, routeName, routingConventions, rootContainer);

            // 4) Resolve HTTP handler, create the OData route and register it.
            ODataRoute route;
            HttpMessageHandler messageHandler = rootContainer.GetService<HttpMessageHandler>();
            if (messageHandler != null)
            {
                route = new ODataRoute(
                    routePrefix,
                    routeConstraint,
                    defaults: null,
                    constraints: null,
                    dataTokens: null,
                    handler: messageHandler);
            }
            else
            {
                ODataBatchHandler batchHandler = rootContainer.GetService<ODataBatchHandler>();
                if (batchHandler != null)
                {
                    batchHandler.ODataRouteName = routeName;
                    string batchTemplate = String.IsNullOrEmpty(routePrefix) ? ODataRouteConstants.Batch
                        : routePrefix + '/' + ODataRouteConstants.Batch;
                    routes.MapHttpBatchRoute(routeName + "Batch", batchTemplate, batchHandler);
                }

                route = new ODataRoute(routePrefix, routeConstraint);
            }

            routes.Add(routeName, route);
            return route;
        }

        /// <summary>
        /// Maps the specified OData route and the OData route attributes.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRoute(this HttpConfiguration configuration, string routeName,
            string routePrefix, IEdmModel model)
        {
            return configuration.MapODataServiceRoute(routeName, routePrefix, builder =>
                builder.AddService(ServiceLifetime.Singleton, sp => model));
        }

        /// <summary>
        /// Maps the specified OData route and the OData route attributes. When the <paramref name="batchHandler"/> is
        /// non-<c>null</c>, it will create a '$batch' endpoint to handle the batch requests.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <param name="batchHandler">The <see cref="ODataBatchHandler"/>.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRoute(this HttpConfiguration configuration, string routeName,
            string routePrefix, IEdmModel model, ODataBatchHandler batchHandler)
        {
            return configuration.MapODataServiceRoute(routeName, routePrefix, builder =>
                builder.AddService(ServiceLifetime.Singleton, sp => model)
                       .AddService(ServiceLifetime.Singleton, sp => batchHandler));
        }

        /// <summary>
        /// Maps the specified OData route and the OData route attributes. When the <paramref name="defaultHandler"/>
        /// is non-<c>null</c>, it will map it as the default handler for the route.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <param name="defaultHandler">The default <see cref="HttpMessageHandler"/> for this route.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRoute(this HttpConfiguration configuration, string routeName,
            string routePrefix, IEdmModel model, HttpMessageHandler defaultHandler)
        {
            return configuration.MapODataServiceRoute(routeName, routePrefix, builder =>
                builder.AddService(ServiceLifetime.Singleton, sp => model)
                       .AddService(ServiceLifetime.Singleton, sp => defaultHandler));
        }

        /// <summary>
        /// Maps the specified OData route.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <param name="pathHandler">The <see cref="IODataPathHandler"/> to use for parsing the OData path.</param>
        /// <param name="routingConventions">
        /// The OData routing conventions to use for controller and action selection.
        /// </param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRoute(this HttpConfiguration configuration, string routeName,
            string routePrefix, IEdmModel model, IODataPathHandler pathHandler,
            IEnumerable<IODataRoutingConvention> routingConventions)
        {
            return configuration.MapODataServiceRoute(routeName, routePrefix, builder =>
                builder.AddService(ServiceLifetime.Singleton, sp => model)
                       .AddService(ServiceLifetime.Singleton, sp => pathHandler)
                       .AddService(ServiceLifetime.Singleton, sp => routingConventions.ToList().AsEnumerable()));
        }

        /// <summary>
        /// Maps the specified OData route. When the <paramref name="batchHandler"/> is non-<c>null</c>, it will
        /// create a '$batch' endpoint to handle the batch requests.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <param name="pathHandler">The <see cref="IODataPathHandler" /> to use for parsing the OData path.</param>
        /// <param name="routingConventions">
        /// The OData routing conventions to use for controller and action selection.
        /// </param>
        /// <param name="batchHandler">The <see cref="ODataBatchHandler"/>.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "We want the handler to be a batch handler.")]
        public static ODataRoute MapODataServiceRoute(this HttpConfiguration configuration, string routeName,
            string routePrefix, IEdmModel model, IODataPathHandler pathHandler,
            IEnumerable<IODataRoutingConvention> routingConventions, ODataBatchHandler batchHandler)
        {
            return configuration.MapODataServiceRoute(routeName, routePrefix, builder =>
                builder.AddService(ServiceLifetime.Singleton, sp => model)
                       .AddService(ServiceLifetime.Singleton, sp => pathHandler)
                       .AddService(ServiceLifetime.Singleton, sp => routingConventions.ToList().AsEnumerable())
                       .AddService(ServiceLifetime.Singleton, sp => batchHandler));
        }

        /// <summary>
        /// Maps the specified OData route. When the <paramref name="defaultHandler"/> is non-<c>null</c>, it will map
        /// it as the handler for the route.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <param name="pathHandler">The <see cref="IODataPathHandler" /> to use for parsing the OData path.</param>
        /// <param name="routingConventions">
        /// The OData routing conventions to use for controller and action selection.
        /// </param>
        /// <param name="defaultHandler">The default <see cref="HttpMessageHandler"/> for this route.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRoute(this HttpConfiguration configuration, string routeName,
            string routePrefix, IEdmModel model, IODataPathHandler pathHandler,
            IEnumerable<IODataRoutingConvention> routingConventions, HttpMessageHandler defaultHandler)
        {
            return configuration.MapODataServiceRoute(routeName, routePrefix, builder =>
                builder.AddService(ServiceLifetime.Singleton, sp => model)
                       .AddService(ServiceLifetime.Singleton, sp => pathHandler)
                       .AddService(ServiceLifetime.Singleton, sp => routingConventions.ToList().AsEnumerable())
                       .AddService(ServiceLifetime.Singleton, sp => defaultHandler));
        }

        private static string RemoveTrailingSlash(string routePrefix)
        {
            if (!String.IsNullOrEmpty(routePrefix))
            {
                int prefixLastIndex = routePrefix.Length - 1;
                if (routePrefix[prefixLastIndex] == '/')
                {
                    // Remove the last trailing slash if it has one.
                    routePrefix = routePrefix.Substring(0, routePrefix.Length - 1);
                }
            }
            return routePrefix;
        }

        private static IContainerBuilder CreateContainerBuilderWithDefaultServices(this HttpConfiguration configuration)
        {
            IContainerBuilder builder;

            object value;
            if (configuration.Properties.TryGetValue(ContainerBuilderFactoryKey, out value))
            {
                Func<IContainerBuilder> builderFactory = (Func<IContainerBuilder>)value;

                builder = builderFactory();
                if (builder == null)
                {
                    throw Error.InvalidOperation(SRResources.NullContainerBuilder);
                }
            }
            else
            {
                builder = new DefaultContainerBuilder();
            }

            builder.AddService(ServiceLifetime.Singleton, sp => configuration);
            builder.AddDefaultODataServices();
            builder.AddDefaultWebApiServices();

            return builder;
        }
    }
}