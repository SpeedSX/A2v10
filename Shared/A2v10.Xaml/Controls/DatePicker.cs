﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.


using System;

namespace A2v10.Xaml
{

	public enum DatePickerDropDownDirection
	{
		Down,
		Up
	}

	public class DatePicker : ValuedControl, ITableControl
	{

		public TextAlign Align { get; set; }

		public DatePickerDropDownDirection Direction { get; set; }

		internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
		{
			CheckDisabledModel(context);
			var input = new TagBuilder("a2-date-picker", null, IsInGrid);
			if (onRender != null)
				onRender(input);
			MergeAttributes(input, context);
			MergeDisabled(input, context);
			MergeAlign(input, context, Align);
			if (Direction != DatePickerDropDownDirection.Down)
				input.AddCssClass("drop-" + Direction.ToString().ToLowerInvariant());
			MergeValue(input, context);
			input.RenderStart(context);
			RenderAddOns(context);
			input.RenderEnd(context);
		}

		protected override void OnEndInit()
		{
			base.OnEndInit();
			if (Align == TextAlign.Default)
				Align = TextAlign.Center;
		}
	}
}
