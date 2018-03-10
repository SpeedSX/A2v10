﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using A2v10.Infrastructure;
using System;

namespace A2v10.Xaml
{
	public enum PaneStyle
	{
		Default,
		Info,
		Warning,
		Danger,
		Error,
		Success,
		Green,
		Cyan,
		Red,
		Yellow
	}

	public enum PaneBackgroundStyle
	{
		None,
		LightGray
	}


	public class Panel : Container, ITableControl
	{
		public Object Header { get; set; }

		public Boolean Collapsible { get; set; }

		public Boolean? Collapsed { get; set; }

		public PaneStyle Style { get; set; }

		public Icon Icon { get; set; }

		public PaneBackgroundStyle Background { get; set; }

		public Boolean DropShadow { get; set; }

		internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
		{
			var panel = new TagBuilder("a2-panel", null, IsInGrid);
			MergeBindingAttributeBool(panel, context, ":initial-collapsed", nameof(Collapsed), Collapsed);
			MergeBindingAttributeBool(panel, context, ":collapsible", nameof(Collapsible), Collapsible);
			if (!HasHeader)
				panel.MergeAttribute(":no-header", "true");
			var sb = GetBinding(nameof(Style));
			if (sb != null)
				panel.MergeAttribute(":panel-style", sb.GetPathFormat(context));
			else if (Style != PaneStyle.Default)
				panel.MergeAttribute("panel-style", Style.ToString().ToLowerInvariant());
			MergeAttributes(panel, context, MergeAttrMode.Visibility);
			if (Background != PaneBackgroundStyle.None)
				panel.AddCssClass("panel-background-" + Background.ToString().ToKebabCase());
			panel.AddCssClassBool(DropShadow, "drop-shadow");
			panel.RenderStart(context);
			RenderHeader(context);
			var content = new TagBuilder("div", "panel-content");
			MergeAttributes(content, context, MergeAttrMode.Margin | MergeAttrMode.Wrap | MergeAttrMode.Tip);
			content.RenderStart(context);
			RenderChildren(context);
			content.RenderEnd(context);
			panel.RenderEnd(context);
		}

		Boolean HasHeader => GetBinding(nameof(Header)) != null || Header != null || Icon != Icon.NoIcon;

		void RenderHeader(RenderContext context)
		{
			if (!HasHeader)
				return;
			var header = new TagBuilder("div", "panel-header-slot");
			header.MergeAttribute("slot", "header");
			header.RenderStart(context);

			RenderIcon(context, Icon);

			var hBind = GetBinding(nameof(Header));
			if (hBind != null)
			{
				var span = new TagBuilder("span");
				span.MergeAttribute("v-text", hBind.GetPathFormat(context));
				span.Render(context);
			}
			else if (Header is UIElementBase)
			{
				(Header as UIElementBase).RenderElement(context);
			}
			else if (Header != null)
			{
				context.Writer.Write(context.Localize(Header.ToString()));
			}
			header.RenderEnd(context);
		}
	}
}
