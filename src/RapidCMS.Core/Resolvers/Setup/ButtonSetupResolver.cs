﻿using System;
using System.Linq;
using System.Threading.Tasks;
using RapidCMS.Core.Abstractions.Resolvers;
using RapidCMS.Core.Abstractions.Setup;
using RapidCMS.Core.Attributes;
using RapidCMS.Core.Enums;
using RapidCMS.Core.Extensions;
using RapidCMS.Core.Handlers;
using RapidCMS.Core.Models.Config;
using RapidCMS.Core.Models.Setup;

namespace RapidCMS.Core.Resolvers.Setup
{
    internal class ButtonSetupResolver : ISetupResolver<ButtonSetup, ButtonConfig>
    {
        private readonly ILanguageResolver _languageResolver;

        public ButtonSetupResolver(ILanguageResolver languageResolver)
        {
            _languageResolver = languageResolver;
        }

        public Task<IResolvedSetup<ButtonSetup>> ResolveSetupAsync(ButtonConfig config, CollectionSetup? collection = default)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            var @default = (config as DefaultButtonConfig)?.ButtonType.GetCustomAttribute<DefaultIconLabelAttribute>();

            var button = new ButtonSetup
            {
                Buttons = Enumerable.Empty<ButtonSetup>(),

                Label = _languageResolver.ResolveText(config.Label ?? @default?.Label ?? "Button"),
                Icon = config.Icon ?? @default?.Icon ?? "",
                IsPrimary = config.IsPrimary,
                IsVisible = config.IsVisible,

                ButtonId = config.Id ?? throw new ArgumentNullException(nameof(config.Id)),
                EntityVariant = collection.EntityVariant
            };

            if (config is DefaultButtonConfig defaultButton)
            {
                if (defaultButton.ButtonType == DefaultButtonType.OpenPane)
                {
                    throw new InvalidOperationException($"An {DefaultButtonType.OpenPane} button is not allowed to be used by DefaultButton");
                }

                button.DefaultButtonType = defaultButton.ButtonType;

                button.ButtonHandlerType = typeof(DefaultButtonActionHandler);

                if (defaultButton.ButtonType == DefaultButtonType.New && collection.SubEntityVariants != null)
                {
                    button.Buttons = collection.SubEntityVariants.ToList(variant =>
                        new ButtonSetup
                        {
                            Label = _languageResolver.ResolveText(string.Format(button.Label ?? variant.Name, variant.Name)),
                            Icon = variant.Icon ?? @default?.Icon ?? "",
                            IsPrimary = config.IsPrimary,
                            ButtonId = $"{config.Id}-{variant.Alias}",
                            EntityVariant = variant,
                            DefaultButtonType = DefaultButtonType.New,
                            ButtonHandlerType = typeof(DefaultButtonActionHandler),
                            Buttons = Enumerable.Empty<ButtonSetup>()
                        });
                }
            }
            else if (config is CustomButtonConfig customButton)
            {
                button.CustomType = customButton.CustomType;
                button.ButtonHandlerType = customButton.ActionHandler;
            }
            else if (config is PaneButtonConfig paneButton)
            {
                button.DefaultButtonType = DefaultButtonType.OpenPane;
                button.ButtonHandlerType = typeof(OpenPaneButtonActionHandler<>).MakeGenericType(paneButton.PaneType);
            }
            else if (config is NavigationButtonConfig navigationButton)
            {
                button.DefaultButtonType = DefaultButtonType.Navigate;
                button.ButtonHandlerType = typeof(NavigateButtonActionHandler<>).MakeGenericType(navigationButton.HandlerType);
            }
            else
            {
                throw new InvalidOperationException();
            }

            return Task.FromResult<IResolvedSetup<ButtonSetup>>(new ResolvedSetup<ButtonSetup>(button, true));
        }
    }
}
