﻿/*invoice index template*/

const utils = require('std:utils');
const du = utils.date;

const template = {
	options: {
		persistSelect: ['Documents'],
	},
	properties: {
		'TRoot.$Combo': String,
		'TRoot.$ComboId': Number,
		'TRoot.$ForCreate'() { return { AgentId: 11 }; },
		'TRoot.$tabIndex': Number,
		'TDocument.$Mark': mark,
		'TAgent.$Mark': agentMark,
		'TDocument.$Icon'() { return this.Done ? 'flag-green' : ''; },
		'TDocument.$Color'() { return this.Sum > 100 ? 'red' : 'green'; },
		'TDocument.$Shipment': getShipment,
		'TDocument.$HasDetails'() { return this.Links.Count > 0; },
		'TDocLink.$DocName': linkDocName,
		"TDocument.$State"() { return this.Done ? 'Проведен' : ''; },
		"TDocument.$StateStyle"() { return this.Done ? 'info' : 'hidden'; }
	},
	events: {
		'GlobalPeriod.change': globalPeriodChange
	},
	delegates: {
		uploadCsv
	},
	commands: {
		doubleClick
	}
};

function globalPeriodChange(root, period) {
	//console.dir(period);
	root.$ctrl.$setFilter(root.Documents, 'Period', period);
}

function mark() {
	return this.Done ? "success" : 'warning';
}

function agentMark() {
	return this.Id === 0 ? 'red' : undefined;
}

function getShipment() {
	return this.Links.filter(doc => doc.Kind === 'Waybill');
}

function linkDocName() {
	return `№ ${this.No} от ${du.formatDate(this.Date)} на сумму ${utils.format(this.Sum, 'Currency')} грн.`;
}

module.exports = template;

function uploadCsv(x) {
	console.dir(x);
}
function doubleClick(doc) {
	alert('double click: ' + doc.Id);
}