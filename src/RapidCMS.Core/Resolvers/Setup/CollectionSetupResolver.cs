﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RapidCMS.Core.Abstractions.Config;
using RapidCMS.Core.Abstractions.Plugins;
using RapidCMS.Core.Abstractions.Resolvers;
using RapidCMS.Core.Enums;
using RapidCMS.Core.Extensions;
using RapidCMS.Core.Models.Config;
using RapidCMS.Core.Models.Setup;
using RapidCMS.Core.Validators;

namespace RapidCMS.Core.Resolvers.Setup
{
    internal class CollectionSetupResolver : ISetupResolver<CollectionSetup>
    {
        private readonly ICmsConfig _cmsConfig;
        private readonly ISetupResolver<IEnumerable<TreeElementSetup>, IEnumerable<ITreeElementConfig>> _treeElementResolver;
        private readonly ISetupResolver<EntityVariantSetup, EntityVariantConfig> _entityVariantResolver;
        private readonly ISetupResolver<TreeViewSetup, TreeViewConfig> _treeViewResolver;
        private readonly ISetupResolver<ElementSetup, ElementConfig> _elementResolver;
        private readonly ISetupResolver<ListSetup, ListConfig> _listResolver;
        private readonly ISetupResolver<NodeSetup, NodeConfig> _nodeResolver;
        private readonly IRepositoryTypeResolver _repositoryTypeResolver;
        private readonly IEnumerable<IPlugin> _plugins;

        private Dictionary<string, IPlugin> _pluginMap { get; set; } = new Dictionary<string, IPlugin>();
        private Dictionary<string, CollectionConfig> _collectionMap { get; set; } = new Dictionary<string, CollectionConfig>();
        private Dictionary<string, CollectionSetup> _cachedCollectionMap { get; set; } = new Dictionary<string, CollectionSetup>();

        public CollectionSetupResolver(ICmsConfig cmsConfig,
            ISetupResolver<IEnumerable<TreeElementSetup>, IEnumerable<ITreeElementConfig>> treeElementResolver,
            ISetupResolver<EntityVariantSetup, EntityVariantConfig> entityVariantResolver,
            ISetupResolver<TreeViewSetup, TreeViewConfig> treeViewResolver,
            ISetupResolver<ElementSetup, ElementConfig> elementResolver,
            ISetupResolver<ListSetup, ListConfig> listResolver,
            ISetupResolver<NodeSetup, NodeConfig> nodeResolver,
            IRepositoryTypeResolver repositoryTypeResolver,
            IEnumerable<IPlugin> plugins)
        {
            _cmsConfig = cmsConfig;
            _treeElementResolver = treeElementResolver;
            _entityVariantResolver = entityVariantResolver;
            _treeViewResolver = treeViewResolver;
            _elementResolver = elementResolver;
            _listResolver = listResolver;
            _nodeResolver = nodeResolver;
            _repositoryTypeResolver = repositoryTypeResolver;
            _plugins = plugins;
            Initialize();
        }

        private void Initialize()
        {
            MapCollections(_cmsConfig.CollectionsAndPages.OfType<CollectionConfig>());

            foreach (var plugin in _plugins)
            {
                _pluginMap.Add(plugin.CollectionPrefix, plugin);
            }

            void MapCollections(IEnumerable<CollectionConfig> collections)
            {
                foreach (var collection in collections.Where(col => col is not ReferencedCollectionConfig))
                {
                    if (!_collectionMap.TryAdd(collection.Alias, collection))
                    {
                        throw new InvalidOperationException($"Duplicate collection alias '{collection.Alias}' not allowed.");
                    }

                    var subCollections = collection.CollectionsAndPages.OfType<CollectionConfig>();
                    if (subCollections.Any())
                    {
                        MapCollections(subCollections);
                    }
                }
            }
        }

        Task<CollectionSetup> ISetupResolver<CollectionSetup>.ResolveSetupAsync()
        {
            throw new InvalidOperationException("Cannot resolve collection or page without alias.");
        }

