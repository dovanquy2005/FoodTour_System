'use strict';

let _map = null;
let _heat = null;

function toLatLngs(points) {
    var arr;
    if (Array.isArray(points)) {
        arr = points;
    } else if (points !== null && typeof points === 'object' && typeof points.lat !== 'undefined') {
        arr = [points];
    } else {
        arr = Object.values(points);
    }

    return arr
        .filter(function(p) {
            return p
                && typeof p.lat === 'number' && !isNaN(p.lat)
                && typeof p.lng === 'number' && !isNaN(p.lng)
                && p.lat !== 0 && p.lng !== 0;
        })
        .map(function(p) { return [p.lat, p.lng, p.weight || 1]; });
}

function getMaxWeight(points) {
    var arr = Array.isArray(points) ? points : [points];
    return arr.reduce(function(m, p) { return Math.max(m, p.weight || 1); }, 1);
}

window.heatmap = {
    init: function (containerId, points, centerLat, centerLng, zoom) {
        if (_map) {
            _map.remove();
            _map = null;
            _heat = null;
        }

        _map = L.map(containerId, { zoomControl: true })
                .setView([centerLat, centerLng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap contributors'
        }).addTo(_map);

        var latlngs = toLatLngs(points);
        var maxW = getMaxWeight(Array.isArray(points) ? points : [points]);

        _heat = L.heatLayer(latlngs, {
            radius: 50,
            blur: 35,
            max: maxW,
            minOpacity: 0.5,        // ← luôn hiện dù zoom out
            gradient: { 0.4: 'blue', 0.65: 'lime', 1: 'red' }
        }).addTo(_map);

        window._debugHeat = _heat;
        window._debugMap = _map;

        setTimeout(() => { _map.invalidateSize(); }, 200);
    },

    update: function (points) {
        console.log("heatmap.update called, points:", points);
        if (!_map || !_heat) return;

        var latlngs = toLatLngs(points);
        var arr = Array.isArray(points) ? points : [points];
        var maxW = getMaxWeight(arr);

        _heat.setLatLngs(latlngs);

        // Update max dynamically so color scale stays correct
        _heat.setOptions({ max: maxW });
        _heat.redraw();
    }
};