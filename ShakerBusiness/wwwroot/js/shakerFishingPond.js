window.shakerFishingPond = {
    _frameId: null,
    _canvas: null,
    _ctx: null,
    _dotNetRef: null,
    _myId: null,
    _imageCache: new Map(),
    _moveSpeed: 0.12,
    _lastSyncSent: 0,
    _lastHeartbeat: 0,
    _lastFrame: 0,
    _keys: { w: false, a: false, s: false, d: false },
    _keydownHandler: null,
    _keyupHandler: null,

    _spots: [],
    _cameraViewSize: 0.30,
    _lastCamera: null,
    _oliShack: { cx: 0.78, cy: 0.22, halfW: 0.032, halfH: 0.024, fenceHalfW: 0.05, fenceHalfH: 0.042, interactionRadius: 0.12 },
    _mapBounds: { min: 0.03, max: 0.97 },

    _my: { x: 0.5, y: 0.14, name: '', avatar: null, casting: false, castX: 0, castY: 0 },
    _remote: new Map(),
    _myHasBite: false,
    _castColor: '#e2503f',
    _chatBubbles: new Map(),

    _qte: null,

    _getImage(url) {
        if (!url) {
            return null;
        }
        let img = this._imageCache.get(url);
        if (!img) {
            img = new Image();
            img.src = url;
            this._imageCache.set(url, img);
        }
        return img.complete && img.naturalWidth > 0 ? img : null;
    },

    _isInWater(x, y) {
        for (const spot of this._spots) {
            const dx = (x - spot.cx) / spot.rx;
            const dy = (y - spot.cy) / spot.ry;
            if (dx * dx + dy * dy <= 1) {
                return true;
            }
        }
        return false;
    },

    _isInOliShack(x, y) {
        const s = this._oliShack;
        return Math.abs(x - s.cx) <= s.fenceHalfW && Math.abs(y - s.cy) <= s.fenceHalfH;
    },

    _isBlocked(x, y) {
        return this._isInWater(x, y) || this._isInOliShack(x, y);
    },

    _getCamera() {
        const half = this._cameraViewSize / 2;
        const cx = Math.max(half, Math.min(1 - half, this._my.x));
        const cy = Math.max(half, Math.min(1 - half, this._my.y));
        return { left: cx - half, top: cy - half, size: this._cameraViewSize };
    },

    _worldToScreen(wx, wy, cam, w, h) {
        return {
            x: (wx - cam.left) / cam.size * w,
            y: (wy - cam.top) / cam.size * h,
        };
    },

    _mulberry32Next(q) {
        let a = q.rngState | 0;
        a = a + 0x6D2B79F5 | 0;
        q.rngState = a;
        let t = Math.imul(a ^ a >>> 15, 1 | a);
        t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
        return ((t ^ t >>> 14) >>> 0) / 4294967296;
    },

    _hashStr(s) {
        let h = 0;
        for (let i = 0; i < s.length; i++) {
            h = (h * 31 + s.charCodeAt(i)) | 0;
        }
        return h;
    },

    _buildPath(a, b) {
        const segments = 14;
        const points = [];
        const dx = b.cx - a.cx;
        const dy = b.cy - a.cy;
        const dist = Math.hypot(dx, dy) || 1;
        const nx = -dy / dist;
        const ny = dx / dist;
        const amplitude = Math.min(0.035, dist * 0.12);
        const phase = this._hashStr(a.id + b.id) % 100;
        for (let i = 0; i <= segments; i++) {
            const t = i / segments;
            const envelope = Math.sin(t * Math.PI);
            const wobble = Math.sin(t * Math.PI * 2.4 + phase) * amplitude * envelope;
            points.push({
                x: a.cx + dx * t + nx * wobble,
                y: a.cy + dy * t + ny * wobble,
            });
        }
        return points;
    },

    _computeMapDecor() {
        this._paths = [];
        this._groundDecor = { cacti: [], rocks: [], scrub: [] };

        const hub = this._spots.find(s => s.id === 'central') || this._spots[0];
        if (hub) {
            for (const spot of this._spots) {
                if (spot === hub) {
                    continue;
                }
                this._paths.push(this._buildPath(hub, spot));
            }

            const shack = this._oliShack;
            this._paths.push(this._buildPath(hub, {
                id: 'olishack',
                cx: shack.cx - shack.fenceHalfW * 0.2,
                cy: shack.cy + shack.fenceHalfH + 0.01,
            }));
        }

        const rng = { rngState: 0xC0FFEE };
        for (let i = 0; i < 55; i++) {
            const x = this._mulberry32Next(rng);
            const y = this._mulberry32Next(rng);
            const seed = this._mulberry32Next(rng);
            const isTall = this._mulberry32Next(rng) > 0.6;
            if (this._isBlocked(x, y)) {
                continue;
            }
            this._groundDecor.cacti.push({ x, y, seed, isTall });
        }
        for (let i = 0; i < 90; i++) {
            const x = this._mulberry32Next(rng);
            const y = this._mulberry32Next(rng);
            const seed = this._mulberry32Next(rng);
            if (this._isBlocked(x, y)) {
                continue;
            }
            this._groundDecor.scrub.push({ x, y, seed });
        }
        for (let i = 0; i < 45; i++) {
            const x = this._mulberry32Next(rng);
            const y = this._mulberry32Next(rng);
            const size = 2 + this._mulberry32Next(rng) * 2.5;
            if (this._isBlocked(x, y)) {
                continue;
            }
            this._groundDecor.rocks.push({ x, y, size });
        }
    },

    _setQteHolding(holding) {
        const q = this._qte;
        if (!q || q.holding === holding) {
            return;
        }
        q.holding = holding;
        q.events.push({ tMs: performance.now() - q.startTime, holding });
    },

    _isTypingTarget(el) {
        const tag = el && el.tagName;
        return tag === 'INPUT' || tag === 'TEXTAREA' || (el && el.isContentEditable);
    },

    init(canvasId, dotNetRef, myTwitchUserId, myName, myAvatarUrl, startX, startY, spotsJson, cameraViewSize, oliShackX, oliShackY, oliShackInteractionRadius) {
        this.destroy();

        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        this._canvas = canvas;
        this._ctx = canvas.getContext('2d');
        this._dotNetRef = dotNetRef;
        this._lastSyncSent = 0;
        this._lastHeartbeat = performance.now();
        this._myId = myTwitchUserId;
        this._my = { x: startX, y: startY, name: myName, avatar: myAvatarUrl, casting: false, castX: 0, castY: 0 };
        if (typeof oliShackX === 'number' && typeof oliShackY === 'number') {
            this._oliShack.cx = oliShackX;
            this._oliShack.cy = oliShackY;
            if (typeof oliShackInteractionRadius === 'number') {
                this._oliShack.interactionRadius = oliShackInteractionRadius;
            }
        }
        this._remote = new Map();
        this._qte = null;
        this._keys = { w: false, a: false, s: false, d: false };
        this._myHasBite = false;
        this._castColor = '#e2503f';
        this._chatBubbles = new Map();
        this._spots = spotsJson ? JSON.parse(spotsJson) : [];
        this._computeMapDecor();
        this._cameraViewSize = cameraViewSize || 0.30;
        this._lastCamera = null;

        const resize = () => {
            canvas.width = Math.max(1, Math.round(canvas.clientWidth));
            canvas.height = Math.max(1, Math.round(canvas.clientHeight));
        };
        resize();
        this._resizeHandler = resize;
        window.addEventListener('resize', resize);

        this._canvasClickHandler = (e) => this._handleClick(e);
        this._canvasMouseDownHandler = () => this._setQteHolding(true);
        this._canvasMouseUpHandler = () => this._setQteHolding(false);
        this._canvasTouchStartHandler = (e) => {
            if (this._qte) {
                e.preventDefault();
            }
            this._setQteHolding(true);
        };
        this._canvasTouchEndHandler = () => this._setQteHolding(false);

        canvas.addEventListener('click', this._canvasClickHandler);
        canvas.addEventListener('mousedown', this._canvasMouseDownHandler);
        canvas.addEventListener('mouseup', this._canvasMouseUpHandler);
        canvas.addEventListener('touchstart', this._canvasTouchStartHandler, { passive: false });
        canvas.addEventListener('touchend', this._canvasTouchEndHandler);

        this._keydownHandler = (e) => {
            if (this._isTypingTarget(e.target)) {
                return;
            }
            const k = e.key.toLowerCase();
            if (k === 'w' || k === 'a' || k === 's' || k === 'd') {
                this._keys[k] = true;
                e.preventDefault();
            }
        };
        this._keyupHandler = (e) => {
            const k = e.key.toLowerCase();
            if (k === 'w' || k === 'a' || k === 's' || k === 'd') {
                this._keys[k] = false;
            }
        };
        window.addEventListener('keydown', this._keydownHandler);
        window.addEventListener('keyup', this._keyupHandler);

        this._initJoystick();

        this._loop(performance.now());
    },

    _initJoystick() {
        this._joystickVector = { x: 0, y: 0 };
        this._joystickPointerId = null;
        const base = document.getElementById('fishing-joystick-base');
        const knob = document.getElementById('fishing-joystick-knob');
        if (!base || !knob) {
            return;
        }

        const isTouchPrimary = window.matchMedia && window.matchMedia('(pointer: coarse)').matches;
        if (!isTouchPrimary) {
            base.style.display = 'none';
            return;
        }

        const maxRadius = 34;

        const applyMove = (clientX, clientY) => {
            const rect = base.getBoundingClientRect();
            const cx = rect.left + rect.width / 2;
            const cy = rect.top + rect.height / 2;
            let dx = clientX - cx;
            let dy = clientY - cy;
            const dist = Math.hypot(dx, dy);
            if (dist > maxRadius) {
                dx = dx / dist * maxRadius;
                dy = dy / dist * maxRadius;
            }
            knob.style.transform = `translate(calc(-50% + ${dx}px), calc(-50% + ${dy}px))`;
            this._joystickVector = { x: dx / maxRadius, y: dy / maxRadius };
        };

        const resetJoystick = () => {
            this._joystickPointerId = null;
            this._joystickVector = { x: 0, y: 0 };
            knob.style.transform = 'translate(-50%, -50%)';
        };

        this._joystickDownHandler = (e) => {
            this._joystickPointerId = e.pointerId;
            try {
                if (base.setPointerCapture) {
                    base.setPointerCapture(e.pointerId);
                }
            } catch {
            }
            applyMove(e.clientX, e.clientY);
            e.preventDefault();
        };
        this._joystickMoveHandler = (e) => {
            if (this._joystickPointerId !== e.pointerId) {
                return;
            }
            applyMove(e.clientX, e.clientY);
            e.preventDefault();
        };
        this._joystickUpHandler = (e) => {
            if (this._joystickPointerId !== e.pointerId) {
                return;
            }
            resetJoystick();
        };

        base.addEventListener('pointerdown', this._joystickDownHandler);
        base.addEventListener('pointermove', this._joystickMoveHandler);
        base.addEventListener('pointerup', this._joystickUpHandler);
        base.addEventListener('pointercancel', this._joystickUpHandler);
        this._joystickBase = base;
    },

    _handleClick(e) {
        if (this._qte || this._my.casting) {
            return;
        }

        const rect = this._canvas.getBoundingClientRect();
        const sx = (e.clientX - rect.left) / rect.width;
        const sy = (e.clientY - rect.top) / rect.height;
        const cam = this._lastCamera || this._getCamera();
        const nx = cam.left + sx * cam.size;
        const ny = cam.top + sy * cam.size;

        if (this._isInOliShack(nx, ny)) {
            this._dotNetRef.invokeMethodAsync('OnOliShackClicked').catch(() => {});
            return;
        }

        if (!this._isInWater(nx, ny)) {
            return;
        }

        this._my.casting = true;
        this._my.moving = false;
        this._my.castX = nx;
        this._my.castY = ny;
        this._my.castStartTime = performance.now();
        this._dotNetRef.invokeMethodAsync('OnCastRequested', nx, ny).catch(() => {});
    },

    setCasting(casting) {
        this._my.casting = casting;
        if (!casting) {
            this._qte = null;
        }
    },

    setCastColor(color) {
        this._castColor = color || '#e2503f';
    },

    showChatBubble(twitchUserId, text) {
        if (!twitchUserId || !text) {
            return;
        }
        const now = performance.now();
        for (const [id, bubble] of this._chatBubbles) {
            if (bubble.expiresAt <= now) {
                this._chatBubbles.delete(id);
            }
        }
        const trimmed = text.length > 42 ? text.slice(0, 41) + '…' : text;
        this._chatBubbles.set(twitchUserId, { text: trimmed, expiresAt: now + 5000 });
    },

    syncPlayers(playersJson) {
        const list = JSON.parse(playersJson);
        const seen = new Set();
        for (const p of list) {
            if (p.twitchUserId === this._myId) {
                continue;
            }
            seen.add(p.twitchUserId);
            let r = this._remote.get(p.twitchUserId);
            if (!r) {
                r = { renderX: p.x, renderY: p.y, casting: false };
                this._remote.set(p.twitchUserId, r);
            }

            const moved = Math.abs(r.x - p.x) > 0.001 || Math.abs(r.y - p.y) > 0.001;
            if (moved) {
                r.lastMoveTime = performance.now();
            }
            if (p.isCasting && !r.casting) {
                r.castStartTime = performance.now();
            }

            r.x = p.x;
            r.y = p.y;
            r.name = p.displayName;
            r.avatar = p.profileImageUrl;
            r.casting = p.isCasting;
            r.castX = p.castX;
            r.castY = p.castY;
            r.hasBite = p.hasBite;
            r.castColor = p.castColor || '#e2503f';
        }
        for (const key of Array.from(this._remote.keys())) {
            if (!seen.has(key)) {
                this._remote.delete(key);
            }
        }
    },

    startQte(difficulty, seed) {
        const startTime = performance.now();
        this._qte = {
            rngState: seed | 0,
            fishPos: 0.5,
            fishTarget: 0,
            fishRetarget: startTime + 700,
            fishSpeed: 0.55 + difficulty * 0.65,
            barPos: 0.5,
            barVelocity: 0,
            holding: false,
            progress: 0.4,
            zoneHeight: 0.42,
            fillRate: 0.62,
            drainRate: 0.26 + difficulty * 0.16,
            maxSpeed: 1.1,
            riseAccel: 5.2,
            fallAccel: 3.4,
            overlapSmooth: 0,
            startTime,
            duration: 7000,
            events: [],
            done: false,
        };
        this._qte.fishTarget = this._mulberry32Next(this._qte) * this._qteMaxFishTarget(this._qte);
    },

    _qteMaxFishTarget(q) {
        return Math.max(0, 1 - q.zoneHeight / 2 - 0.06);
    },

    _updateQte(dt) {
        const q = this._qte;
        if (!q || q.done) {
            return;
        }

        const dtSec = dt / 1000;
        const now = performance.now();
        if (now >= q.fishRetarget) {
            q.fishTarget = this._mulberry32Next(q) * this._qteMaxFishTarget(q);
            q.fishRetarget = now + 550 + this._mulberry32Next(q) * 650;
        }
        const fishLerp = 1 - Math.pow(0.001, dtSec * q.fishSpeed);
        q.fishPos += (q.fishTarget - q.fishPos) * fishLerp;

        if (q.holding) {
            q.barVelocity = Math.min(q.maxSpeed, q.barVelocity + q.riseAccel * dtSec);
        } else {
            q.barVelocity = Math.max(-q.maxSpeed, q.barVelocity - q.fallAccel * dtSec);
        }
        q.barPos += q.barVelocity * dtSec;
        if (q.barPos < 0) {
            q.barPos = 0;
            q.barVelocity = 0;
        } else if (q.barPos > 1) {
            q.barPos = 1;
            q.barVelocity = 0;
        }

        const overlap = Math.abs(q.fishPos - (1 - q.barPos)) < q.zoneHeight / 2;
        const overlapTarget = overlap ? 1 : 0;
        const overlapLerp = 1 - Math.pow(0.001, dtSec * 12);
        q.overlapSmooth += (overlapTarget - q.overlapSmooth) * overlapLerp;
        const rate = q.overlapSmooth * q.fillRate - (1 - q.overlapSmooth) * q.drainRate;
        q.progress += rate * dtSec;
        q.progress = Math.max(0, Math.min(1, q.progress));

        const elapsed = now - q.startTime;
        if (q.progress >= 1 || q.progress <= 0 || elapsed >= q.duration) {
            q.done = true;
            const events = q.events;
            const elapsedMs = Math.min(elapsed, q.duration);
            this._qte = null;
            this._my.casting = false;
            this._dotNetRef.invokeMethodAsync('OnCatchAttemptFinished', events, elapsedMs).catch(() => {});
        }
    },

    _drawRipple(ctx, x, y, radius, alpha) {
        if (alpha <= 0) {
            return;
        }
        ctx.beginPath();
        ctx.strokeStyle = `rgba(255,255,255,${alpha})`;
        ctx.lineWidth = 1.2;
        ctx.arc(x, y, radius, 0, Math.PI * 2);
        ctx.stroke();
    },

    _drawPlayer(ctx, px, pyIn, name, avatarUrl, casting, bx, by, castStartTime, hasBite, isSelf, isMoving, bobPhase, castColor, twitchUserId) {
        const bob = isMoving ? Math.sin(performance.now() / 140 + bobPhase) * 2 : 0;
        const py = pyIn + bob;

        if (casting) {
            const throwDuration = 420;
            const elapsed = castStartTime ? performance.now() - castStartTime : throwDuration;
            const t = Math.max(0, Math.min(1, elapsed / throwDuration));
            const eased = 1 - Math.pow(1 - t, 3);
            const arcLift = Math.sin(t * Math.PI) * 26;
            const lureX = px + (bx - px) * eased;
            const lureY = py - 14 + (by - (py - 14)) * eased - arcLift;

            ctx.strokeStyle = 'rgba(255,255,255,0.75)';
            ctx.lineWidth = 1.4;
            ctx.beginPath();
            ctx.moveTo(px, py - 14);
            ctx.lineTo(lureX, lureY);
            ctx.stroke();

            ctx.beginPath();
            ctx.fillStyle = isSelf ? this._castColor : (castColor || '#e2503f');
            ctx.arc(lureX, lureY, 4, 0, Math.PI * 2);
            ctx.fill();

            if (t >= 1) {
                const cycle = 2200;
                const ripplePhase = ((elapsed - throwDuration) % cycle) / cycle;
                this._drawRipple(ctx, bx, by, 3 + ripplePhase * 15, (1 - ripplePhase) * 0.4);
            }

            if (hasBite) {
                const pulse = 1 + Math.sin(performance.now() / 110) * 0.15;
                ctx.save();
                ctx.translate(bx, by - 12);
                ctx.scale(pulse, pulse);
                ctx.fillStyle = '#f4c542';
                ctx.font = 'bold 16px sans-serif';
                ctx.textAlign = 'center';
                ctx.fillText('!', 0, 0);
                ctx.restore();
            }
        }

        const radius = 15;
        ctx.save();
        ctx.beginPath();
        ctx.arc(px, py, radius, 0, Math.PI * 2);
        ctx.closePath();
        ctx.fillStyle = isSelf ? '#f4c542' : '#8a5a2e';
        ctx.fill();
        ctx.clip();

        const img = this._getImage(avatarUrl);
        if (img) {
            ctx.drawImage(img, px - radius, py - radius, radius * 2, radius * 2);
        } else {
            ctx.fillStyle = '#241a12';
            ctx.fillRect(px - radius, py - radius, radius * 2, radius * 2);
            ctx.fillStyle = '#f4c542';
            ctx.font = 'bold 14px sans-serif';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText((name || '?').charAt(0).toUpperCase(), px, py + 1);
        }
        ctx.restore();

        ctx.strokeStyle = isSelf ? '#f4c542' : 'rgba(255,255,255,0.6)';
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.arc(px, py, radius, 0, Math.PI * 2);
        ctx.stroke();

        ctx.font = 'bold 10px sans-serif';
        ctx.textAlign = 'center';
        ctx.fillStyle = '#241a12';
        ctx.lineWidth = 3;
        ctx.strokeStyle = 'rgba(255,255,255,0.85)';
        ctx.strokeText(name || '', px, py - radius - 6);
        ctx.fillText(name || '', px, py - radius - 6);

        const bubble = twitchUserId ? this._chatBubbles.get(twitchUserId) : null;
        if (bubble) {
            if (bubble.expiresAt <= performance.now()) {
                this._chatBubbles.delete(twitchUserId);
            } else {
                this._drawChatBubble(ctx, px, py - radius - 20, bubble.text);
            }
        }
    },

    _drawChatBubble(ctx, px, bottomY, text) {
        const maxWidth = 190;
        ctx.save();
        ctx.font = '11px sans-serif';
        const textWidth = Math.min(maxWidth, ctx.measureText(text).width);
        const bubbleW = textWidth + 16;
        const bubbleH = 22;
        const bubbleX = px - bubbleW / 2;
        const bubbleY = bottomY - bubbleH;

        ctx.fillStyle = 'rgba(255,255,255,0.95)';
        this._roundRect(ctx, bubbleX, bubbleY, bubbleW, bubbleH, 8);
        ctx.fill();
        ctx.strokeStyle = 'rgba(0,0,0,0.15)';
        ctx.lineWidth = 1;
        ctx.stroke();

        ctx.beginPath();
        ctx.moveTo(px - 5, bubbleY + bubbleH);
        ctx.lineTo(px + 5, bubbleY + bubbleH);
        ctx.lineTo(px, bubbleY + bubbleH + 6);
        ctx.closePath();
        ctx.fillStyle = 'rgba(255,255,255,0.95)';
        ctx.fill();

        ctx.fillStyle = '#241a12';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(text, px, bubbleY + bubbleH / 2, maxWidth);
        ctx.restore();
    },

    _drawAmbientRipples(ctx, cx, cy, rx, ry) {
        const now = performance.now();
        for (let i = 0; i < 6; i++) {
            const seed = i * 971;
            const px = cx + (((seed * 97) % 200) / 200 - 0.5) * rx * 1.5;
            const py = cy + (((seed * 53) % 200) / 200 - 0.5) * ry * 1.5;
            const cycle = 4200;
            const phase = ((now + seed * 3) % cycle) / cycle;
            if (phase > 0.8) {
                continue;
            }
            this._drawRipple(ctx, px, py, 3 + phase * 18, (1 - phase / 0.8) * 0.22);
        }
    },

    _drawReeds(ctx, cx, cy, rx, ry) {
        const count = 16;
        const now = performance.now();
        for (let i = 0; i < count; i++) {
            const angle = (i / count) * Math.PI * 2;
            if (((Math.sin(angle * 2.7 + 1.3) + 1) / 2) > 0.4) {
                continue;
            }
            const bx = cx + Math.cos(angle) * (rx + 5);
            const by = cy + Math.sin(angle) * (ry + 5);
            const sway = Math.sin(now / 900 + i * 1.7) * 3;
            ctx.strokeStyle = '#7a8f3f';
            ctx.lineCap = 'round';
            ctx.lineWidth = 2;
            for (let b = 0; b < 3; b++) {
                const ox = (b - 1) * 3;
                ctx.beginPath();
                ctx.moveTo(bx + ox, by + 6);
                ctx.quadraticCurveTo(bx + ox + sway, by - 6, bx + ox + sway * 1.6, by - 15);
                ctx.stroke();
            }
        }
    },

    _drawGroundDecor(ctx, cam, w, h) {
        const margin = 0.02;
        const left = cam.left - margin;
        const right = cam.left + cam.size + margin;
        const top = cam.top - margin;
        const bottom = cam.top + cam.size + margin;
        const now = performance.now();

        for (const rock of this._groundDecor.rocks) {
            if (rock.x < left || rock.x > right || rock.y < top || rock.y > bottom) {
                continue;
            }
            const p = this._worldToScreen(rock.x, rock.y, cam, w, h);
            ctx.beginPath();
            ctx.fillStyle = 'rgba(0,0,0,0.2)';
            ctx.ellipse(p.x + 1, p.y + 1.5, rock.size, rock.size * 0.7, 0, 0, Math.PI * 2);
            ctx.fill();
            ctx.beginPath();
            ctx.fillStyle = '#a97a52';
            ctx.ellipse(p.x, p.y, rock.size, rock.size * 0.7, 0, 0, Math.PI * 2);
            ctx.fill();
            ctx.beginPath();
            ctx.fillStyle = 'rgba(255,235,200,0.25)';
            ctx.ellipse(p.x - rock.size * 0.25, p.y - rock.size * 0.25, rock.size * 0.35, rock.size * 0.25, 0, 0, Math.PI * 2);
            ctx.fill();
        }

        for (const s of this._groundDecor.scrub) {
            if (s.x < left || s.x > right || s.y < top || s.y > bottom) {
                continue;
            }
            const p = this._worldToScreen(s.x, s.y, cam, w, h);
            const sway = Math.sin(now / 1300 + s.seed * 20) * 1.1;
            ctx.strokeStyle = '#8a7a3f';
            ctx.lineCap = 'round';
            ctx.lineWidth = 1.2;
            for (let b = 0; b < 4; b++) {
                const ox = (b - 1.5) * 2;
                ctx.beginPath();
                ctx.moveTo(p.x + ox, p.y + 2);
                ctx.quadraticCurveTo(p.x + ox + sway * 0.5, p.y - 2, p.x + ox + sway, p.y - 5 - b * 0.3);
                ctx.stroke();
            }
        }

        for (const c of this._groundDecor.cacti) {
            if (c.x < left || c.x > right || c.y < top || c.y > bottom) {
                continue;
            }
            const p = this._worldToScreen(c.x, c.y, cam, w, h);
            this._drawCactus(ctx, p.x, p.y, c);
        }
    },

    _drawCactus(ctx, px, py, c) {
        const bodyH = c.isTall ? 22 : 11;
        const bodyW = c.isTall ? 6 : 8;

        ctx.beginPath();
        ctx.fillStyle = 'rgba(0,0,0,0.2)';
        ctx.ellipse(px + 1, py + 2, bodyW * 0.9, bodyW * 0.35, 0, 0, Math.PI * 2);
        ctx.fill();

        ctx.fillStyle = '#4f7a4a';
        this._roundRect(ctx, px - bodyW / 2, py - bodyH, bodyW, bodyH, bodyW / 2);
        ctx.fill();

        if (c.isTall) {
            ctx.fillStyle = '#4f7a4a';
            this._roundRect(ctx, px - bodyW / 2 - 6, py - bodyH * 0.65, 5, bodyH * 0.4, 2.5);
            ctx.fill();
            this._roundRect(ctx, px - bodyW / 2 - 6, py - bodyH * 0.72, 5, 6, 2.5);
            ctx.fill();
            this._roundRect(ctx, px + bodyW / 2 + 1, py - bodyH * 0.5, 5, bodyH * 0.35, 2.5);
            ctx.fill();
            this._roundRect(ctx, px + bodyW / 2 + 1, py - bodyH * 0.5 - 6, 5, 6, 2.5);
            ctx.fill();
        }

        ctx.strokeStyle = 'rgba(0,0,0,0.18)';
        ctx.lineWidth = 1;
        for (let r = -1; r <= 1; r++) {
            ctx.beginPath();
            ctx.moveTo(px + r * bodyW * 0.28, py - 1);
            ctx.lineTo(px + r * bodyW * 0.28, py - bodyH + 1);
            ctx.stroke();
        }

        if (!c.isTall) {
            ctx.fillStyle = '#e2503f';
            for (let i = 0; i < 3; i++) {
                const fx = px - bodyW * 0.3 + i * bodyW * 0.3;
                ctx.beginPath();
                ctx.arc(fx, py - bodyH - 1, 1.4, 0, Math.PI * 2);
                ctx.fill();
            }
        }
    },

    _drawPaths(ctx, cam, w, h) {
        const widthPx = Math.max(3, (0.02 / cam.size) * w);
        for (const path of this._paths) {
            const screenPts = path.map(p => this._worldToScreen(p.x, p.y, cam, w, h));
            let visible = false;
            for (const sp of screenPts) {
                if (sp.x > -40 && sp.x < w + 40 && sp.y > -40 && sp.y < h + 40) {
                    visible = true;
                    break;
                }
            }
            if (!visible) {
                continue;
            }

            ctx.beginPath();
            ctx.moveTo(screenPts[0].x, screenPts[0].y);
            for (let i = 1; i < screenPts.length; i++) {
                ctx.lineTo(screenPts[i].x, screenPts[i].y);
            }
            ctx.strokeStyle = 'rgba(60,42,24,0.35)';
            ctx.lineWidth = widthPx + 3;
            ctx.lineCap = 'round';
            ctx.lineJoin = 'round';
            ctx.stroke();

            ctx.beginPath();
            ctx.moveTo(screenPts[0].x, screenPts[0].y);
            for (let i = 1; i < screenPts.length; i++) {
                ctx.lineTo(screenPts[i].x, screenPts[i].y);
            }
            ctx.strokeStyle = '#8a6b45';
            ctx.lineWidth = widthPx;
            ctx.stroke();

            ctx.beginPath();
            ctx.moveTo(screenPts[0].x, screenPts[0].y);
            for (let i = 1; i < screenPts.length; i++) {
                ctx.lineTo(screenPts[i].x, screenPts[i].y);
            }
            ctx.setLineDash([widthPx * 0.5, widthPx * 0.9]);
            ctx.strokeStyle = 'rgba(196,168,120,0.45)';
            ctx.lineWidth = Math.max(1, widthPx * 0.22);
            ctx.stroke();
            ctx.setLineDash([]);
        }
    },

    _drawPondDeco(ctx, spot, center, rx, ry) {
        const angle = (this._hashStr(spot.id) % 360) * Math.PI / 180;
        const dockLen = Math.max(18, rx * 0.9);
        const dx = Math.cos(angle);
        const dy = Math.sin(angle);
        const baseX = center.x + dx * rx * 0.55;
        const baseY = center.y + dy * ry * 0.55;
        const tipX = baseX + dx * dockLen;
        const tipY = baseY + dy * dockLen;
        const perpX = -dy;
        const perpY = dx;
        const plankHalf = 7;

        ctx.strokeStyle = '#6b4a2a';
        ctx.lineWidth = 3;
        for (let side = -1; side <= 1; side += 2) {
            ctx.beginPath();
            ctx.moveTo(baseX + perpX * plankHalf * side, baseY + perpY * plankHalf * side);
            ctx.lineTo(tipX + perpX * plankHalf * side, tipY + perpY * plankHalf * side);
            ctx.stroke();
        }
        ctx.strokeStyle = '#8a6238';
        ctx.lineWidth = 5;
        const plankCount = 5;
        for (let i = 0; i <= plankCount; i++) {
            const t = i / plankCount;
            const px = baseX + (tipX - baseX) * t;
            const py = baseY + (tipY - baseY) * t;
            ctx.beginPath();
            ctx.moveTo(px + perpX * plankHalf, py + perpY * plankHalf);
            ctx.lineTo(px - perpX * plankHalf, py - perpY * plankHalf);
            ctx.stroke();
        }

        const crateAngle = angle + 1.9;
        const crateX = center.x + Math.cos(crateAngle) * (rx + 16);
        const crateY = center.y + Math.sin(crateAngle) * (ry + 12);
        ctx.fillStyle = '#6b4a2a';
        ctx.fillRect(crateX - 6, crateY - 6, 12, 12);
        ctx.strokeStyle = '#3a2a1a';
        ctx.lineWidth = 1;
        ctx.strokeRect(crateX - 6, crateY - 6, 12, 12);
        ctx.beginPath();
        ctx.moveTo(crateX - 6, crateY - 6);
        ctx.lineTo(crateX + 6, crateY + 6);
        ctx.moveTo(crateX + 6, crateY - 6);
        ctx.lineTo(crateX - 6, crateY + 6);
        ctx.stroke();

        const barrelX = crateX + 14;
        const barrelY = crateY + 4;
        ctx.fillStyle = '#5a4632';
        this._roundRect(ctx, barrelX - 5, barrelY - 8, 10, 16, 3);
        ctx.fill();
        ctx.strokeStyle = '#3a2a1a';
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.moveTo(barrelX - 5, barrelY - 3);
        ctx.lineTo(barrelX + 5, barrelY - 3);
        ctx.moveTo(barrelX - 5, barrelY + 3);
        ctx.lineTo(barrelX + 5, barrelY + 3);
        ctx.stroke();
    },

    _drawOliShack(ctx, cam, w, h) {
        const s = this._oliShack;
        const now = performance.now();
        const center = this._worldToScreen(s.cx, s.cy, cam, w, h);
        const fenceRx = (s.fenceHalfW / cam.size) * w;
        const fenceRy = (s.fenceHalfH / cam.size) * h;
        if (center.x + fenceRx < 0 || center.x - fenceRx > w || center.y + fenceRy < 0 || center.y - fenceRy > h) {
            return;
        }

        ctx.beginPath();
        ctx.fillStyle = 'rgba(120,86,48,0.35)';
        ctx.ellipse(center.x, center.y + 6, fenceRx * 0.94, fenceRy * 0.94, 0, 0, Math.PI * 2);
        ctx.fill();

        const fx0 = center.x - fenceRx;
        const fx1 = center.x + fenceRx;
        const fy0 = center.y - fenceRy;
        const fy1 = center.y + fenceRy;
        const postGap = 16;
        ctx.strokeStyle = '#5a4632';
        ctx.lineWidth = 1.6;
        const postXCount = Math.max(2, Math.round((fx1 - fx0) / postGap));
        const postYCount = Math.max(2, Math.round((fy1 - fy0) / postGap));
        for (let i = 0; i <= postXCount; i++) {
            const px = fx0 + (fx1 - fx0) * (i / postXCount);
            ctx.beginPath();
            ctx.moveTo(px, fy0);
            ctx.lineTo(px, fy0 - 5);
            ctx.moveTo(px, fy1);
            ctx.lineTo(px, fy1 - 5);
            ctx.stroke();
        }
        for (let i = 0; i <= postYCount; i++) {
            const py = fy0 + (fy1 - fy0) * (i / postYCount);
            ctx.beginPath();
            ctx.moveTo(fx0, py);
            ctx.lineTo(fx0 - 5, py);
            ctx.moveTo(fx1, py);
            ctx.lineTo(fx1 - 5, py);
            ctx.stroke();
        }
        ctx.setLineDash([4, 3]);
        ctx.strokeStyle = '#8a8378';
        ctx.lineWidth = 1;
        ctx.strokeRect(fx0, fy0, fx1 - fx0, fy1 - fy0);
        ctx.setLineDash([]);

        const wallRx = (s.halfW / cam.size) * w;
        const wallRy = (s.halfH / cam.size) * h;
        const wallX = center.x - wallRx;
        const wallY = center.y - wallRy;
        const wallW = wallRx * 2;
        const wallH = wallRy * 2;

        ctx.beginPath();
        ctx.fillStyle = 'rgba(0,0,0,0.22)';
        ctx.ellipse(center.x + 2, center.y + wallRy + 3, wallRx * 1.05, 5, 0, 0, Math.PI * 2);
        ctx.fill();

        const wallGrad = ctx.createLinearGradient(0, wallY, 0, wallY + wallH);
        wallGrad.addColorStop(0, '#c9683a');
        wallGrad.addColorStop(1, '#9e4a26');
        ctx.fillStyle = wallGrad;
        ctx.fillRect(wallX, wallY, wallW, wallH);
        ctx.strokeStyle = '#7a3a1e';
        ctx.lineWidth = 1.5;
        ctx.strokeRect(wallX, wallY, wallW, wallH);

        ctx.strokeStyle = 'rgba(0,0,0,0.14)';
        ctx.lineWidth = 1;
        const courseCount = 3;
        for (let i = 1; i < courseCount; i++) {
            const cy = wallY + (wallH / courseCount) * i;
            ctx.beginPath();
            ctx.moveTo(wallX, cy);
            ctx.lineTo(wallX + wallW, cy);
            ctx.stroke();
        }

        const roofOverhang = 5;
        const roofPeakY = wallY - 12;
        const roofLeftX = wallX - roofOverhang;
        const roofRightX = wallX + wallW + roofOverhang;
        const roofBaseY = wallY + 2;

        ctx.save();
        ctx.beginPath();
        ctx.moveTo(roofLeftX, roofBaseY);
        ctx.lineTo(center.x, roofPeakY);
        ctx.lineTo(roofRightX, roofBaseY);
        ctx.closePath();
        ctx.fillStyle = '#6b7076';
        ctx.fill();
        ctx.clip();

        ctx.strokeStyle = '#3a3d40';
        ctx.lineWidth = 1;
        const ridgeCount = 6;
        for (let i = 1; i < ridgeCount; i++) {
            const t = i / ridgeCount;
            const lx = roofLeftX + (center.x - roofLeftX) * t;
            const ly = roofBaseY + (roofPeakY - roofBaseY) * t;
            ctx.beginPath();
            ctx.moveTo(lx, roofBaseY);
            ctx.lineTo(lx, ly);
            ctx.stroke();
            const rx = roofRightX + (center.x - roofRightX) * t;
            const ry = roofBaseY + (roofPeakY - roofBaseY) * t;
            ctx.beginPath();
            ctx.moveTo(rx, roofBaseY);
            ctx.lineTo(rx, ry);
            ctx.stroke();
        }
        ctx.restore();

        ctx.strokeStyle = '#3a3d40';
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.moveTo(roofLeftX, roofBaseY);
        ctx.lineTo(center.x, roofPeakY);
        ctx.lineTo(roofRightX, roofBaseY);
        ctx.stroke();

        const chimneyX = wallX + wallW * 0.78;
        ctx.fillStyle = '#6b6b6b';
        ctx.fillRect(chimneyX - 2.5, roofPeakY - 6, 5, 10);
        const smokeT = (now / 1400) % 1;
        ctx.fillStyle = `rgba(220,220,220,${0.3 * (1 - smokeT)})`;
        ctx.beginPath();
        ctx.arc(chimneyX + smokeT * 3, roofPeakY - 8 - smokeT * 14, 2 + smokeT * 3, 0, Math.PI * 2);
        ctx.fill();

        const doorW = wallW * 0.26;
        const doorH = wallH * 0.62;
        const doorX = wallX + wallW * 0.36 - doorW / 2;
        const doorY = wallY + wallH - doorH;
        ctx.fillStyle = '#4a3320';
        ctx.fillRect(doorX, doorY, doorW, doorH);
        ctx.strokeStyle = '#2a1c10';
        ctx.lineWidth = 1;
        for (let i = 1; i < 3; i++) {
            ctx.beginPath();
            ctx.moveTo(doorX + (doorW / 3) * i, doorY);
            ctx.lineTo(doorX + (doorW / 3) * i, doorY + doorH);
            ctx.stroke();
        }

        const winSize = wallH * 0.24;
        const winX = wallX + wallW * 0.74 - winSize / 2;
        const winY = wallY + wallH * 0.26;
        ctx.fillStyle = '#1c1712';
        ctx.fillRect(winX, winY, winSize, winSize);
        ctx.strokeStyle = '#3a2a1a';
        ctx.lineWidth = 1;
        ctx.strokeRect(winX, winY, winSize, winSize);
        ctx.beginPath();
        ctx.moveTo(winX + winSize / 3, winY);
        ctx.lineTo(winX + winSize / 3, winY + winSize);
        ctx.moveTo(winX + (winSize / 3) * 2, winY);
        ctx.lineTo(winX + (winSize / 3) * 2, winY + winSize);
        ctx.stroke();

        const signX = center.x;
        const signY = doorY + doorH + 16;
        const signW = 46;
        const signH = 18;
        ctx.strokeStyle = '#5a4632';
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(signX, doorY + doorH + 2);
        ctx.lineTo(signX, signY);
        ctx.stroke();

        ctx.fillStyle = '#c98a4f';
        this._roundRect(ctx, signX - signW / 2, signY - signH / 2, signW, signH, 2);
        ctx.fill();
        ctx.strokeStyle = '#5a4632';
        ctx.lineWidth = 1;
        this._roundRect(ctx, signX - signW / 2, signY - signH / 2, signW, signH, 2);
        ctx.stroke();

        const avatarCx = signX - signW / 2 + 9;
        const oliImg = this._getImage('images/businesses/Oli.png');
        if (oliImg) {
            ctx.save();
            ctx.beginPath();
            ctx.arc(avatarCx, signY, 6, 0, Math.PI * 2);
            ctx.closePath();
            ctx.clip();
            ctx.drawImage(oliImg, avatarCx - 6, signY - 6, 12, 12);
            ctx.restore();
        }
        ctx.fillStyle = '#2a1c10';
        ctx.font = 'bold 7px sans-serif';
        ctx.textAlign = 'left';
        ctx.fillText('PRIVAT', signX - signW / 2 + 17, signY + 2.5);

        ctx.font = 'bold 10px sans-serif';
        ctx.textAlign = 'center';
        ctx.lineWidth = 3;
        ctx.strokeStyle = 'rgba(0,0,0,0.45)';
        ctx.strokeText('Investor Olis Versteck', center.x, roofPeakY - 8);
        ctx.fillStyle = 'rgba(255,255,255,0.9)';
        ctx.fillText('Investor Olis Versteck', center.x, roofPeakY - 8);

        const bob = Math.sin(now / 500) * 3;
        const coinY = roofPeakY - 24 + bob;
        ctx.save();
        ctx.shadowColor = '#f4c542';
        ctx.shadowBlur = 8;
        ctx.beginPath();
        ctx.fillStyle = '#f4c542';
        ctx.arc(center.x, coinY, 7, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
        ctx.strokeStyle = '#8a5a2e';
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.arc(center.x, coinY, 7, 0, Math.PI * 2);
        ctx.stroke();
        ctx.fillStyle = '#8a5a2e';
        ctx.font = 'bold 9px sans-serif';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText('$', center.x, coinY + 0.5);
        ctx.textBaseline = 'alphabetic';

        const dx = this._my.x - s.cx;
        const dy = this._my.y - s.cy;
        const inRange = dx * dx + dy * dy <= s.interactionRadius * s.interactionRadius;
        if (inRange) {
            const pulse = (Math.sin(now / 260) + 1) / 2;
            ctx.save();
            ctx.strokeStyle = `rgba(244,197,66,${0.35 + pulse * 0.35})`;
            ctx.lineWidth = 2.5;
            this._roundRect(ctx, fx0 - 3, fy0 - 3, (fx1 - fx0) + 6, (fy1 - fy0) + 6, 6);
            ctx.stroke();
            ctx.restore();

            ctx.font = 'bold 9px sans-serif';
            ctx.textAlign = 'center';
            ctx.lineWidth = 3;
            ctx.strokeStyle = 'rgba(0,0,0,0.5)';
            ctx.strokeText('Klicken zum Handeln', center.x, fy1 + 14);
            ctx.fillStyle = '#f4c542';
            ctx.fillText('Klicken zum Handeln', center.x, fy1 + 14);
        }
    },

    _drawFenceLine(ctx, wx0, wy0, wx1, wy1, cam, w, h) {
        const p0 = this._worldToScreen(wx0, wy0, cam, w, h);
        const p1 = this._worldToScreen(wx1, wy1, cam, w, h);
        const dx = p1.x - p0.x;
        const dy = p1.y - p0.y;
        const len = Math.hypot(dx, dy);
        if (len < 1) {
            return;
        }

        const nx = -dy / len;
        const ny = dx / len;
        const postGap = 26;
        const postCount = Math.max(1, Math.round(len / postGap));

        ctx.strokeStyle = '#8a8378';
        ctx.lineWidth = 1;
        ctx.setLineDash([5, 4]);
        for (let side = -1; side <= 1; side += 2) {
            ctx.beginPath();
            ctx.moveTo(p0.x + nx * 3 * side, p0.y + ny * 3 * side);
            ctx.lineTo(p1.x + nx * 3 * side, p1.y + ny * 3 * side);
            ctx.stroke();
        }
        ctx.setLineDash([]);

        ctx.strokeStyle = '#5a4632';
        ctx.lineWidth = 1.8;
        for (let i = 0; i <= postCount; i++) {
            const t = i / postCount;
            const px = p0.x + dx * t;
            const py = p0.y + dy * t;
            ctx.beginPath();
            ctx.moveTo(px - nx * 7, py - ny * 7);
            ctx.lineTo(px + nx * 7, py + ny * 7);
            ctx.stroke();
        }
    },

    _drawMapFence(ctx, cam, w, h) {
        const b = this._mapBounds;
        const left = Math.max(b.min, cam.left);
        const right = Math.min(b.max, cam.left + cam.size);
        const top = Math.max(b.min, cam.top);
        const bottom = Math.min(b.max, cam.top + cam.size);

        if (left >= right && top >= bottom) {
            return;
        }

        if (cam.top <= b.min && left < right) {
            this._drawFenceLine(ctx, left, b.min, right, b.min, cam, w, h);
        }
        if (cam.top + cam.size >= b.max && left < right) {
            this._drawFenceLine(ctx, left, b.max, right, b.max, cam, w, h);
        }
        if (cam.left <= b.min && top < bottom) {
            this._drawFenceLine(ctx, b.min, top, b.min, bottom, cam, w, h);
        }
        if (cam.left + cam.size >= b.max && top < bottom) {
            this._drawFenceLine(ctx, b.max, top, b.max, bottom, cam, w, h);
        }
    },

    _drawScene(w, h) {
        const ctx = this._ctx;
        const cam = this._getCamera();
        this._lastCamera = cam;

        const groundGrad = ctx.createLinearGradient(0, 0, 0, h);
        groundGrad.addColorStop(0, '#dcb471');
        groundGrad.addColorStop(1, '#b8834a');
        ctx.fillStyle = groundGrad;
        ctx.fillRect(0, 0, w, h);

        this._drawGroundDecor(ctx, cam, w, h);
        this._drawPaths(ctx, cam, w, h);
        this._drawOliShack(ctx, cam, w, h);
        this._drawMapFence(ctx, cam, w, h);

        for (const spot of this._spots) {
            const center = this._worldToScreen(spot.cx, spot.cy, cam, w, h);
            const rx = (spot.rx / cam.size) * w;
            const ry = (spot.ry / cam.size) * h;
            if (center.x + rx < 0 || center.x - rx > w || center.y + ry < 0 || center.y - ry > h) {
                continue;
            }

            ctx.fillStyle = '#6b5334';
            ctx.beginPath();
            ctx.ellipse(center.x, center.y, rx + 10, ry + 10, 0, 0, Math.PI * 2);
            ctx.fill();

            const waterGrad = ctx.createRadialGradient(center.x, center.y, 4, center.x, center.y, Math.max(rx, ry));
            waterGrad.addColorStop(0, '#3a86ab');
            waterGrad.addColorStop(1, '#1c4a5a');
            ctx.fillStyle = waterGrad;
            ctx.beginPath();
            ctx.ellipse(center.x, center.y, rx, ry, 0, 0, Math.PI * 2);
            ctx.fill();

            ctx.save();
            ctx.beginPath();
            ctx.ellipse(center.x, center.y, rx, ry, 0, 0, Math.PI * 2);
            ctx.clip();
            this._drawAmbientRipples(ctx, center.x, center.y, rx, ry);
            ctx.restore();

            this._drawReeds(ctx, center.x, center.y, rx, ry);
            this._drawPondDeco(ctx, spot, center, rx, ry);

            ctx.font = 'bold 10px sans-serif';
            ctx.textAlign = 'center';
            ctx.lineWidth = 3;
            ctx.strokeStyle = 'rgba(0,0,0,0.45)';
            ctx.strokeText(spot.name, center.x, center.y - ry - 12);
            ctx.fillStyle = 'rgba(255,255,255,0.9)';
            ctx.fillText(spot.name, center.x, center.y - ry - 12);
        }

        const entries = [{
            x: this._my.x, y: this._my.y, name: this._my.name, avatar: this._my.avatar,
            casting: this._my.casting, castX: this._my.castX, castY: this._my.castY,
            castStartTime: this._my.castStartTime, hasBite: this._myHasBite, isSelf: true,
            isMoving: !!this._my.moving, bobPhase: 0, twitchUserId: this._myId,
        }];
        for (const [id, r] of this._remote.entries()) {
            entries.push({
                x: r.renderX, y: r.renderY, name: r.name, avatar: r.avatar,
                casting: r.casting, castX: r.castX, castY: r.castY,
                castStartTime: r.castStartTime, hasBite: r.hasBite, isSelf: false,
                isMoving: !!(r.lastMoveTime && performance.now() - r.lastMoveTime < 300),
                bobPhase: ((r.renderX || 0) * 97 + (r.renderY || 0) * 53) % (Math.PI * 2),
                castColor: r.castColor, twitchUserId: id,
            });
        }
        entries.sort((a, b) => a.y - b.y);
        for (const e of entries) {
            const p = this._worldToScreen(e.x, e.y, cam, w, h);
            const cast = this._worldToScreen(e.castX, e.castY, cam, w, h);
            this._drawPlayer(ctx, p.x, p.y, e.name, e.avatar, e.casting, cast.x, cast.y, e.castStartTime, e.hasBite, e.isSelf, e.isMoving, e.bobPhase, e.castColor, e.twitchUserId);
        }
    },

    _roundRect(ctx, x, y, width, height, radius) {
        const r = Math.min(radius, width / 2, height / 2);
        ctx.beginPath();
        ctx.moveTo(x + r, y);
        ctx.arcTo(x + width, y, x + width, y + height, r);
        ctx.arcTo(x + width, y + height, x, y + height, r);
        ctx.arcTo(x, y + height, x, y, r);
        ctx.arcTo(x, y, x + width, y, r);
        ctx.closePath();
    },

    _drawQte(w, h) {
        const ctx = this._ctx;
        const q = this._qte;

        ctx.fillStyle = 'rgba(8,8,10,0.6)';
        ctx.fillRect(0, 0, w, h);

        const barW = 46;
        const barX = w / 2 - barW / 2;
        const barTop = h * 0.14;
        const barH = h * 0.68;

        this._roundRect(ctx, barX, barTop, barW, barH, 10);
        ctx.fillStyle = 'rgba(20,14,10,0.55)';
        ctx.fill();
        ctx.save();
        this._roundRect(ctx, barX, barTop, barW, barH, 10);
        ctx.clip();

        const zoneH = q.zoneHeight * barH;
        const zoneY = barTop + (1 - q.barPos) * barH - zoneH / 2;
        const zoneGrad = ctx.createLinearGradient(0, zoneY, 0, zoneY + zoneH);
        zoneGrad.addColorStop(0, 'rgba(143,194,103,0.35)');
        zoneGrad.addColorStop(0.5, 'rgba(143,194,103,0.8)');
        zoneGrad.addColorStop(1, 'rgba(143,194,103,0.35)');
        ctx.fillStyle = zoneGrad;
        ctx.fillRect(barX, zoneY, barW, zoneH);
        ctx.restore();

        this._roundRect(ctx, barX, barTop, barW, barH, 10);
        ctx.strokeStyle = 'rgba(244,197,66,0.5)';
        ctx.lineWidth = 1.5;
        ctx.stroke();

        const fishY = barTop + q.fishPos * barH;
        ctx.save();
        ctx.shadowColor = '#f4c542';
        ctx.shadowBlur = 10;
        ctx.beginPath();
        ctx.fillStyle = '#f4c542';
        ctx.arc(w / 2, fishY, 7, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
        ctx.strokeStyle = '#8a5a2e';
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.arc(w / 2, fishY, 7, 0, Math.PI * 2);
        ctx.stroke();

        const progW = 14;
        const progX = barX + barW + 16;
        this._roundRect(ctx, progX, barTop, progW, barH, 6);
        ctx.fillStyle = 'rgba(255,255,255,0.15)';
        ctx.fill();

        const filledH = q.progress * barH;
        ctx.save();
        this._roundRect(ctx, progX, barTop, progW, barH, 6);
        ctx.clip();
        const progColor = q.progress > 0.66 ? '#8fc267' : q.progress > 0.33 ? '#f4c542' : '#e2503f';
        ctx.fillStyle = progColor;
        ctx.fillRect(progX, barTop + barH - filledH, progW, filledH);
        ctx.restore();

        const elapsed = performance.now() - q.startTime;
        const timeLeft = Math.max(0, 1 - elapsed / q.duration);
        const timeBarW = barW;
        this._roundRect(ctx, barX, barTop - 10, timeBarW, 4, 2);
        ctx.fillStyle = 'rgba(255,255,255,0.15)';
        ctx.fill();
        this._roundRect(ctx, barX, barTop - 10, timeBarW * timeLeft, 4, 2);
        ctx.fillStyle = '#f4c542';
        ctx.fill();

        ctx.fillStyle = '#f4c542';
        ctx.font = 'bold 12px sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText('Halten zum Anziehen!', w / 2, barTop - 18);
    },

    _updateMovement(dt) {
        let dx = 0;
        let dy = 0;
        if (this._keys.w) dy -= 1;
        if (this._keys.s) dy += 1;
        if (this._keys.a) dx -= 1;
        if (this._keys.d) dx += 1;

        if (this._joystickVector) {
            dx += this._joystickVector.x;
            dy += this._joystickVector.y;
        }

        this._my.moving = dx !== 0 || dy !== 0;
        if (dx === 0 && dy === 0) {
            return;
        }

        const len = Math.max(1, Math.hypot(dx, dy));
        dx /= len;
        dy /= len;
        const step = this._moveSpeed * (dt / 1000);

        const tryMove = (nx, ny) => {
            const cx = Math.max(this._mapBounds.min, Math.min(this._mapBounds.max, nx));
            const cy = Math.max(this._mapBounds.min, Math.min(this._mapBounds.max, ny));
            if (this._isBlocked(cx, cy)) {
                return false;
            }
            this._my.x = cx;
            this._my.y = cy;
            return true;
        };

        const targetX = this._my.x + dx * step;
        const targetY = this._my.y + dy * step;
        if (!tryMove(targetX, targetY)) {
            if (!tryMove(targetX, this._my.y)) {
                tryMove(this._my.x, targetY);
            }
        }

        const now = performance.now();
        if (now - this._lastSyncSent > 120) {
            this._lastSyncSent = now;
            this._dotNetRef.invokeMethodAsync('OnLocalMove', this._my.x, this._my.y).catch(() => {});
        }
    },

    _loop(now) {
        if (!this._canvas) {
            return;
        }

        const dt = Math.min(64, now - (this._lastFrame || now));
        this._lastFrame = now;

        if (this._my.casting && !this._qte && this._my.castStartTime && now - this._my.castStartTime > 40000) {
            this._my.casting = false;
            this._my.castStartTime = 0;
            if (this._dotNetRef) {
                this._dotNetRef.invokeMethodAsync('OnCastWatchdogTimeout').catch(() => {});
            }
        }

        if (!this._my.casting) {
            this._updateMovement(dt);
        }

        if (now - this._lastHeartbeat > 15000) {
            this._lastHeartbeat = now;
            this._dotNetRef.invokeMethodAsync('OnLocalMove', this._my.x, this._my.y).catch(() => {});
        }

        for (const r of this._remote.values()) {
            r.renderX += (r.x - r.renderX) * Math.min(1, dt / 180);
            r.renderY += (r.y - r.renderY) * Math.min(1, dt / 180);
        }

        const w = this._canvas.width;
        const h = this._canvas.height;
        this._drawScene(w, h);

        if (this._qte) {
            this._updateQte(dt);
            if (this._qte) {
                this._drawQte(w, h);
            }
        }

        this._frameId = requestAnimationFrame((t) => this._loop(t));
    },

    notifyBite() {
        this._myHasBite = true;
    },

    clearBite() {
        this._myHasBite = false;
    },

    destroy() {
        if (this._frameId !== null) {
            cancelAnimationFrame(this._frameId);
            this._frameId = null;
        }
        if (this._resizeHandler) {
            window.removeEventListener('resize', this._resizeHandler);
            this._resizeHandler = null;
        }
        if (this._keydownHandler) {
            window.removeEventListener('keydown', this._keydownHandler);
            this._keydownHandler = null;
        }
        if (this._keyupHandler) {
            window.removeEventListener('keyup', this._keyupHandler);
            this._keyupHandler = null;
        }
        if (this._canvas) {
            this._canvas.removeEventListener('click', this._canvasClickHandler);
            this._canvas.removeEventListener('mousedown', this._canvasMouseDownHandler);
            this._canvas.removeEventListener('mouseup', this._canvasMouseUpHandler);
            this._canvas.removeEventListener('touchstart', this._canvasTouchStartHandler);
            this._canvas.removeEventListener('touchend', this._canvasTouchEndHandler);
        }
        this._canvasClickHandler = null;
        this._canvasMouseDownHandler = null;
        this._canvasMouseUpHandler = null;
        this._canvasTouchStartHandler = null;
        this._canvasTouchEndHandler = null;
        if (this._joystickBase) {
            this._joystickBase.removeEventListener('pointerdown', this._joystickDownHandler);
            this._joystickBase.removeEventListener('pointermove', this._joystickMoveHandler);
            this._joystickBase.removeEventListener('pointerup', this._joystickUpHandler);
            this._joystickBase.removeEventListener('pointercancel', this._joystickUpHandler);
            this._joystickBase = null;
        }
        this._joystickVector = { x: 0, y: 0 };
        this._joystickPointerId = null;
        this._canvas = null;
        this._ctx = null;
        this._dotNetRef = null;
        this._remote = new Map();
        this._qte = null;
        this._myId = null;
        this._my = { x: 0.5, y: 0.14, name: '', avatar: null, casting: false, castX: 0, castY: 0 };
        this._myHasBite = false;
        this._chatBubbles = new Map();
        this._lastCamera = null;
    },
};
