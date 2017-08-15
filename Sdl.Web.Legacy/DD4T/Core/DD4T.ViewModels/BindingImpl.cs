using DD4T.ContentModel;
using DD4T.Core.Contracts.ViewModels;
using DD4T.Core.Contracts.ViewModels.Binding;
using DD4T.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;


namespace DD4T.ViewModels.Binding
{

    public class DefaultMappedModelFactory : IMappedModelFactory
    {
        //private static readonly IMappedModelFactory instance = new DefaultMappedModelFactory(ViewModelDefaults.Factory);
        ////Optional singleton
        //public static IMappedModelFactory Instance { get { return instance; } }

        protected IViewModelFactory factory;
        public DefaultMappedModelFactory(IViewModelFactory factory)
        {
            this.factory = factory;
        }
        public virtual object BuildMappedModel(IModel modelData, IModelMapping mapping)
        {
            var model = factory.ModelResolver.ResolveInstance(mapping.ModelType);
            return BuildMappedModel(model, modelData, mapping);
        }

        public virtual T BuildMappedModel<T>(IModel modelData, IModelMapping mapping) //where T: class
        {
            T model = (T)factory.ModelResolver.ResolveInstance(typeof(T));
            return BuildMappedModel<T>(model, modelData, mapping);
        }

        public virtual T BuildMappedModel<T>(T model, IModel modelData, IModelMapping mapping) //where T : class
        {
            foreach (var property in mapping.ModelProperties)
            {
                factory.SetPropertyValue(model, modelData, property);
            }
            return model;
        }
    }

    public class BindingContainer : IBindingContainer
    {
        private readonly IDictionary<Type, IModelMapping> modelMappings =
            new Dictionary<Type, IModelMapping>();
        private readonly IDictionary<Type, List<IPropertyMapping>> propertyMappingLists =
            new Dictionary<Type, List<IPropertyMapping>>();
        private readonly IViewModelResolver resolver;

        public BindingContainer(IViewModelResolver resolver, IReflectionHelper helper, params IBindingModule[] modules)
        {
            this.resolver = resolver;
            foreach (var module in modules)
            {
                module.OnLoad(resolver, helper);
                Load(module);
            }
        }

        public IModelMapping GetMapping<T>()
        {
            return GetMapping(typeof(T));
        }

        public IModelMapping GetMapping(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            IModelMapping result = null;
            var generics = type.GetGenericArguments();
            if (generics.Length > 0)
            {
                Type genericType = type.GetGenericArguments()[0];
                if (genericType != null && typeof(ICollection<>).MakeGenericType(genericType).IsAssignableFrom(type))
                    type = genericType;
            }
            if (modelMappings.ContainsKey(type))
            {
                result = modelMappings[type];
            }
            else if (propertyMappingLists.ContainsKey(type))
            {
                var propertyMappings = propertyMappingLists[type];
                //IList<IModelProperty> modelProperties = new List<IModelProperty>();
                var modelProperties = propertyMappings.Select(mapping =>
                {
                    mapping.GetMapping(mapping.PropertyAttribute, this);    //GetMapping is called when the mapping is first requested, assuming all Binding Modules have been loaded up
                    return resolver.GetModelProperty(mapping.Property, mapping.PropertyAttribute);
                }).ToList();
                //foreach (var mapping in propertyMappings)
                //{
                //    mapping.GetMapping(mapping.PropertyAttribute, this);
                //    modelProperties.Add(resolver.GetModelProperty(mapping.Property, mapping.PropertyAttribute));
                //}
                result = new ModelMapping(type, modelProperties);
                modelMappings.Add(type, result); //The first time it's asked for, it's loaded into the dictionary permanently and can't change
            }
            return result;
        }

        protected virtual void Load(IBindingModule module)
        {
            if (module == null) throw new ArgumentNullException("module");
            module.Load();
            foreach (var mapping in module.ModelMappings)
            {
                if (propertyMappingLists.ContainsKey(mapping.Key))
                {
                    var propMappingList = propertyMappingLists[mapping.Key];
                    propMappingList.AddRange(mapping.Value);
                }
                else
                {
                    propertyMappingLists.Add(mapping.Key, mapping.Value.ToList());
                }
            }
        }
    }
    public abstract class BindingModuleBase : IBindingModule
    {
        private IViewModelResolver resolver;
        private IReflectionHelper helper;
   
