﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RapidCMS.Core.Abstractions.Config;
using RapidCMS.Core.Abstractions.Resolvers;
using RapidCMS.Core.Enums;
using RapidCMS.Core.Models.Config;
using RapidCMS.Core.Models.Setup;

namespace RapidCMS.Core.Resolvers.Setup
{
    internal class TreeElementSetupResolver : ISetupResolver<IEnumerable<TreeElementSetup>, IEnumerable<ITreeElementConfig>>
    {
        public Task<IResolvedSetup<IEnumerable<TreeElementSetup>>> ResolveSetupAsync(IEnumerable<ITreeElementConfig> config, CollectionSetup? collection = default)
        {
            return Task.FromResult< IResolvedSetup<IEnumerable<TreeElementSetup>>>(
                new ResolvedSetup<IEnumerable<TreeElementSetup>>(
                    config.Select(corp =>
                    {
                        var type = corp switch
                        {
                            IPageConfig page => PageType.Page,
                            _ => PageType.Collection
                        };

                        return new TreeElementSetup(corp.Alias, corp.Name, type)
                        {
                            RootVisibility = (corp as CollectionConfig)?.TreeView?.RootVisibility ?? default
                        };

                    }) ?? Enumerable.Empty<TreeElementSetup>(),
                    true));
        }
    }
}
