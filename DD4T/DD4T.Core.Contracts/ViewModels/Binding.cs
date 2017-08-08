using DD4T.ContentModel;
using DD4T.Core.Contracts.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DD4T.Core.Contracts.ViewModels.Binding
{
     //The intention of this is to allow for the following:
     //BindModel<MyModel>()
     //   .FromProperty(x => x.MyProperty)
     //   .ToAttribute<MyFieldAttribute>()
     //   .With(attr => 
     //       {
     //           attr.AllowMultipleValues = true;
     //           attr.IsMetadata = false;
     //           attr.InlineEditable = true;
     //       });

    public interface IMappedModelFactory
    {
        //void AddModelMapping<T>(IModelMapping<T> mapping); //Doesn't seem possible to store a collection of different generics without making the whole Factory generic
        //T BuildMappedModel<T>(IModel modelData) where T : class, new();
        T BuildMappedModel<T>(T model, IModel modelData, IModelMapping mapping); //where T : class;
        T BuildMappedModel<T>(IModel modelData, IModelMapping mapping); //where T : class;
        object BuildMappedModel(IModel modelData, IModelMapping mapping);
    }
    public interface IModelMapping
    {
        Type ModelType { get; }
        IList<IModelProperty> ModelProperties { get; }
    }
    //public interface ITypeResolver
    //{
    //    T ResolveInstance<T>(params object[] ctorArgs);
    //    IModelProperty GetModelProperty(PropertyInfo propertyInfo, IPropertyAttribute attribute); //This method makes no sense here
    //}
    public interface IPropertyMapping
    {
        PropertyInfo Property { get; }
        Action<IPropertyAttribute, IBindingContainer> GetMapping { get; set; }
        IPropertyAttribute PropertyAttribute { get; }
    }
    public interface IModelBinding<TModel>// where TModel : class
    {
        IPropertyBinding<TModel, TProp> FromProperty<TProp>(Expression<Func<TModel, TProp>> propertyLambda);
    }
    public interface IPropertyBinding<TModel, TProp>// where TModel : class
    {
        //void ToAttribute(IPropertyAttribute propertyAttribute);
        //void ToMethod(Func<IBindingContainer, IPropertyAttribute> attributeMethod);
        IAttributeBinding<TProp, TAttribute> ToAttribute<TAttribute>(params object[] ctorArguments) where TAttribute : IPropertyAttribute;
    }
    public interface IAttributeBinding<TProp, TAttribute> where TAttribute : IPropertyAttribute
    {
        void With(Action<TAttribute> action);
        //void WithMethod(Action<TAttribute, IBindingContainer> action); //Is this necessary? What are the use cases?
    }
    public interface IBindingContainer
    {
        IModelMapping GetMapping<T>(); // where T : class;
        IModelMapping GetMapping(Type type);
    }
    public interface IBindingModule //Responsible for loading bindings and turning them into useful model mapping
    {
        IModelBinding<T> BindModel<T>();
        void Load();
        void OnLoad(IViewModelResolver resolver, IReflectionHelper helper);
        IDictionary<Type, IList<IPropertyMapping>> ModelMappings { get; }
        //void AddBinding(); //Not supposed to pass binding -- what should be passed?
    }
   

}
