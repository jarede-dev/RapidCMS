﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using RapidCMS.Common.Data;
using RapidCMS.Common.Exceptions;
using RapidCMS.Common.Helpers;


namespace RapidCMS.Common.Models.Config
{
    // TODO: add button support
    public abstract class NodeEditorPaneConfig
    {
        protected NodeEditorPaneConfig(Type variantType)
        {
            VariantType = variantType ?? throw new ArgumentNullException(nameof(variantType));
        }

        internal string? CustomAlias { get; set; }
        internal string? Label { get; set; }

        internal Func<object, bool> IsVisible { get; set; } = (x) => true;

        internal Type VariantType { get; set; }

        internal int FieldIndex { get; set; } = 0;

        internal List<FieldConfig> Fields { get; set; } = new List<FieldConfig>();
        internal List<SubCollectionListConfig> SubCollectionLists { get; set; } = new List<SubCollectionListConfig>();
        internal List<RelatedCollectionListConfig> RelatedCollectionLists { get; set; } = new List<RelatedCollectionListConfig>();
    }

    public class NodeEditorPaneConfig<TEntity> : NodeEditorPaneConfig
        where TEntity : IEntity
    {
        public NodeEditorPaneConfig(Type variantType) : base(variantType)
        {
        }

        public NodeEditorPaneConfig(Type variantType, Type customSectionType) : base(variantType)
        {
            CustomAlias = customSectionType.FullName;
        }

        public NodeEditorPaneConfig<TEntity> SetLabel(string label)
        {
            Label = label;

            return this;
        }

        public FieldConfig<TEntity> AddField<TValue>(Expression<Func<TEntity, TValue>> propertyExpression, Action<FieldConfig<TEntity>>? configure = null)
        {
            var config = new FieldConfig<TEntity>()
            {
                Property = PropertyMetadataHelper.GetPropertyMetadata(propertyExpression) ?? throw new InvalidPropertyExpressionException(nameof(propertyExpression)),
            };
            config.Name = config.Property.PropertyName;
            config.Type = EditorTypeHelper.TryFindDefaultEditorType(config.Property.PropertyType);

            configure?.Invoke(config);

            config.Index = FieldIndex++;

            Fields.Add(config);

            return config;
        }


        // TODO: check if sub collection is part of collection
        public NodeEditorPaneConfig<TEntity> AddSubCollectionListEditor<TSubEntity>(string collectionAlias, Action<SubCollectionListConfig<TSubEntity>>? configure = null)
            where TSubEntity : IEntity
        {
            var config = new SubCollectionListConfig<TSubEntity>(collectionAlias);

            configure?.Invoke(config);

            config.Index = FieldIndex++;

            SubCollectionLists.Add(config);

            return this;
        }

        public NodeEditorPaneConfig<TEntity> AddRelatedCollectionListEditor<TRelatedEntity>(string collectionAlias, Action<RelatedCollectionListConfig<TEntity,TRelatedEntity>>? configure = null)
            where TRelatedEntity : IEntity
        {
            var config = new RelatedCollectionListConfig<TEntity, TRelatedEntity>(collectionAlias);

            configure?.Invoke(config);

            config.Index = FieldIndex++;

            RelatedCollectionLists.Add(config);

            return this;
        }

        public NodeEditorPaneConfig<TEntity> VisibleWhen(Func<TEntity, bool> predicate)
        {
            IsVisible = (entity) => predicate.Invoke((TEntity)entity);

            return this;
        }
    }
}