        public virtual IModelBinding<T> BindModel<T>()
        {
            return new ModelBinding<T>(this, resolver, helper);
        }

        public abstract void Load();

        public void OnLoad(IViewModelResolver resolver, IReflectionHelper helper)
        {
            if (resolver == null) throw new ArgumentNullException("resolver");
            if (helper == null) throw new ArgumentNullException("helper");
            this.resolver = resolver;
            this.helper = helper;
            ModelMappings = new Dictionary<Type, IList<IPropertyMapping>>();
        }

        public IDictionary<Type, IList<IPropertyMapping>> ModelMappings
        {
            get;
            private set;
        }
    }

    public class ModelBinding<TModel> : IModelBinding<TModel>// where TModel : class
    {
        protected readonly IBindingModule module;
        protected readonly IViewModelResolver resolver;
        protected readonly IReflectionHelper helper;
        public ModelBinding(IBindingModule module, IViewModelResolver resolver, IReflectionHelper helper)
        {
            if (module == null) throw new ArgumentNullException("module");
            if (resolver == null) throw new ArgumentNullException("resolver");
            if (helper == null) throw new ArgumentNullException("helper");
            this.module = module;
            this.resolver = resolver;
            this.helper = helper;
        }
        public virtual IPropertyBinding<TModel, TProp> FromProperty<TProp>(Expression<Func<TModel, TProp>> propertyLambda)
        {
            var propInfo = helper.GetPropertyInfo<TModel, TProp>(propertyLambda);
            var result = new PropertyBinding<TModel, TProp>(propInfo, module, resolver);
            return result;
        }
    }
    public class PropertyBinding<TModel, TProp> : IPropertyBinding<TModel, TProp>// where TModel : class
    {
        protected readonly IBindingModule module;
        protected readonly PropertyInfo propInfo;
        protected readonly IViewModelResolver resolver;
        protected readonly Type modelType;
        public PropertyBinding(PropertyInfo propInfo, IBindingModule module, IViewModelResolver resolver)
        {
            if (propInfo == null) throw new ArgumentNullException("propInfo");
            if (module == null) throw new ArgumentNullException("module");
            if (resolver == null) throw new ArgumentNullException("resolver");
            this.module = module;
            this.propInfo = propInfo;
            this.resolver = resolver;
            modelType = typeof(TModel);
        }
        #region IPropertyBinding

        public virtual IAttributeBinding<TProp, TAttribute> ToAttribute<TAttribute>(params object[] ctorArguments) where TAttribute : IPropertyAttribute
        {
            TAttribute attribute = (TAttribute)resolver.ResolveInstance<TAttribute>(ctorArguments); //Should we really allow ctor args? This is meant for Domain models that don't have any, but it also removes flexibility to not allow them
            Type mappingType;
            Type temp;
            Type propType = typeof(TProp);
            //REQUIREMENT: Multi value must be either ICollection<T> or T[] where T is the type for the mapping
            if (resolver.ReflectionHelper.IsArray(propType, out temp))
            {
                mappingType = temp;
            }
            else if (resolver.ReflectionHelper.IsGenericCollection(propType, out temp))
            {
                mappingType = temp;
            }
            else
                mappingType = propType;
            
            ////old: Multi-values must implement ICollection<>, not just IEnumerable<> (this lets us use the Add method)
            //if (typeof(TProp).IsAssignableFrom(typeof(ICollection<>))) //watch out for performance here, do this ONCE
            //    mappingType = typeof(TProp).GetGenericArguments()[0];
            //else
            //    mappingType = typeof(TProp);

            Action<IPropertyAttribute, IBindingContainer> deferredMapping =
                (IPropertyAttribute attr, IBindingContainer container) => attr.ComplexTypeMapping = container.GetMapping(mappingType);
            IPropertyMapping propMapping = new PropertyMapping(propInfo, attribute, deferredMapping);
            var mapping = GetMapping();
            if (mapping != null) mapping.Add(propMapping);
            return new AttributeBinding<TProp, TAttribute>(propMapping, attribute);
        }

        #endregion
        private IList<IPropertyMapping> GetMapping()
        {
            IList<IPropertyMapping> result = null;
            if (module.ModelMappings != null)
            {
                if (module.ModelMappings.ContainsKey(modelType))
                {
                    result = module.ModelMappings[modelType];
                }
                else
                {
                    result = new List<IPropertyMapping>();
                    module.ModelMappings.Add(modelType, result);
                }
            }
            return result;
        }

    }

