using DD4T.ContentModel;
using Sdl.Web.Mvc.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.DD4T.Extensions
{
    public static class ExtensionMethods
    {
        public static void SetProperty(this object model, IField field)
        {
            if (field.Values.Count > 0)
            {
                PropertyInfo pi = model.GetType().GetProperty(field.Name.Substring(0, 1).ToUpper() + field.Name.Substring(1));
                if (pi != null)
                {
                    //TODO check/cast to the type we are mapping to 
                    bool multival = pi.PropertyType is IEnumerable;
                    switch (field.FieldType)
                    {
                        case (FieldType.Date):
                            pi.SetValue(model, GetDates(field,pi.PropertyType,multival));
                            break;
                        case (FieldType.Number):
                            pi.SetValue(model, GetNumbers(field, pi.PropertyType, multival));
                            break;
                        case (FieldType.MultiMediaLink):
                            pi.SetValue(model, GetMultiMediaLinks(field, pi.PropertyType, multival));
                            break;
                        default:
                            pi.SetValue(model, GetStrings(field, pi.PropertyType, multival));
                            break;
                    }
                }
            }
        }

        private static object GetDates(IField field, Type modelType, bool multival)
        {
            if (typeof(DateTime).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return field.DateTimeValues;
                }
                else
                {
                    return field.DateTimeValues[0];
                }
            }
            return null;
        }

        private static object GetNumbers(IField field, Type modelType, bool multival)
        {
            if (typeof(Double).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return field.NumericValues;
                }
                else
                {
                    return field.NumericValues[0];
                }
            }
            return null;
        }

        private static object GetMultiMediaLinks(IField field, Type modelType, bool multival)
        {
            if (typeof(Image).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return GetImages(field.LinkedComponentValues);
                }
                else
                {
                    return GetImages(field.LinkedComponentValues)[0];
                }
            }
            return null;
        }

        private static object GetStrings(IField field, Type modelType, bool multival)
        {
            if (typeof(String).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return field.Values;
                }
                else
                {
                    return field.Value;
                }
            }
            return null;
        }

        private static List<Image> GetImages(IList<IComponent> components)
        {
            return components.Select(c => new Image { Url = c.Multimedia.Url, Id = c.Id, FileSize = c.Multimedia.Size }).ToList();
        }

    }
}
