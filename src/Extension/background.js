// ═══════════════════════════════════════════════════════
// Cortex Speed Interceptor — Background Service Worker v4.0
// Uses HTTP localhost:19256 — simple, fast, reliable
// ═══════════════════════════════════════════════════════

const CORTEX_URL = 'http://localhost:19256';
let interceptEnabled = true;
let appIsRunning = false;

// Load saved preference
chrome.storage.local.get(['interceptEnabled'], (r) => {
    interceptEnabled = r.interceptEnabled !== undefined ? r.interceptEnabled : true;
});

chrome.storage.onChanged.addListener((changes, area) => {
    if (area === 'local' && changes.interceptEnabled) {
        interceptEnabled = changes.interceptEnabled.newValue;
        updatePopupStatus();
    }
});

// ─────────────────────────────────────────────────────────
// Check if Cortex Speed app is running (ping every 5s)
// ─────────────────────────────────────────────────────────
async function pingApp() {
    try {
        const resp = await fetch(`${CORTEX_URL}/ping`, { signal: AbortSignal.timeout(2000) });
        const wasRunning = appIsRunning;
        appIsRunning = resp.ok;
        if (!wasRunning && appIsRunning) {
            console.log('[CortexSpeed] App connected!');
            chrome.action.setBadgeText({ text: '' });
        }
    } catch {
        appIsRunning = false;
    }
    updatePopupStatus();
    return appIsRunning;
}

function updatePopupStatus() {
    chrome.storage.local.set({ appIsRunning });
}

// Ping immediately and then every 5 seconds
pingApp();
setInterval(pingApp, 5000);

// ─────────────────────────────────────────────────────────
// INTERCEPT METHOD 1: onDeterminingFilename
// Fires before Chrome shows save dialog — earliest hook
// ─────────────────────────────────────────────────────────
chrome.downloads.onDeterminingFilename.addListener((item, suggest) => {
    if (!interceptEnabled) { suggest(); return; }

    const url = item.finalUrl || item.url;
    if (!url || isInternalUrl(url)) { suggest(); return; }
    if (item.fileSize > 0 && item.fileSize < 51200) { suggest(); return; }

    console.log('[CortexSpeed] Intercepted (onDeterminingFilename):', url);

    // Cancel Chrome's download
    chrome.downloads.cancel(item.id, () => chrome.downloads.erase({ id: item.id }));

    // Send to app
    sendToApp(url, item.filename || '');

    // Do NOT call suggest() → Chrome pauses filename determination (download is already canceled)
});

// ─────────────────────────────────────────────────────────
// INTERCEPT METHOD 2: onCreated (backup)
// ─────────────────────────────────────────────────────────
chrome.downloads.onCreated.addListener((item) => {
    if (!interceptEnabled) return;
    const url = item.finalUrl || item.url;
    if (!url || isInternalUrl(url)) return;
    if (item.fileSize > 0 && item.fileSize < 51200) return;

    console.log('[CortexSpeed] Intercepted (onCreated):', url);
    chrome.downloads.cancel(item.id, () => chrome.downloads.erase({ id: item.id }));
    sendToApp(url, item.filename || '');
});

// ─────────────────────────────────────────────────────────
// INTERCEPT METHOD 3: Right-click context menu
// ─────────────────────────────────────────────────────────
chrome.runtime.onInstalled.addListener(() => {
    chrome.contextMenus.create({
        id: 'cs-link', title: '⚡ Download with Cortex Speed', contexts: ['link']
    });
});

chrome.contextMenus.onClicked.addListener((info) => {
    if (info.menuItemId === 'cs-link' && info.linkUrl) {
        sendToApp(info.linkUrl, '');
    }
});

// ─────────────────────────────────────────────────────────
// Send URL to Cortex Speed via HTTP POST to localhost
// ─────────────────────────────────────────────────────────
async function sendToApp(url, filename) {
    console.log('[CortexSpeed] Sending to app:', url);

    try {
        const resp = await fetch(`${CORTEX_URL}/download`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ url, filename }),
            signal: AbortSignal.timeout(5000)
        });

        if (resp.ok) {
            const data = await resp.json();
            console.log('[CortexSpeed] ✅ Download started:', data);
            appIsRunning = true;
            setBadge('✓', '#00D4AA', 3000);
        } else {
            console.error('[CortexSpeed] ❌ App error:', resp.status);
            setBadge('!', '#FF5252', 4000);
        }
    } catch (err) {
        console.error('[CortexSpeed] ❌ Cannot reach app:', err.message);
        appIsRunning = false;
        updatePopupStatus();
        setBadge('✕', '#FF5252', 5000);

        // Show in-page notification
        showToast('⚠ Cortex Speed is not running! Start the app first (run.bat), then retry.');
    }
}

// ─────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────
function isInternalUrl(url) {
    return url.startsWith('chrome') || url.startsWith('edge') ||
           url.startsWith('about:') || url.startsWith('blob:') ||
           url.startsWith('data:') || url.startsWith('javascript:') ||
           url.includes('chrome-extension://') || url.includes('moz-extension://');
}

function setBadge(text, color, durationMs) {
    chrome.action.setBadgeText({ text });
    chrome.action.setBadgeBackgroundColor({ color });
    if (durationMs) setTimeout(() => chrome.action.setBadgeText({ text: '' }), durationMs);
}

function showToast(message) {
    chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
        if (!tabs[0]?.id) return;
        chrome.scripting?.executeScript({
            target: { tabId: tabs[0].id },
            func: (msg) => {
                const d = document.createElement('div');
                d.textContent = msg;
                d.style.cssText = 'position:fixed;top:20px;right:20px;z-index:2147483647;background:#1C1F24;color:#EAEEF3;padding:14px 20px;border-radius:10px;border-left:4px solid #FF5252;font-family:Segoe UI,sans-serif;font-size:13px;box-shadow:0 8px 30px rgba(0,0,0,.5);max-width:360px;';
                document.body.appendChild(d);
                setTimeout(() => d.remove(), 6000);
            },
            args: [message]
        }).catch(() => {});
    });
}

// Keep service worker alive
setInterval(() => chrome.storage.local.get(['_ka'], () => {}), 25000);
chrome.alarms.create('keepAlive', { periodInMinutes: 0.4 });
chrome.alarms.onAlarm.addListener(() => pingApp());

console.log('[CortexSpeed] v4.0 started — using HTTP localhost:' + CORTEX_URL.split(':')[2]);
