﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace A2v10.Xaml
{
    public class PropertyGridItems : List<PropertyGridItem>
    {
        internal void Render(RenderContext context)
        {
            foreach (var itm in this)
                itm.RenderElement(context);
        }
    }

    [ContentProperty("Content")]
    public class PropertyGridItem : UIElementBase
    {
        public String Name { get; set; }
        public Object Content { get; set; }

        public Boolean? Bold { get; set; }

        internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
        {
            var tr = new TagBuilder("tr");
            if (onRender != null)
                onRender(tr);
            MergeAttributes(tr, context);
            tr.RenderStart(context);

            var nameCell = new TagBuilder("td", "prop-name");
            var nameBind = GetBinding(nameof(Name));
            if (nameBind != null)
                nameCell.MergeAttribute("v-text", nameBind.GetPathFormat(context));
            nameCell.RenderStart(context);
            if (!String.IsNullOrEmpty(Name))
                context.Writer.Write(Name);
            nameCell.RenderEnd(context);

            var valCell = new TagBuilder("td", "prop-value");
            if (Bold != null)
                valCell.AddCssClass(Bold.Value ? "bold" : "no-bold");
            var contBind = GetBinding(nameof(Content));
            if (contBind != null)
                valCell.MergeAttribute("v-text", contBind.GetPathFormat(context));
            valCell.RenderStart(context);
            if (Content is UIElementBase)
                (Content as UIElementBase).RenderElement(context);
            else if (Content != null)
                context.Writer.Write(Content.ToString());
            valCell.RenderEnd(context);

            tr.RenderEnd(context);
        }

    }
}
