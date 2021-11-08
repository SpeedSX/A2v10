﻿
/* index template */

const du = require('std:utils').date;

const template = {
    properties: {
        'TRoot.$style': String,
		'TRoot.$text'() {
			return 'button text';
		},
		'TRoot.$InlineDepth'() { console.dir(this.$vm); return this.$vm ? this.$vm.$inlineDepth() : 0; },
		'TRoot.$Arg142'() { return { Id: 142 };}
	},
	methods: {
	},
	delegates: {
		drawChart,
		onUpload
	},
	events: {
	},
	validators: {
	},
	commands: {
		showAlert() {
			this.$ctrl.$inlineOpen('Service');

		},
		showNested() {
			this.$ctrl.$inlineOpen('Nested');
		},
		dlgCommand(arg) {
			alert('dlgCommand:' + arg);
		},
		javascript() {
			this.$ctrl.$navigate('/sales/invoice'); //, {Id: ''});
			/*
			let r = await this.$ctrl.$invoke('javascript', { Id: 77, StrVal: 'string value', DateVal: du.today(), NumVal: 55.32 });
			console.dir(r);
			alert(JSON.stringify(r));
			*/
		},
		async wait() {
			let r = await this.$ctrl.$invoke('wait');
		},
		fetchdbevents() {
			this.$ctrl.$invoke('fetchDbEvents');
		},
		testDepth() {
			alert(this.$ctrl.$inlineDepth());
		}
	}
};

module.exports = template;

function drawChart(g, arg, elem) {
	console.dir(elem);
	const chart = g.append('svg')
		.attr('viewBox', `0 0 300 50`);

	const sine = d3.range(0, 50)
		.map(k => {
			let x = .5 * k * Math.PI;
			return [x, Math.sin(x / 4)];
		});

	// scale functions
	const x = d3.scaleLinear()
		.range([0, 300])
		.domain(d3.extent(sine, d => d[0]));

	const y = d3.scaleLinear()
		.range([50, 0])
		.domain([-1, 1]);

	// line generator
	const area = d3.area()
		.x(d => x(d[0]))
		.y0(50)
		.y1(d => y(d[1]));

	chart.append('g')
		.append('path')
		.datum(sine)
		.attr('fill', "hsla(0,0%,70%,.25)")
		.attr('d', area);
		//.attr('stroke', '#fff')
}

function onUpload(result) {
	console.dir(result);
}