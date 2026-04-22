'use strict';

let _map = null;
let _heat = null;
let _shopMarkers = [];

function toLatLngs(points) {
    if (!points) return [];
    // Đảm bảo luôn là mảng các object
    var arr;
    if (Array.isArray(points)) {
        // Nếu là mảng [10.7, 106.7, 1] thì phải bọc lại hoặc xử lý riêng
        if (typeof points[0] === 'number') {
            arr = [points]; // Biến thành [[10.7, 106.7, 1]]
        } else {
            arr = points;
        }
    } else {
        arr = [points]; // Nếu là 1 object đơn lẻ
    }
    // console.log("DEBUG: Array to process:", arr);
    return arr.map(function(p) {
        // Hỗ trợ cả định dạng object {lat, lng} và định dạng mảng [lat, lng, w]
        var lat = parseFloat(Array.isArray(p) ? p[0] : p.lat);
        var lng = parseFloat(Array.isArray(p) ? p[1] : p.lng);
        var w = parseFloat(Array.isArray(p) ? p[2] : (p.weight || 1));

        if (!isNaN(lat) && !isNaN(lng) && lat !== 0 && lng !== 0) {
            return [lat, lng, w];
        }
        return null;
    }).filter(function(p) { return p !== null; });
    // LOG 2: Kiểm tra dữ liệu sau khi lọc (4 điểm của bạn phải hiện ở đây)
    // console.log("DEBUG: Processed LatLngs for Heatmap:", result);
    return result;
}

function calcMax(latlngs) {
    if (!latlngs || latlngs.length === 0) return 1;
    return Math.max.apply(null, latlngs.map(function(p) { return p[2]; }));
}

window.heatmap = {
    init: function (containerId, points, centerLat, centerLng, zoom) {
        if (_map) {
            _map.remove();
            _map = null;
            _heat = null;
            _shopMarkers = [];
        }

        _map = L.map(containerId, { zoomControl: true })
                .setView([centerLat, centerLng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap contributors'
        }).addTo(_map);

        var latlngs = toLatLngs(points);
        var maxW = calcMax(latlngs);
        // LOG 3: Kiểm tra giá trị Max được tính toán
        // console.log("DEBUG: Heatmap Update - Points Count:", latlngs.length, "| Max Weight:", maxW);
        _heat = L.heatLayer(latlngs, {
            radius: 30,
            blur: 20,
            max: maxW,
            minOpacity: 0.5,
            gradient: { 0.0: 'blue', 0.5: 'lime', 1: 'red' }
        }).addTo(_map);

        window._debugHeat = _heat;
        window._debugMap = _map;

        setTimeout(function() {
            if (_map) _map.invalidateSize();
        }, 100);
    },

    update: function (points) {
        if (!_map || !_heat) return;

        var size = _map.getSize();
        if (size.x === 0 || size.y === 0) {
            setTimeout(function() { window.heatmap.update(points); }, 200);
            return;
        }

        var latlngs = toLatLngs(points);
        var maxW = calcMax(latlngs);

        _heat.setLatLngs(latlngs);
        _heat.setOptions({ max: maxW });
        try {
            _heat.redraw();
            console.log("DEBUG: Heatmap redraw executed successfully.");
        } catch (e) {
            console.error("Heatmap redraw failed:", e);
        }
    },

    addShopMarkers: function (shops) {
        if (!_map) return;

        _shopMarkers.forEach(function (m) { _map.removeLayer(m); });
        _shopMarkers = [];

        if (!shops || shops.length === 0) return;

        shops.forEach(function (shop) {
            if (!shop.lat || !shop.lng || shop.lat === 0 || shop.lng === 0) return;

            var popup = '<b style="color:#2D1F14;font-size:14px;">' + (shop.name || 'N/A') + '</b>' +
                        (shop.address ? '<br><small style="color:#8C7B6B;">' + shop.address + '</small>' : '');

            var marker = L.circleMarker([shop.lat, shop.lng], {
                radius: 8,
                fillColor: '#E8672A',
                color: '#ffffff',
                weight: 2.5,
                opacity: 1,
                fillOpacity: 0.95
            }).bindPopup(popup, {
                maxWidth: 220,
                closeButton: false
            });

            marker.on('mouseover', function () { this.openPopup(); });
            marker.on('mouseout', function () { this.closePopup(); });

            marker.addTo(_map);
            _shopMarkers.push(marker);
        });
    }
};