        async Task<CollectionSetup> ISetupResolver<CollectionSetup>.ResolveSetupAsync(string alias)
        {
            if (_cachedCollectionMap.TryGetValue(alias, out var collectionSetup))
            {
                return collectionSetup;
            }
            else if (_cachedCollectionMap.FirstOrDefault(x => x.Value.RepositoryAlias == alias).Value is CollectionSetup collection)
            {
                return collection;
            }

            var resolvedSetup = default(IResolvedSetup<CollectionSetup>);
            if (_collectionMap.TryGetValue(alias, out var collectionConfig))
            {
                resolvedSetup = await ConvertConfigAsync(collectionConfig);
                
            }
            else if (alias.TryParseAsPluginAlias(out var pluginAlias) && 
                _pluginMap.TryGetValue(pluginAlias.prefix, out var plugin) &&
                await plugin.GetCollectionAsync(pluginAlias.collectionAlias) is ResolvedSetup<CollectionSetup> collection)
            {
                resolvedSetup = collection;
            }

            if (resolvedSetup == null)
            {
                throw new InvalidOperationException($"Cannot find collection with alias {alias}.");
            }

            if (resolvedSetup.Cachable)
            {
                _cachedCollectionMap[alias] = resolvedSetup.Setup;
            }

            return resolvedSetup.Setup;
        }

        private async Task<IResolvedSetup<CollectionSetup>> ConvertConfigAsync(CollectionConfig config)
        {
            var repositoryAlias = _repositoryTypeResolver.GetAlias(config.RepositoryType);

            var collection = new CollectionSetup(
                config.Icon,
                config.Color,
                config.Name,
                config.Alias,
                repositoryAlias)
            {
                DataViews = config.DataViews,
                DataViewBuilder = config.DataViewBuilder,
                UsageType = GetCollectionUsage(config),
                Validators = config.Validators.ToList(x => new ValidationSetup(x.Type, x.Configuration))
            };

            if (!_cmsConfig.Advanced.RemoveDataAnnotationEntityValidator)
            {
                collection.Validators.Insert(0, new ValidationSetup(typeof(DataAnnotationEntityValidator), default));
            }

            var cacheable = true;

            if (!string.IsNullOrWhiteSpace(config.ParentAlias) && _collectionMap.TryGetValue(config.ParentAlias, out var collectionConfig))
            {
                collection.Parent = new TreeElementSetup(collectionConfig.Alias, collectionConfig.Name, PageType.Collection); // TODO: this assumes nesting is always with collections
            }
            collection.Collections = (await _treeElementResolver.ResolveSetupAsync(config.CollectionsAndPages, collection)).CheckIfCachable(ref cacheable).ToList();

            collection.EntityVariant = (await _entityVariantResolver.ResolveSetupAsync(config.EntityVariant, collection)).CheckIfCachable(ref cacheable);
            if (config.SubEntityVariants.Any())
            {
                collection.SubEntityVariants = (await _entityVariantResolver.ResolveSetupAsync(config.SubEntityVariants, collection)).CheckIfCachable(ref cacheable).ToList();
            }

            collection.TreeView = config.TreeView == null ? null : (await _treeViewResolver.ResolveSetupAsync(config.TreeView, collection)).CheckIfCachable(ref cacheable);

            collection.ElementSetup = config.ElementConfig == null ? null : (await _elementResolver.ResolveSetupAsync(config.ElementConfig, collection)).CheckIfCachable(ref cacheable);

            collection.ListView = config.ListView == null ? null : (await _listResolver.ResolveSetupAsync(config.ListView, collection)).CheckIfCachable(ref cacheable);
            collection.ListEditor = config.ListEditor == null ? null : (await _listResolver.ResolveSetupAsync(config.ListEditor, collection)).CheckIfCachable(ref cacheable);

            collection.NodeView = config.NodeView == null ? null : (await _nodeResolver.ResolveSetupAsync(config.NodeView, collection)).CheckIfCachable(ref cacheable);
            collection.NodeEditor = config.NodeEditor == null ? null : (await _nodeResolver.ResolveSetupAsync(config.NodeEditor, collection)).CheckIfCachable(ref cacheable);

            return new ResolvedSetup<CollectionSetup>(collection, cacheable);
        }

        private static UsageType GetCollectionUsage(CollectionConfig config)
        {
            var hasDetailsPageUsage = config.GetType().IsGenericType && config.GetType().GetGenericTypeDefinition() == typeof(DetailPageConfig<>);
            var hasNodeUsage = !hasDetailsPageUsage && (config.NodeEditor != null || config.NodeView != null);
            var hasCollectionUsage = config.ListEditor != null || config.ListView != null;

            return (hasNodeUsage ? UsageType.Node : UsageType.None) |
                (hasCollectionUsage ? UsageType.List : UsageType.None) |
                (hasDetailsPageUsage ? UsageType.Details : UsageType.None);
        }
    }
}
