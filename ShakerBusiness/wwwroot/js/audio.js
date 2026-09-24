window.shakerAudio = {
    ambience: null,
    started: false,
    armed: false,
    volumeStorageKey: 'shakerVolume',
    sfxVolumeStorageKey: 'shakerSfxVolume',
    armAutoplayOnFirstClick() {
        if (this.armed) {
            return;
        }
        this.armed = true;
        document.addEventListener('click', () => this.init(), { once: true });
    },
    getStoredVolume() {
        const stored = parseFloat(localStorage.getItem(this.volumeStorageKey));
        return Number.isFinite(stored) ? Math.min(1, Math.max(0, stored)) : 0.5;
    },
    getStoredSfxVolume() {
        const stored = parseFloat(localStorage.getItem(this.sfxVolumeStorageKey));
        return Number.isFinite(stored) ? Math.min(1, Math.max(0, stored)) : 0.6;
    },
    applyAmbienceVolume(volume) {
        if (!this.ambience) {
            return;
        }
        this.ambience.volume = volume;
        this.ambience.muted = volume <= 0;
    },
    applyStoredVolume(sliderElementId) {
        const volume = this.getStoredVolume();
        const slider = document.getElementById(sliderElementId);
        if (slider) {
            slider.value = volume;
        }
        this.applyAmbienceVolume(volume);
        return volume;
    },
    applyStoredSfxVolume(sliderElementId) {
        const volume = this.getStoredSfxVolume();
        const slider = document.getElementById(sliderElementId);
        if (slider) {
            slider.value = volume;
        }
        return volume;
    },
    init() {
        const volume = this.getStoredVolume();
        if (!this.ambience) {
            this.ambience = new Audio('sounds/ambience.mp3');
            this.ambience.loop = true;
            this.applyAmbienceVolume(volume);
        }
        if (!this.started) {
            this.started = true;
            this.playStart();
            this.ambience.play().catch(() => {
                this.started = false;
            });
        }
    },
    setVolume(volume) {
        localStorage.setItem(this.volumeStorageKey, volume);
        this.applyAmbienceVolume(volume);
    },
    setSfxVolume(volume) {
        localStorage.setItem(this.sfxVolumeStorageKey, volume);
    },
    playSfx(path) {
        const sound = new Audio(path);
        const volume = this.getStoredSfxVolume();
        sound.volume = volume;
        sound.muted = volume <= 0;
        sound.play().catch(() => {});
    },
    playClick() {
        this.playSfx('sounds/click.mp3');
    },
    playBuy() {
        this.playSfx('sounds/buy.mp3');
    },
    playInvest() {
        this.playSfx('sounds/ShakerOliInvestSound.mp3');
    },
    playInvestUpgrade() {
        this.playSfx('sounds/ShakerOliInvestUpgrade.mp3');
    },
    playStart() {
        this.playSfx('sounds/ShakerStartSlotSound.mp3');
    },
    playJa() {
        this.playSfx('sounds/ShakerJaSound.mp3');
    },
    playManagerBuy() {
        this.playSfx('sounds/ShakerManagerBuySound.mp3');
    },
    playTimberDeath() {
        this.playSfx('sounds/ShakerOliTimber.mp3');
    },
};

document.addEventListener('click', (event) => {
    if (event.target.closest('.no-click-sound')) {
        return;
    }

    if (event.target.closest('button, .business-icon-wrap')) {
        window.shakerAudio.playClick();
    }
});
