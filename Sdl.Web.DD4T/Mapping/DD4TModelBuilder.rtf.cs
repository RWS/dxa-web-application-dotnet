using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DD4T.ContentModel;
using DD4T.ContentModel.Factories;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;
using SDL.Web.Helpers;

namespace Sdl.Web.DD4T.Mapping
{
    partial class DD4TModelBuilder
    {
        //readonly RichTextHelper RichTextHelper = new RichTextHelper()
        class RichTextHelperFac
        {
            RichTextHelper value;

            RichTextHelper InitializeValue()
            {
                return default(RichTextHelper);// new RichTextHelper(ILinkFactory linkFactory, IComponentFactory componentFactory);
            }

            public RichTextHelper Value
            {
                get { return value ?? (value = InitializeValue()); }
            }
        }
    }
}
