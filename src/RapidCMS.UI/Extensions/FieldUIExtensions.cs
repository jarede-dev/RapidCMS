﻿using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RapidCMS.Core.Enums;
using RapidCMS.Core.Forms;
using RapidCMS.Core.Models.UI;
using RapidCMS.UI.Components.Displays;
using RapidCMS.UI.Components.Editors;

namespace RapidCMS.UI.Extensions
{
    public static class FieldUIExtensions
    {
        public static RenderFragment? ToRenderFragment(this FieldUI field, FormEditContext editContext, ListType usedInDisplayType)
        {
            if (field is CustomExpressionFieldUI customExpressionField)
            {
                return builder =>
                {
                    var editorType = customExpressionField.CustomType;

                    builder.OpenComponent(0, editorType);

                    builder.AddAttributes(editContext, customExpressionField, usedInDisplayType);

                    builder.CloseComponent();
                };
            }
            else if (field is ExpressionFieldUI expressionField)
            {
                var displayType = expressionField.Type.GetDisplay();
                if (displayType == null)
                {
                    return null;
                }

                return builder =>
                {
                    builder.OpenComponent(0, displayType);

                    builder.AddAttributes(editContext, expressionField, usedInDisplayType);

                    builder.CloseComponent();
                };
            }
            else if (field is CustomPropertyFieldUI customPropertyField)
            {
                return builder =>
                {
                    var editorType = customPropertyField.CustomType;

                    builder.OpenComponent(0, editorType);

                    builder.AddAttributes(editContext, customPropertyField, usedInDisplayType);

                    if (editorType.IsSubclassOf(typeof(BaseDataEditor)))
                    {
                        builder.AddAttribute(9, nameof(BaseDataEditor.DataCollection), customPropertyField.DataCollection);
                    }

                    builder.CloseComponent();
                };
            }
            else if (field is PropertyFieldUI propertyField)
            {
                var editorType = propertyField.Type.GetEditor();
                if (editorType == null)
                {
                    return null;
                }

                return builder =>
                {
                    builder.OpenComponent(0, editorType);

                    builder.AddAttributes(editContext, propertyField, usedInDisplayType);

                    if (editorType.IsSubclassOf(typeof(BaseDataEditor)))
                    {
                        builder.AddAttribute(9, nameof(BaseDataEditor.DataCollection), propertyField.DataCollection);
                    }
                    if (editorType.IsSubclassOf(typeof(BaseRelationEditor)))
                    {
                        builder.AddAttribute(10, nameof(BaseRelationEditor.DataCollection), propertyField.DataCollection);
                    }
                    builder.CloseComponent();
                };
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static void AddAttributes(this RenderTreeBuilder builder, FormEditContext editContext, ExpressionFieldUI expressionField, ListType displayType)
        {
            builder.AddAttribute(1, nameof(BaseDisplay.Entity), editContext.Entity);
            builder.AddAttribute(2, nameof(BaseDisplay.EntityState), editContext.EntityState);
            builder.AddAttribute(3, nameof(BaseDisplay.Parent), editContext.Parent);
            builder.AddAttribute(4, nameof(BaseDisplay.Expression), expressionField.Expression);
            builder.AddAttribute(5, nameof(BaseDisplay.DisplayType), displayType);
            builder.AddAttribute(6, nameof(BaseDisplay.Configuration), expressionField.Configuration);
        }

        private static void AddAttributes(this RenderTreeBuilder builder, FormEditContext editContext, PropertyFieldUI propertyField, ListType displayType)
        {
            builder.AddAttribute(1, nameof(BaseEditor.Entity), editContext.Entity);
            builder.AddAttribute(2, nameof(BaseEditor.EntityState), editContext.EntityState);
            builder.AddAttribute(3, nameof(BaseEditor.Parent), editContext.Parent);
            builder.AddAttribute(4, nameof(BaseEditor.Property), propertyField.Property);
            builder.AddAttribute(5, nameof(BaseEditor.IsDisabledFunc), propertyField.IsDisabled);
            builder.AddAttribute(6, nameof(BaseEditor.Placeholder), propertyField.Placeholder);
            builder.AddAttribute(7, nameof(BaseEditor.DisplayType), displayType);
            builder.AddAttribute(8, nameof(BaseEditor.Configuration), propertyField.Configuration);
        }
    }
}
