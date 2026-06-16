export function hasStoredPreference() {
    try {
        return localStorage.getItem('dsms.onboarding.collapsed') !== null;
    } catch {
        return false;
    }
}

export function getCollapsed() {
    try {
        return localStorage.getItem('dsms.onboarding.collapsed') === 'true';
    } catch {
        return false;
    }
}

export function setCollapsed(value) {
    try {
        localStorage.setItem('dsms.onboarding.collapsed', value ? 'true' : 'false');
    } catch {
        // localStorage nicht verfügbar – Zustand bleibt sitzungsbezogen
    }
}
