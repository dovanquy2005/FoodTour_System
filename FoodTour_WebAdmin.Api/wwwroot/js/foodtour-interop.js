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

// ═══════ Landing Page Interop ═══════

window.getBrowserLanguage = function () {
    return navigator.language || navigator.userLanguage || 'en';
};

window.playAudio = function (elementId) {
    var audio = document.getElementById(elementId);
    if (audio) {
        audio.currentTime = 0;
        audio.play();
    }
};

window.pauseAudio = function (elementId) {
    var audio = document.getElementById(elementId);
    if (audio) audio.pause();
};

// ═══════ AUTO-PLAY THÔNG MINH (Xử lý Browser Autoplay Policy) ═══════
// Trình duyệt di động thường chặn audio.play() nếu chưa có tương tác người dùng.
// Hàm này thử phát ngay → nếu bị chặn, gắn listener "chạm 1 lần" để phát ẩn khi user lỡ chạm màn hình.
window.playAudioAuto = function (audioElementId) {
    var audio = document.getElementById(audioElementId);
    if (!audio) {
        console.warn('[FoodTour] Không tìm thấy audio element:', audioElementId);
        return;
    }

    audio.currentTime = 0;

    // Thử phát trực tiếp (hầu hết sẽ thành công nếu user đã tương tác trước đó)
    var playPromise = audio.play();

    if (playPromise !== undefined) {
        playPromise
            .then(function () {
                console.log('[FoodTour] Auto-play thành công!');
            })
            .catch(function (err) {
                console.warn('[FoodTour] Autoplay bị chặn:', err.name, '- Đang chờ tương tác người dùng...');

                // Gỡ bỏ listener cũ (tránh duplicate) rồi gắn listener mới
                if (window._ftAutoplayHandler) {
                    document.body.removeEventListener('touchstart', window._ftAutoplayHandler);
                    document.body.removeEventListener('click', window._ftAutoplayHandler);
                }

                // Tạo handler phát audio ngay khi user chạm/click vào bất cứ đâu (dùng 1 lần)
                window._ftAutoplayHandler = function () {
                    var a = document.getElementById(audioElementId);
                    if (a) {
                        a.play().then(function () {
                            console.log('[FoodTour] Audio đã phát sau tương tác người dùng!');
                        }).catch(function () { });
                    }
                    // Tự gỡ bỏ listener sau khi đã kích hoạt
                    document.body.removeEventListener('touchstart', window._ftAutoplayHandler);
                    document.body.removeEventListener('click', window._ftAutoplayHandler);
                    window._ftAutoplayHandler = null;
                };

                // Gắn cả touchstart (mobile) và click (desktop) với { once: false }
                // Dùng manual removeEventListener thay vì once: true để kiểm soát chặt hơn
                document.body.addEventListener('touchstart', window._ftAutoplayHandler, { passive: true });
                document.body.addEventListener('click', window._ftAutoplayHandler);
            });
    }
};

// ═══════ TẢI LẠI AUDIO VÀ TỰ ĐỘNG PHÁT (khi đổi ngôn ngữ) ═══════
// Buộc thẻ <audio> load lại source mới rồi gọi autoplay
window.reloadAndPlayAudio = function (audioElementId) {
    var audio = document.getElementById(audioElementId);
    if (!audio) return;

    // Dừng audio cũ, ép load lại source mới từ DOM
    audio.pause();
    audio.load();

    // Chờ audio sẵn sàng rồi phát tự động
    audio.addEventListener('canplaythrough', function onReady() {
        audio.removeEventListener('canplaythrough', onReady);
        window.playAudioAuto(audioElementId);
    }, { once: true });
};

// ═══════ HỖ TRỢ TẢI ẢNH TỪ BASE64 ═══════
window.downloadBase64File = function (base64String, fileName) {
    var a = document.createElement("a");
    a.href = base64String;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};

// ═══════ HÀM HỖ TRỢ: LẤY MÃ VÂN TAY TRÌNH DUYỆT ═══════
window.getBrowserFingerprint = async function () {
    try {
        // Load động thư viện FingerprintJS bản mã nguồn mở
        const fpPromise = import('https://openfpcdn.io/fingerprintjs/v4')
            .then(FingerprintJS => FingerprintJS.load());

        const fp = await fpPromise;
        const result = await fp.get();
        return result.visitorId; // Trả về chuỗi ID duy nhất (VD: "9b1deb4d-3b7d-...")
    } catch (error) {
        console.error('[FoodTour] Lỗi khi tạo Fingerprint, dùng fallback:', error);
        // Fallback tự tạo một chuỗi ngẫu nhiên lưu vào localStorage nếu tải thư viện xịt
        let fallbackId = localStorage.getItem('foodtour_fallback_id');
        if (!fallbackId) {
            fallbackId = 'fb_' + Math.random().toString(36).substring(2);
            localStorage.setItem('foodtour_fallback_id', fallbackId);
        }
        return fallbackId;
    }
};

// ═══════ SERVER-SIDE TRACKING TRIAL (BẢN MỚI) ═══════
window.recordServerTrial = async function (shopId) {
    try {
        // 1. Lấy mã Fingerprint của thiết bị
        const deviceFingerprint = await window.getBrowserFingerprint();

        console.log(`[FoodTour] Đang gửi lượt nghe với Fingerprint: ${deviceFingerprint}`);

        // 2. Gọi API mới (Gửi dữ liệu qua JSON Body)
        const response = await fetch(`/api/trial/record`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json', // Bắt buộc phải có để báo cho C# biết đây là JSON
                'Accept': 'application/json'
            },
            body: JSON.stringify({
                fingerprint: deviceFingerprint,
                shopId: shopId
            })
        });

        if (response.ok) {
            const data = await response.json();
            return data.success === true;
        } else if (response.status === 403) {
            console.warn('[FoodTour] Đã hết 3 lượt nghe miễn phí!');
            return false; // Limit reached
        } else {
            console.error('[FoodTour] API trả về lỗi:', response.status);
        }
    } catch (err) {
        console.error('[FoodTour] Lỗi gọi API server tracking:', err);
    }

    return false; // Chặn nếu có lỗi mạng hoặc lỗi ngầm định
};

// ═══════ GHI LOG AUDIO ACTIVITY (Tracking) ═══════
// Gọi API /api/audiologs/record để ghi nhận mỗi lần du khách phát audio trên web
window.recordAudioLog = async function (shopId, languageCode, shopItemId) {
    try {
        const fingerprint = await window.getBrowserFingerprint();

        const body = {
            deviceId: fingerprint,
            shopId: shopId,
            languageCode: languageCode || 'vi',
            browserFingerprint: fingerprint,
            source: 'Web'
        };

        if (shopItemId) {
            body.shopItemId = shopItemId;
        }

        fetch('/api/audiologs/record', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(body)
        }).then(res => {
            if (res.ok) {
                console.log('[FoodTour] Audio log ghi thành công.');
            } else {
                console.warn('[FoodTour] Audio log lỗi:', res.status);
            }
        }).catch(err => {
            console.warn('[FoodTour] Audio log fetch error:', err);
        });
    } catch (err) {
        console.warn('[FoodTour] recordAudioLog error:', err);
    }
};
