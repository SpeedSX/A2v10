﻿// Copyright © 2015-2022 Alex Kukhtin. All rights reserved.

// 20200111-7850
// components/taskpad.js

Vue.component("a2-taskpad", {
	template:
		`<div :class="cssClass">
	<a class="ico taskpad-collapse-handle" @click.stop="toggle"></a>
	<div v-if="expanded" class="taskpad-body">
		<slot>
		</slot>
	</div>
	<div v-else class="taskpad-title" @click.prevent="toggle">
		<span class="taskpad-label" v-text="tasksText"></span>
	</div>
</div>
`,
	props: {
		title: String,
		initialCollapsed: Boolean,
		position: String
	},
	data() {
		return {
			expanded: true,
			__savedCols: ''
		};
	},
	computed: {
		cssClass() {
			let cls = "taskpad";
			cls += ' position-' + this.position;
			if (this.expanded) cls += ' expanded'; else cls += ' collapsed';

			return cls;
		},
		tasksText() {
			return this.title || window.$$locale.$Tasks;
		}
	},
	methods: {
		setExpanded(exp) {
			this.expanded = exp;
			// HACK
			let topStyle = this.$el.parentElement.style;
			if (this.expanded)
				topStyle.gridTemplateColumns = this.__savedCols;
			else {
				if (this.position === 'left')
					topStyle.gridTemplateColumns = "36px 1fr"; // TODO: ???
				else
					topStyle.gridTemplateColumns = "1fr 36px"; // TODO: ???
			}
		},
		toggle() {
			this.setExpanded(!this.expanded);
		}
	},
	mounted() {
		let topStyle = this.$el.parentElement.style;
		this.__savedCols = topStyle.gridTemplateColumns;
		if (this.initialCollapsed)
			this.setExpanded(false);
	}
});