    public class ModelMapping : IModelMapping
    {
        public ModelMapping(Type modelType, IList<IModelProperty> modelProperties)
        {
            ModelType = modelType;
            ModelProperties = modelProperties;
        }
        public Type ModelType
        {
            get;
            private set;
        }

        public IList<IModelProperty> ModelProperties
        {
            get;
            private set;
        }
    }

    public class PropertyMapping : IPropertyMapping
    {
        public PropertyMapping(PropertyInfo property, IPropertyAttribute propertyAttribute, Action<IPropertyAttribute, IBindingContainer> getMapping)
        {
            Property = property;
            PropertyAttribute = propertyAttribute;
            GetMapping = getMapping;
        }

        public PropertyInfo Property
        {
            get;
            private set;
        }

        public Action<IPropertyAttribute, IBindingContainer> GetMapping
        {
            get;
            set;
        }

        public IPropertyAttribute PropertyAttribute
        {
            get;
            private set;
        }
    }

    public class AttributeBinding<TProp, TAttribute> : IAttributeBinding<TProp, TAttribute> where TAttribute : IPropertyAttribute
    {
        IPropertyMapping mapping;
        TAttribute attribute;
        public AttributeBinding(IPropertyMapping mapping, TAttribute attribute)
        {
            if (mapping == null) throw new ArgumentNullException("mapping");
            if (attribute == null) throw new ArgumentNullException("attribute");
            this.mapping = mapping;
            this.attribute = attribute;
        }
        public virtual void With(Action<TAttribute> action)
        {
            action(attribute);
        }

        //public void WithMethod(Action<TAttribute, IBindingContainer> action)
        //{
        //    //Any reason to allow for this?
        //    mapping.GetMapping = (IPropertyAttribute attr, IBindingContainer container) =>
        //        {
        //            action((TAttribute)attr, container);
        //        };

        //}
    }

    //public class DefaultResolver : ITypeResolver
    //{
    //    private readonly IReflectionHelper helper;
    //    public DefaultResolver(IReflectionHelper helper)
    //    {
    //        this.helper = helper;
    //    }

    //    public T ResolveInstance<T>(params object[] ctorArgs)
    //    {
    //        return (T)helper.CreateInstance(typeof(T)); //This will bomb if it expected ctor args or if it has no constructor
    //    }

    //    public IModelProperty GetModelProperty(PropertyInfo propertyInfo, IPropertyAttribute attribute)
    //    {
    //        if (propertyInfo == null) throw new ArgumentNullException("propertyInfo");
    //        //if (attribute == null) throw new ArgumentNullException("attribute");
    //        var generics = propertyInfo.PropertyType.GetGenericArguments();
    //        bool isMultiValue = false;
    //        if (generics.Length > 0)
    //        {
    //            var genericType = generics[0];
    //            if (typeof(ICollection<>).MakeGenericType(genericType).IsAssignableFrom(propertyInfo.PropertyType))
    //            {
    //                isMultiValue = true;
    //            }
    //        }
    //        return new ModelProperty
    //        {
    //            Name = propertyInfo.Name,
    //            PropertyAttribute = attribute,
    //            Set = helper.BuildSetter(propertyInfo),
    //            Get = helper.BuildGetter(propertyInfo),
    //            PropertyType = propertyInfo.PropertyType,
    //            IsMultiValue = isMultiValue
    //        };
    //    }
    //}

    public class DefaultModelMapping : IModelMapping
    {
        private readonly IList<IModelProperty> propertyList = new List<IModelProperty>();
        private readonly IViewModelResolver resolver;
        private readonly IReflectionHelper helper;
        public DefaultModelMapping(IViewModelResolver resolver, IReflectionHelper helper, Type modelType)
        {
            if (helper == null) throw new ArgumentNullException("helper");
            if (resolver == null) throw new ArgumentNullException("resolver");
            if (modelType == null) throw new ArgumentNullException("modelType");
            this.helper = helper;
            this.resolver = resolver;
            this.ModelType = modelType;
        }

        public Type ModelType
        {
            get;
            private set;
        }

        IList<IModelProperty> IModelMapping.ModelProperties
        {
            get { return propertyList; }
        }
    }

    public class ContextModel : IContextModel
    {
        TcmUri _pageId = TcmUri.NullUri;
        public TcmUri PageId 
        { 
            get { return _pageId; }
            set { _pageId = value; }
        }
    }
        
}
