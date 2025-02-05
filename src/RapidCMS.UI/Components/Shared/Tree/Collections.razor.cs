﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using RapidCMS.Core.Abstractions.Mediators;
using RapidCMS.Core.Abstractions.Services;
using RapidCMS.Core.Extensions;
using RapidCMS.Core.Models.Data;
using RapidCMS.Core.Models.EventArgs.Mediators;
using RapidCMS.Core.Models.UI;

namespace RapidCMS.UI.Components.Shared.Tree
{
    public partial class Collections
    {
        [Inject] private ITreeService TreeService { get; set; } = default!;
        [Inject] private IMediator Mediator { get; set; } = default!;

        [Parameter] public string CollectionAlias { get; set; } = default!;

        [Parameter] public ParentPath? ParentPath { get; set; } = null;

        private bool NodesVisible { get; set; }

        private TreeCollectionUI? UI { get; set; }
        private string? Error { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            DisposeWhenDisposing(Mediator.RegisterCallback<NavigationEventArgs>(LocationChangedAsync));
        }

        protected override async Task OnParametersSetAsync()
        {
            try
            {
                UI = await TreeService.GetCollectionAsync(CollectionAlias, ParentPath);

                NodesVisible = NodesVisible || (UI?.DefaultOpenEntities ?? false);

                if (Mediator.GetLatestEventArgs<NavigationEventArgs>() is NavigationEventArgs @event)
                {
                    await LocationChangedAsync(this, @event);
                }
            }
            catch (UnauthorizedAccessException)
            {
                UI = TreeCollectionUI.None;
            }
            catch (Exception ex)
            {
                UI = null;
                Error = ex.Message;
            }

            StateHasChanged();
        }

        private async Task LocationChangedAsync(object sender, NavigationEventArgs args)
        {
            if (sender is NavigationLink || UI == null || args.OldState != null || !args.NewState.HasCollectionAlias)
            {
                return;
            }

            if ((ParentPath?.ToPathString() == args.NewState.ParentPath?.ToPathString() && args.NewState.CollectionAlias == CollectionAlias) ||
                ParentPath.IsBaseOf(args.NewState.ParentPath, UI.RepositoryAlias, default))
            {
                await InvokeAsync(() =>
                {
                    NodesVisible = true;
                    StateHasChanged();
                });
            }
        }
    }
}
