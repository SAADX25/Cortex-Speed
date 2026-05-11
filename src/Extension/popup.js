const toggle = document.getElementById('toggleIntercept');
const statusDot = document.getElementById('statusDot');
const statusText = document.getElementById('statusText');
const appStatusEl = document.getElementById('appStatus');

// Load current state
chrome.storage.local.get(['interceptEnabled', 'appIsRunning'], (r) => {
    toggle.checked = r.interceptEnabled !== undefined ? r.interceptEnabled : true;
    updateUI(toggle.checked, r.appIsRunning === true);
});

// Listen for changes (app ping updates)
chrome.storage.onChanged.addListener((changes) => {
    chrome.storage.local.get(['interceptEnabled', 'appIsRunning'], (r) => {
        updateUI(r.interceptEnabled !== false, r.appIsRunning === true);
    });
});

toggle.addEventListener('change', () => {
    chrome.storage.local.set({ interceptEnabled: toggle.checked });
    chrome.storage.local.get(['appIsRunning'], (r) => updateUI(toggle.checked, r.appIsRunning === true));
});

function updateUI(intercept, appRunning) {
    toggle.checked = intercept;

    if (!intercept) {
        statusDot.className = 'status-dot inactive';
        statusText.textContent = 'Interception Disabled';
    } else if (!appRunning) {
        statusDot.className = 'status-dot warning';
        statusText.textContent = 'App Not Running!';
    } else {
        statusDot.className = 'status-dot active';
        statusText.textContent = 'Interception Active';
    }

    if (appStatusEl) {
        appStatusEl.textContent = appRunning ? '✅ Cortex Speed: Running' : '❌ Cortex Speed: Not Running — start run.bat';
        appStatusEl.style.color = appRunning ? '#00D4AA' : '#FF5252';
    }
}
