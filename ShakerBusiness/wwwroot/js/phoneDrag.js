window.shakerPhoneDrag = {
    _el: null,
    _handle: null,
    _dragging: false,
    _pointerId: null,
    _offsetX: 0,
    _offsetY: 0,
    _downHandler: null,
    _moveHandler: null,
    _upHandler: null,

    init(elementId, handleId) {
        const el = document.getElementById(elementId);
        const handle = document.getElementById(handleId);
        if (!el || !handle) {
            return;
        }

        this._el = el;
        this._handle = handle;
        this._setDefaultPosition();

        if (this._downHandler) {
            return;
        }

        this._downHandler = (e) => {
            this._dragging = true;
            this._pointerId = e.pointerId;
            const rect = el.getBoundingClientRect();
            this._offsetX = e.clientX - rect.left;
            this._offsetY = e.clientY - rect.top;
            try {
                handle.setPointerCapture(e.pointerId);
            } catch {
            }
            e.preventDefault();
        };
        this._moveHandler = (e) => {
            if (!this._dragging || this._pointerId !== e.pointerId) {
                return;
            }
            const rect = el.getBoundingClientRect();
            let left = e.clientX - this._offsetX;
            let top = e.clientY - this._offsetY;
            left = Math.max(0, Math.min(window.innerWidth - rect.width, left));
            top = Math.max(0, Math.min(window.innerHeight - rect.height, top));
            this._applyPosition(left, top);
        };
        this._upHandler = (e) => {
            if (this._pointerId !== e.pointerId) {
                return;
            }
            this._dragging = false;
            this._pointerId = null;
        };

        handle.addEventListener('pointerdown', this._downHandler);
        window.addEventListener('pointermove', this._moveHandler);
        window.addEventListener('pointerup', this._upHandler);
        window.addEventListener('pointercancel', this._upHandler);
    },

    reset() {
        if (this._el) {
            this._setDefaultPosition();
        }
    },

    _applyPosition(left, top) {
        this._el.style.left = `${left}px`;
        this._el.style.top = `${top}px`;
        this._el.style.right = 'auto';
        this._el.style.bottom = 'auto';
    },

    _setDefaultPosition() {
        const rect = this._el.getBoundingClientRect();
        const left = Math.max(0, window.innerWidth - rect.width - 24);
        const top = Math.max(0, window.innerHeight - rect.height - 24);
        this._applyPosition(left, top);
    },
};
