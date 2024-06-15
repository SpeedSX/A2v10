﻿// Copyright © 2015-2024 Oleksandr Kukhtin. All rights reserved.

// 20240120-7958
/* services/http.js */

app.modules['std:http'] = function () {

	const eventBus = require('std:eventBus');
	const urlTools = require('std:url');

	const httpQueue = {
		arr: [],
		processing: false
	};

	return {
		get,
		post,
		load,
		upload,
		localpost,
		queue
	};

	async function doRequest(method, url, data, raw, skipEvents) {
		if (!skipEvents)
			eventBus.$emit('beginRequest', url);
		let appver = '';
		try {
			var response = await fetch(url, {
				method,
				mode: 'same-origin',
				headers: {
					'X-Requested-With': 'XMLHttpRequest',
					'Accept': 'application/json, text/html'
				},
				body: data
			});
			let ct = response.headers.get("content-type") || '';
			appver = response.headers.get("app-version") || '';
			switch (response.status) {
				case 200:
					if (raw)
						return await response.blob();
					if (ct.startsWith('application/json'))
						return await response.json();
					return await response.text();
					break;
				case 255:
					let txt = response.statusText;
					if (ct.startsWith('text/'))
						txt = await response.text();
					throw txt;
				case 401: // Unauthorized
					setTimeout(() => {
						window.location.assign('/');
					}, 10);
					throw '__blank__';
					break;
				case 473: /*non standard */
					if ((response.statusText || (await response.text())) === 'Unauthorized') {
						// go to login page
						setTimeout(() => {
							window.location.assign('/');
						}, 10)
						throw '__blank__';
					}
					break;
				default:
					throw response.statusText;
			}
		}
		catch (err) {
			throw err;
		}
		finally {
			if (!skipEvents) {
				eventBus.$emit('endRequest', url);
				if (appver)
					eventBus.$emit('checkVersion', appver);
			}
		}
	}

	function get(url, raw) {
		return doRequest('GET', url, null, raw);
	}

	function post(url, data, raw, skipEvents) {
		return doRequest('POST', url, data, raw, skipEvents);
	}

	async function upload(url, data) {
		eventBus.$emit('beginRequest', url);
		try {
			var response = await fetch(url, {
				method: 'POST',
				headers: {
					'X-Requested-With': 'XMLHttpRequest',
					'Accept': 'application/json'
				},
				body: data
			});
			let ct = response.headers.get("content-type") || '';
			switch (response.status) {
				case 200:
					if (ct.startsWith('application/json'))
						return await response.json();
					return await response.text();
				case 255:
					let txt = response.statusText;
					if (ct.startsWith('text/'))
						txt = await response.text();
					throw txt;
			}

		} catch (err) {
			throw err;
		} finally {
			eventBus.$emit('endRequest', url);
		}
	}

	function load(url, selector, baseUrl, skipIndicator) {

		if (selector) {
			let fc = selector.firstElementChild
			if (fc && fc.__vue__) {
				let ve = fc.__vue__;
				ve.$destroy();
				ve.$el.remove();
				ve.$el = null;
				fc.__vue__ = null;
				selector.innerHTML = '';
			}
			selector.__loadedUrl__ = url;
		}

		return new Promise(function (resolve, reject) {
			eventBus.$emit('beginLoad');
			doRequest('GET', url, null, false, skipIndicator)
				.then(function (html) {
					if (!html)
						return;
					if (html.startsWith('<!DOCTYPE')) {
						// full page - may be login?
						window.location.assign('/');
						return;
					}

					let cu = selector ? selector.__loadedUrl__ : undefined;
					if (cu && cu !== url) {
						// foreign url
						eventBus.$emit('endLoad');
						return;
					}

					let dp = new DOMParser();
					let rdoc = dp.parseFromString(html, 'text/html');
					// first element from fragment body
					let srcElem = rdoc.body.firstElementChild;
					let elemId = srcElem.id || null;
					selector.innerHTML = srcElem ? srcElem.outerHTML : '';
					if (elemId && !document.getElementById(elemId)) {
						selector.innerHTML = '';
						resolve(false, null);
						return;
					}
					for (let i = 0; i < rdoc.scripts.length; i++) {
						let s = rdoc.scripts[i];
						if (s.type === 'text/javascript') {
							let newScript = document.createElement("script");
							if (s.src)
								newScript.src = s.src;
							else
								newScript.text = s.text;
							document.body.appendChild(newScript).parentNode.removeChild(newScript);
						}
					}

					let fec = selector.firstElementChild;
					let ve = null;
					if (fec && fec.__vue__) {
						ve = fec.__vue__;
						ve.$data.__baseUrl__ = baseUrl || urlTools.normalizeRoot(url);
						// save initial search
						ve.$data.__baseQuery__ = urlTools.parseUrlAndQuery(url).query;
						if (fec.classList.contains('modal')) {
							let dca = fec.getAttribute('data-controller-attr');
							if (dca)
								eventBus.$emit('modalSetAttribites', dca, ve);
						}
					}
					rdoc.body.remove();
					resolve(ve);
					eventBus.$emit('endLoad');
				})
				.catch(function (error) {
					if (error == '__blank__')
						return;
					reject(error);
					eventBus.$emit('endLoad');
				});
		});
	}

	async function localpost(command, data) {
		let url = "http://127.0.0.1:64031/" + command;
		let response = await fetch(url, {
			method: 'POST',
			headers: {
				'X-Requested-With': 'XMLHttpRequest',
				'Accept': 'text/plain',
				'Content-Type': 'text/plain'
			},
			body: data
		});

		if (response.status == 200) {
			let ct = response.headers.get("content-type") || '';
			if (ct.startsWith('application/json'))
				return await response.json();
			return await response.text();
		} else if (response.status == 255) {
			throw await response.text() || response.statusText;
		} else {
			throw response.statusText;
		}
	}

	function queue(url, selector) {
		httpQueue.arr.push({ url, selector });
		if (!httpQueue.processing)
			doQueue();
	}

	async function doQueue() {
		if (!httpQueue.arr.length)
			return;
		httpQueue.processing = true;
		while (httpQueue.arr.length > 0) {
			let el = httpQueue.arr.shift();
			await load(el.url, el.selector, null);
		}
		httpQueue.processing = false;
	}
};




