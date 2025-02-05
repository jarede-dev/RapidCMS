﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using RapidCMS.Core.Abstractions.Data;
using RapidCMS.Core.Abstractions.Metadata;
using RapidCMS.Core.Abstractions.UI;
using RapidCMS.Core.Enums;
using RapidCMS.Core.Forms;

namespace RapidCMS.UI.Components.Editors
{
    public class BaseEditor : EditContextComponentBase, IBaseUIElement
    {
        protected ValidationState State { get; private set; }

        [Parameter] public IEntity Entity { get; set; } = default!;
        [Parameter] public IParent? Parent { get; set; }

        [Parameter] public EntityState EntityState { get; set; }
        [Parameter] public ListType DisplayType { get; set; }

        [Parameter] public IPropertyMetadata Property { get; set; } = default!;

        [Parameter] public Func<object, EntityState, bool>? IsDisabledFunc { get; set; }

        [Parameter] public string? Placeholder { get; set; }

        /// <summary>
        /// Implement the IWantConfiguration or IRequireConfiguration interfaces to 
        /// leverage the GetConfigAsync extension method so handling configuration is more easy.
        /// </summary>
        [Parameter] public Func<object, EntityState, Task<object?>>? Configuration { get; set; }

        protected bool IsDisabled => IsDisabledFunc?.Invoke(Entity, EntityState) ?? false; 

        protected object GetValueAsObject()
        {
            return Property.Getter(Entity);
        }

        protected string GetValueAsString()
        {
            return TypeDescriptor.GetConverter(Property.PropertyType).ConvertToString(GetValueAsObject()) ?? "";
        }

        protected IEnumerable<string> GetValidationMessages()
        {
            return EditContext.GetValidationMessages(Property);
        }

        private void ValidationStateChangeHandler(object? sender, ValidationStateChangedEventArgs eventArgs)
        {
            var state = ValidationState.None;

            if (!EditContext.WasValidated(Property))
            {
                state |= ValidationState.NotValidated;
            }
            else
            {
                if (EditContext.IsValid(Property))
                {
                    state |= ValidationState.Valid;
                }
                else
                {
                    state |= ValidationState.Invalid;
                }
            }

            if (EditContext.IsModified())
            {
                state |= ValidationState.Modified;
            }

            State = state;
            
            StateHasChanged();
        }

        protected override void AttachListener()
        {
            if (EditContext != null)
            {
                EditContext.OnValidationStateChanged += ValidationStateChangeHandler;
                EditContext.NotifyPropertyIncludedInForm(Property);

                State = ValidationState.NotValidated;
            }
        }

        protected override void DetachListener()
        {
            if (EditContext != null)
            {
                EditContext.OnValidationStateChanged -= ValidationStateChangeHandler;
            }
        }
    }
}
