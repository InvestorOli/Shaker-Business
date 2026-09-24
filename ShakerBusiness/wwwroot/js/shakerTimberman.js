window.shakerTimberman = {
    _state: null,

    init(canvasId, playerImagePath, bestScore, dotNetRef) {
        this.destroy();

        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const width = Math.max(1, Math.round(canvas.clientWidth));
        const height = Math.max(1, Math.round(canvas.clientHeight));
        canvas.width = width;
        canvas.height = height;

        const ctx = canvas.getContext('2d');

        const VISIBLE_SEGMENTS = 7;
        const GROUND_HEIGHT = height * 0.08;
        const SEGMENT_HEIGHT = (height - GROUND_HEIGHT) / VISIBLE_SEGMENTS;
        const TRUNK_WIDTH = width * 0.26;
        const TRUNK_X = (width - TRUNK_WIDTH) / 2;
        const BRANCH_REACH = width * 0.24;
        const BRANCH_SCALE = SEGMENT_HEIGHT * 0.6;
        const SHIFT_DURATION = 0.11;
        const TIMER_MAX = 13;
        const TIMER_REFILL = 0.6;
        const LEVEL_UP_EVERY = 8;
        const DRAIN_RATE_PER_LEVEL = 0.30;
        const MAX_DRAIN_RATE = 4;
        const CHOP_ANIM_DURATION = 0.16;
        const MIN_BRANCH_CHANCE = 0.55;
        const MAX_BRANCH_CHANCE = 0.92;
        const BRANCH_CHANCE_RAMP_SCORE = 30;
        const SAFE_START_SEGMENTS = 6;
        const TIMER_BAR_WIDTH = width * 0.64;
        const TIMER_BAR_X = (width - TIMER_BAR_WIDTH) / 2;
        const TIMER_BAR_Y = height * 0.025;
        const TIMER_BAR_HEIGHT = Math.max(16, height * 0.032);

        const playerImg = new Image();
        playerImg.src = playerImagePath;

        const skyGradient = ctx.createLinearGradient(0, 0, 0, height);
        skyGradient.addColorStop(0, '#f0c6c6');
        skyGradient.addColorStop(0.45, '#f0b98a');
        skyGradient.addColorStop(1, '#f7dca3');

        const trunkGradient = ctx.createLinearGradient(TRUNK_X, 0, TRUNK_X + TRUNK_WIDTH, 0);
        trunkGradient.addColorStop(0, '#a5713c');
        trunkGradient.addColorStop(0.5, '#8a5a2e');
        trunkGradient.addColorStop(1, '#6b4423');

        const timerFillGradient = ctx.createLinearGradient(TIMER_BAR_X, 0, TIMER_BAR_X + TIMER_BAR_WIDTH, 0);
        timerFillGradient.addColorStop(0, '#e05a3a');
        timerFillGradient.addColorStop(1, '#c1382a');

        const bgStripes = [];
        const stripeCount = 8;
        for (let i = 0; i < stripeCount; i++) {
            const bandW = width / stripeCount;
            bgStripes.push({
                x: (i * bandW) + (bandW * 0.15),
                w: bandW * (0.4 + Math.random() * 0.3),
            });
        }

        let segments = [];
        let side = 'left';
        let score = 0;
        let best = 0;
        let timer = TIMER_MAX;
        let started = false;
        let ended = false;
        let shiftProgress = 1;
        let chopAnimElapsed = CHOP_ANIM_DURATION;
        let lastFrameTime = performance.now();

        const branchChanceForScore = (s) => Math.min(MAX_BRANCH_CHANCE, MIN_BRANCH_CHANCE + (s / BRANCH_CHANCE_RAMP_SCORE) * (MAX_BRANCH_CHANCE - MIN_BRANCH_CHANCE));

        const randomGrain = () => [0.22 + Math.random() * 0.1, 0.5 + Math.random() * 0.12, 0.78 + Math.random() * 0.1];

        const randomSegment = (allowBranch, previousBranch) => {
            const grain = randomGrain();
            if (!allowBranch || previousBranch !== 'none' || Math.random() > branchChanceForScore(score)) {
                return { branch: 'none', grain };
            }
            return { branch: Math.random() < 0.5 ? 'left' : 'right', grain };
        };

        const reset = () => {
            segments = [];
            let previousBranch = 'none';
            for (let i = 0; i < VISIBLE_SEGMENTS + 1; i++) {
                const segment = randomSegment(i >= SAFE_START_SEGMENTS, previousBranch);
                segments.push(segment);
                previousBranch = segment.branch;
            }

            side = 'left';
            score = 0;
            timer = TIMER_MAX;
            started = false;
            ended = false;
            shiftProgress = 1;
            chopAnimElapsed = CHOP_ANIM_DURATION;
            lastFrameTime = performance.now();
        };

        const chop = (chosenSide) => {
            if (ended) {
                reset();
                return;
            }

            if (!started) {
                started = true;
                side = chosenSide;
                dotNetRef.invokeMethodAsync('OnTimberRunStarted').catch(() => {});
                return;
            }

            side = chosenSide;
            chopAnimElapsed = 0;

            if (segments[0].branch !== 'none' && segments[0].branch === side) {
                ended = true;
                window.shakerAudio?.playTimberDeath();
                dotNetRef.invokeMethodAsync('OnTimberGameOver').catch(() => {});
                return;
            }

            const previousTopBranch = segments[segments.length - 1].branch;
            segments.shift();
            segments.push(randomSegment(true, previousTopBranch));
            shiftProgress = 0;

            const newBottom = segments[0];
            if (newBottom.branch !== 'none' && newBottom.branch === side) {
                ended = true;
                window.shakerAudio?.playTimberDeath();
                dotNetRef.invokeMethodAsync('OnTimberGameOver').catch(() => {});
                return;
            }

            score++;
            timer = Math.min(TIMER_MAX, timer + TIMER_REFILL);
            dotNetRef.invokeMethodAsync('OnTimberChopScored').catch(() => {});

            if (score > best) {
                best = score;
            }
        };

        const drainRateForScore = (s) => Math.min(MAX_DRAIN_RATE, 1 + (Math.floor(s / LEVEL_UP_EVERY) * DRAIN_RATE_PER_LEVEL));

        const update = (dt) => {
            if (shiftProgress < 1) {
                shiftProgress = Math.min(1, shiftProgress + dt / SHIFT_DURATION);
            }

            if (chopAnimElapsed < CHOP_ANIM_DURATION) {
                chopAnimElapsed = Math.min(CHOP_ANIM_DURATION, chopAnimElapsed + dt);
            }

            if (!started || ended) {
                return;
            }

            timer -= dt * drainRateForScore(score);
            if (timer <= 0) {
                timer = 0;
                ended = true;
                window.shakerAudio?.playTimberDeath();
                dotNetRef.invokeMethodAsync('OnTimberGameOver').catch(() => {});
            }
        };

        const roundRectPath = (x, y, w, h, r) => {
            ctx.beginPath();
            ctx.moveTo(x + r, y);
            ctx.arcTo(x + w, y, x + w, y + h, r);
            ctx.arcTo(x + w, y + h, x, y + h, r);
            ctx.arcTo(x, y + h, x, y, r);
            ctx.arcTo(x, y, x + w, y, r);
            ctx.closePath();
        };

        const drawBackground = () => {
            ctx.fillStyle = skyGradient;
            ctx.fillRect(0, 0, width, height);

            ctx.fillStyle = 'rgba(107,58,38,0.3)';
            for (const s of bgStripes) {
                ctx.fillRect(s.x, 0, s.w, height - GROUND_HEIGHT);
            }

            ctx.fillStyle = '#cfa25f';
            ctx.fillRect(0, height - GROUND_HEIGHT, width, GROUND_HEIGHT);
            ctx.strokeStyle = 'rgba(0,0,0,0.15)';
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.moveTo(0, height - GROUND_HEIGHT);
            ctx.lineTo(width, height - GROUND_HEIGHT);
            ctx.stroke();
        };

        const drawBranch = (branchSide, centerY) => {
            const dir = branchSide === 'left' ? -1 : 1;
            const stubX = branchSide === 'left' ? TRUNK_X : TRUNK_X + TRUNK_WIDTH;
            const stubLen = BRANCH_REACH * 0.45;

            ctx.strokeStyle = '#6b4423';
            ctx.lineWidth = 6;
            ctx.beginPath();
            ctx.moveTo(stubX, centerY);
            ctx.lineTo(stubX + (dir * stubLen), centerY);
            ctx.stroke();

            const leafW = BRANCH_SCALE * 0.95;
            const leafH = BRANCH_SCALE * 0.7;
            const leafX = stubX + (dir * (stubLen + leafW * 0.35));

            ctx.fillStyle = '#6b9c4a';
            roundRectPath(leafX - (dir > 0 ? 0 : leafW), centerY - leafH / 2, leafW, leafH, leafH * 0.35);
            ctx.fill();

            ctx.strokeStyle = '#4c6b2a';
            ctx.lineWidth = 2;
            roundRectPath(leafX - (dir > 0 ? 0 : leafW), centerY - leafH / 2, leafW, leafH, leafH * 0.35);
            ctx.stroke();
        };

        const drawSegment = (segment, y, isActive) => {
            ctx.fillStyle = trunkGradient;
            ctx.fillRect(TRUNK_X, y, TRUNK_WIDTH, SEGMENT_HEIGHT + 2);

            ctx.strokeStyle = 'rgba(0,0,0,0.22)';
            ctx.lineWidth = 2;
            for (const g of segment.grain) {
                ctx.beginPath();
                ctx.moveTo(TRUNK_X + (TRUNK_WIDTH * g), y);
                ctx.lineTo(TRUNK_X + (TRUNK_WIDTH * g), y + SEGMENT_HEIGHT + 2);
                ctx.stroke();
            }

            ctx.strokeStyle = '#4a2f18';
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.moveTo(TRUNK_X, y);
            ctx.lineTo(TRUNK_X + TRUNK_WIDTH, y);
            ctx.stroke();

            if (segment.branch !== 'none') {
                drawBranch(segment.branch, y + (SEGMENT_HEIGHT / 2));
            }

            if (isActive) {
                ctx.strokeStyle = '#f4c542';
                ctx.lineWidth = 3;
                ctx.strokeRect(TRUNK_X - 3, y - 3, TRUNK_WIDTH + 6, SEGMENT_HEIGHT + 8);
            }
        };

        const drawPlayer = () => {
            const groundY = height - GROUND_HEIGHT;
            const playerSize = SEGMENT_HEIGHT * 0.95;
            const playerCenterX = side === 'left' ? TRUNK_X - (playerSize * 0.75) : TRUNK_X + TRUNK_WIDTH + (playerSize * 0.75);
            const playerTopY = groundY - playerSize;

            ctx.save();
            ctx.beginPath();
            ctx.ellipse(playerCenterX, groundY + 3, playerSize * 0.42, playerSize * 0.11, 0, 0, Math.PI * 2);
            ctx.fillStyle = 'rgba(0,0,0,0.25)';
            ctx.fill();
            ctx.restore();

            const cornerRadius = playerSize * 0.14;
            if (playerImg.complete && playerImg.naturalWidth > 0) {
                ctx.save();
                roundRectPath(playerCenterX - (playerSize / 2), playerTopY, playerSize, playerSize, cornerRadius);
                ctx.clip();
                ctx.drawImage(playerImg, playerCenterX - (playerSize / 2), playerTopY, playerSize, playerSize);
                ctx.restore();
            } else {
                ctx.fillStyle = '#d9a441';
                roundRectPath(playerCenterX - (playerSize / 2), playerTopY, playerSize, playerSize, cornerRadius);
                ctx.fill();
            }

            ctx.strokeStyle = '#f4c542';
            ctx.lineWidth = 2.5;
            roundRectPath(playerCenterX - (playerSize / 2), playerTopY, playerSize, playerSize, cornerRadius);
            ctx.stroke();

            const swingT = Math.min(1, chopAnimElapsed / CHOP_ANIM_DURATION);
            const swing = Math.sin(swingT * Math.PI);
            const axeDir = side === 'left' ? 1 : -1;
            ctx.save();
            ctx.translate(playerCenterX + (axeDir * playerSize * 0.72), playerTopY + (playerSize * (0.3 - swing * 0.14)));
            ctx.scale(axeDir, 1);
            ctx.rotate(-0.15 - swing * 0.85);
            ctx.fillStyle = '#8a5a2e';
            ctx.fillRect(-2, -playerSize * 0.42, 5, playerSize * 0.62);
            ctx.fillStyle = '#9aa5ad';
            ctx.beginPath();
            ctx.moveTo(3, -playerSize * 0.42);
            ctx.lineTo(3 + (playerSize * 0.26), -playerSize * 0.33);
            ctx.lineTo(3, -playerSize * 0.22);
            ctx.closePath();
            ctx.fill();
            ctx.strokeStyle = '#5c3a1a';
            ctx.lineWidth = 1.5;
            ctx.stroke();
            ctx.restore();
        };

        const drawTimerBar = () => {
            ctx.fillStyle = '#241a12';
            roundRectPath(TIMER_BAR_X, TIMER_BAR_Y, TIMER_BAR_WIDTH, TIMER_BAR_HEIGHT, TIMER_BAR_HEIGHT / 2);
            ctx.fill();

            const pct = Math.max(0, timer / TIMER_MAX);
            if (pct > 0) {
                ctx.fillStyle = timerFillGradient;
                roundRectPath(TIMER_BAR_X + 3, TIMER_BAR_Y + 3, Math.max(0, (TIMER_BAR_WIDTH - 6) * pct), TIMER_BAR_HEIGHT - 6, (TIMER_BAR_HEIGHT - 6) / 2);
                ctx.fill();
            }

            ctx.strokeStyle = '#f4c542';
            ctx.lineWidth = 3;
            roundRectPath(TIMER_BAR_X, TIMER_BAR_Y, TIMER_BAR_WIDTH, TIMER_BAR_HEIGHT, TIMER_BAR_HEIGHT / 2);
            ctx.stroke();
        };

        const drawScore = () => {
            ctx.textAlign = 'center';
            const scoreY = height * 0.34;

            ctx.font = `bold ${Math.round(width * 0.11)}px sans-serif`;
            ctx.lineWidth = 6;
            ctx.strokeStyle = '#2e2119';
            ctx.strokeText(String(score), width / 2, scoreY);
            ctx.fillStyle = '#f5ead9';
            ctx.fillText(String(score), width / 2, scoreY);

            ctx.font = `bold ${Math.round(width * 0.034)}px sans-serif`;
            ctx.lineWidth = 3;
            ctx.strokeStyle = '#2e2119';
            ctx.strokeText(`Best: ${best}`, width / 2, scoreY + (width * 0.08));
            ctx.fillStyle = 'rgba(245,234,217,0.85)';
            ctx.fillText(`Best: ${best}`, width / 2, scoreY + (width * 0.08));

            ctx.textAlign = 'left';
        };

        const draw = () => {
            drawBackground();

            const offsetY = (1 - shiftProgress) * -SEGMENT_HEIGHT;
            for (let i = 0; i < segments.length; i++) {
                const y = (height - GROUND_HEIGHT) - ((i + 1) * SEGMENT_HEIGHT) + offsetY;
                if (y > -SEGMENT_HEIGHT * 2 && y < height + SEGMENT_HEIGHT) {
                    drawSegment(segments[i], y, i === 0 && shiftProgress >= 1);
                }
            }

            drawPlayer();
            drawTimerBar();
            drawScore();

            if (!started && !ended) {
                ctx.fillStyle = 'rgba(28,20,16,0.72)';
                ctx.fillRect(0, 0, width, height);
                ctx.fillStyle = '#f4c542';
                ctx.textAlign = 'center';
                ctx.font = 'bold 20px sans-serif';
                ctx.fillText('ShakerTimber', width / 2, height / 2 - 30);
                ctx.fillStyle = '#f5ead9';
                ctx.font = '12px sans-serif';
                ctx.fillText('Links/Rechts tippen oder Pfeiltasten', width / 2, height / 2);
                ctx.fillText('Nie in einen Ast hacken!', width / 2, height / 2 + 20);
                ctx.textAlign = 'left';
            } else if (ended) {
                ctx.fillStyle = 'rgba(28,20,16,0.78)';
                ctx.fillRect(0, 0, width, height);
                ctx.textAlign = 'center';
                ctx.fillStyle = '#9c3232';
                ctx.font = 'bold 22px sans-serif';
                ctx.fillText('Game Over!', width / 2, height / 2 - 20);
                ctx.fillStyle = '#f5ead9';
                ctx.font = '14px sans-serif';
                ctx.fillText(`Stämme: ${score}`, width / 2, height / 2 + 8);
                ctx.fillStyle = '#d9a441';
                ctx.font = '12px sans-serif';
                ctx.fillText('Tippen zum Neustart', width / 2, height / 2 + 32);
                ctx.textAlign = 'left';
            }
        };

        const loop = () => {
            const now = performance.now();
            const dt = Math.min(0.05, (now - lastFrameTime) / 1000);
            lastFrameTime = now;

            update(dt);
            draw();

            renderState.id = requestAnimationFrame(loop);
        };

        const renderState = { id: null };

        const keyHandler = (e) => {
            if (e.key === 'ArrowLeft' || e.key.toLowerCase() === 'a') {
                chop('left');
                e.preventDefault();
            } else if (e.key === 'ArrowRight' || e.key.toLowerCase() === 'd') {
                chop('right');
                e.preventDefault();
            }
        };

        const clickHandler = (e) => {
            const rect = canvas.getBoundingClientRect();
            const x = e.clientX - rect.left;
            chop(x < rect.width / 2 ? 'left' : 'right');
        };

        document.addEventListener('keydown', keyHandler);
        canvas.addEventListener('click', clickHandler);

        best = bestScore || 0;
        reset();
        renderState.id = requestAnimationFrame(loop);

        this._state = { keyHandler, canvas, clickHandler, renderState };
    },

    destroy() {
        if (this._state) {
            if (this._state.renderState && this._state.renderState.id !== null) {
                cancelAnimationFrame(this._state.renderState.id);
            }
            document.removeEventListener('keydown', this._state.keyHandler);
            this._state.canvas.removeEventListener('click', this._state.clickHandler);
            this._state = null;
        }
    },
};
