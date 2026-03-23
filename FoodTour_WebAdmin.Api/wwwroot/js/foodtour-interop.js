/**
 * FoodTour QR Code Interop — called from Blazor via JSInterop
 * Depends on: qrcode.min.js (loaded before this file)
 */

window.generateQrCode = function (elementId, content, size) {
    var container = document.getElementById(elementId);
    if (!container) return;

    // Guard: nếu QRCode chưa load xong thì retry sau 200ms
    if (typeof QRCode === 'undefined') {
        console.warn('[FoodTour] QRCode library not loaded yet, retrying...');
        setTimeout(function () { window.generateQrCode(elementId, content, size); }, 200);
        return;
    }

    container.innerHTML = '';
    var canvas = document.createElement('canvas');
    container.appendChild(canvas);
    QRCode.toCanvas(canvas, content, {
        width: size,
        margin: 2,
        color: { dark: '#2D1F14', light: '#FFFFFF' }
    });
};

window.downloadQrCode = function (elementId, fileName) {
    var container = document.getElementById(elementId);
    if (!container) return;
    var canvas = container.querySelector('canvas');
    if (!canvas) return;
    var link = document.createElement('a');
    link.download = fileName;
    link.href = canvas.toDataURL('image/png');
    link.click();
};

window.printQrCodes = function () {
    window.print();
};

window.copyToClipboard = function (text) {
    return navigator.clipboard.writeText(text);
};
