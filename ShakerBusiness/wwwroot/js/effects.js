window.shakerEffects = {
    manualClickTokenKey: 'shakerManualClickTokens',
    oliChatHistoryKey: 'shakerOliChatHistory',
    guestStateKey: 'shakerGuestState',
    buyModeKey: 'shakerBuyMode',
    slotWalletKeyPrefix: 'shakerSlotWallet:',

    storeBuyMode(mode) {
        localStorage.setItem(this.buyModeKey, mode);
    },

    readBuyMode() {
        return localStorage.getItem(this.buyModeKey);
    },

    storeSlotWallet(ownerId, token) {
        localStorage.setItem(this.slotWalletKeyPrefix + ownerId, token);
    },

    readSlotWallet(ownerId) {
        return localStorage.getItem(this.slotWalletKeyPrefix + ownerId);
    },

    storeGuestState(token) {
        localStorage.setItem(this.guestStateKey, token);
    },

    readGuestState() {
        return localStorage.getItem(this.guestStateKey);
    },

    clearGuestState() {
        localStorage.removeItem(this.guestStateKey);
    },

    storeOliChatHistory(token) {
        localStorage.setItem(this.oliChatHistoryKey, token);
    },

    readOliChatHistory() {
        return localStorage.getItem(this.oliChatHistoryKey);
    },

    clearOliChatHistory() {
        localStorage.removeItem(this.oliChatHistoryKey);
    },

    storeManualClickToken(businessId, token) {
        const store = JSON.parse(localStorage.getItem(this.manualClickTokenKey) || '{}');
        store[businessId] = token;
        localStorage.setItem(this.manualClickTokenKey, JSON.stringify(store));
    },

    readAndClearManualClickTokens() {
        const raw = localStorage.getItem(this.manualClickTokenKey);
        localStorage.removeItem(this.manualClickTokenKey);
        return raw || '{}';
    },

    getTimezoneOffsetMinutes() {
        return new Date().getTimezoneOffset();
    },

    registerFlushOnHide(dotNetRef) {
        const flush = () => dotNetRef.invokeMethodAsync('FlushNow').catch(() => {});
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'hidden') {
                flush();
            }
        });
        window.addEventListener('pagehide', flush);
    },

    registerActivityTracking(dotNetRef, throttleMs) {
        let lastReport = 0;
        const report = () => {
            const now = Date.now();
            if (now - lastReport < throttleMs) {
                return;
            }
            lastReport = now;
            dotNetRef.invokeMethodAsync('OnUserActive').catch(() => {});
        };

        ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'].forEach(evt => {
            document.addEventListener(evt, report, { passive: true });
        });

        report();
    },

    syncSidebarHeight(sourceId, targetId) {
        if (this._sidebarHeightObserver) {
            this._sidebarHeightObserver.disconnect();
        }

        const source = document.getElementById(sourceId);
        const target = document.getElementById(targetId);
        if (!source || !target) {
            return;
        }

        const apply = () => {
            target.style.maxHeight = window.innerWidth >= 1280 ? `${source.offsetHeight}px` : '';
        };

        apply();
        this._sidebarHeightObserver = new ResizeObserver(apply);
        this._sidebarHeightObserver.observe(source);
        window.addEventListener('resize', apply);
    },
};
