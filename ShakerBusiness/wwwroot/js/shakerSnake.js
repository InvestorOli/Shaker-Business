window.shakerSnake = {
    _state: null,

    init(canvasId, oliImagePath, businessImagePaths, initialBest, dotNetRef) {
        this.destroy();

        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const size = Math.max(1, Math.round(canvas.clientWidth));
        canvas.width = size;
        canvas.height = size;

        const ctx = canvas.getContext('2d');
        const gridSize = 15;
        const cellSize = size / gridSize;
        const TICK_MS = 150;

        const scoreEl = document.getElementById('shaker-snake-score');
        const bestEl = document.getElementById('shaker-snake-best');
        const updateHud = () => {
            if (scoreEl) { scoreEl.textContent = `Score: ${score}`; }
            if (bestEl) { bestEl.textContent = `Best: ${best}`; }
        };

        const oliImg = new Image();
        oliImg.src = oliImagePath;

        const bodyImages = businessImagePaths.map((src) => {
            const img = new Image();
            img.src = src;
            return img;
        });

        let snake = [];
        let segmentImages = [];
        let direction = { x: 1, y: 0 };
        let nextDirection = { x: 1, y: 0 };
        let food = null;
        let foodImage = null;
        let score = 0;
        let best = initialBest || 0;
        let gameOver = false;
        let started = false;
        let prevSnakePositions = [];
        let lastTickTime = performance.now();

        const randomEmptyCell = () => {
            let cell;
            do {
                cell = { x: Math.floor(Math.random() * gridSize), y: Math.floor(Math.random() * gridSize) };
            } while (snake.some((s) => s.x === cell.x && s.y === cell.y));
            return cell;
        };

        const spawnFood = () => {
            food = randomEmptyCell();
            foodImage = bodyImages[Math.floor(Math.random() * bodyImages.length)];
        };

        const snapshotPrev = () => {
            prevSnakePositions = snake.map((s) => ({ x: s.x, y: s.y }));
        };

        const reset = () => {
            snake = [{ x: 7, y: 7 }, { x: 6, y: 7 }, { x: 5, y: 7 }];
            segmentImages = [bodyImages[0], bodyImages[0]];
            direction = { x: 1, y: 0 };
            nextDirection = { x: 1, y: 0 };
            score = 0;
            gameOver = false;
            spawnFood();
            snapshotPrev();
            lastTickTime = performance.now();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnSnakeRunStarted').catch(() => {});
            }
        };

        const getRenderPositions = () => {
            if (gameOver || !started) {
                return snake;
            }

            const t = Math.min(1, (performance.now() - lastTickTime) / TICK_MS);
            return snake.map((seg, i) => {
                const prev = prevSnakePositions[i];
                if (!prev) {
                    return seg;
                }

                return { x: prev.x + (seg.x - prev.x) * t, y: prev.y + (seg.y - prev.y) * t };
            });
        };

        const draw = () => {
            ctx.fillStyle = '#cbb99f';
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            ctx.strokeStyle = 'rgba(0,0,0,0.05)';
            for (let i = 1; i < gridSize; i++) {
                ctx.beginPath();
                ctx.moveTo(i * cellSize, 0);
                ctx.lineTo(i * cellSize, canvas.height);
                ctx.stroke();
                ctx.beginPath();
                ctx.moveTo(0, i * cellSize);
                ctx.lineTo(canvas.width, i * cellSize);
                ctx.stroke();
            }

            if (food) {
                if (foodImage && foodImage.complete && foodImage.naturalWidth > 0) {
                    ctx.save();
                    ctx.beginPath();
                    ctx.arc((food.x + 0.5) * cellSize, (food.y + 0.5) * cellSize, cellSize * 0.46, 0, Math.PI * 2);
                    ctx.clip();
                    ctx.drawImage(foodImage, food.x * cellSize, food.y * cellSize, cellSize, cellSize);
                    ctx.restore();
                    ctx.beginPath();
                    ctx.strokeStyle = '#f4c542';
                    ctx.lineWidth = 2;
                    ctx.arc((food.x + 0.5) * cellSize, (food.y + 0.5) * cellSize, cellSize * 0.46, 0, Math.PI * 2);
                    ctx.stroke();
                } else {
                    ctx.beginPath();
                    ctx.fillStyle = '#f4c542';
                    ctx.arc((food.x + 0.5) * cellSize, (food.y + 0.5) * cellSize, cellSize * 0.32, 0, Math.PI * 2);
                    ctx.fill();
                    ctx.strokeStyle = '#8a6d1f';
                    ctx.lineWidth = 1.5;
                    ctx.stroke();
                }
            }

            const renderPositions = getRenderPositions();

            for (let i = renderPositions.length - 1; i >= 1; i--) {
                const seg = renderPositions[i];
                const img = segmentImages[i - 1];
                if (img && img.complete && img.naturalWidth > 0) {
                    ctx.drawImage(img, seg.x * cellSize, seg.y * cellSize, cellSize, cellSize);
                } else {
                    ctx.fillStyle = '#7a9c52';
                    ctx.fillRect(seg.x * cellSize + 1, seg.y * cellSize + 1, cellSize - 2, cellSize - 2);
                }
            }

            const head = renderPositions[0];
            if (oliImg.complete && oliImg.naturalWidth > 0) {
                ctx.save();
                ctx.beginPath();
                ctx.arc((head.x + 0.5) * cellSize, (head.y + 0.5) * cellSize, cellSize / 2, 0, Math.PI * 2);
                ctx.clip();
                ctx.drawImage(oliImg, head.x * cellSize, head.y * cellSize, cellSize, cellSize);
                ctx.restore();
            }

            updateHud();

            if (gameOver) {
                ctx.fillStyle = 'rgba(30,18,10,0.72)';
                ctx.fillRect(0, 0, canvas.width, canvas.height);
                ctx.fillStyle = '#f4c542';
                ctx.textAlign = 'center';
                ctx.font = 'bold 22px sans-serif';
                ctx.fillText('Game Over', canvas.width / 2, canvas.height / 2 - 14);
                ctx.font = 'bold 14px sans-serif';
                ctx.fillStyle = '#f5ead9';
                ctx.fillText(`Score: ${score}`, canvas.width / 2, canvas.height / 2 + 10);
                ctx.font = '12px sans-serif';
                ctx.fillText('Tippen zum Neustart', canvas.width / 2, canvas.height / 2 + 30);
                ctx.textAlign = 'left';
            } else if (!started) {
                ctx.fillStyle = 'rgba(30,18,10,0.72)';
                ctx.fillRect(0, 0, canvas.width, canvas.height);
                ctx.textAlign = 'center';
                ctx.fillStyle = '#f4c542';
                ctx.font = 'bold 20px sans-serif';
                ctx.fillText('ShakerSnake', canvas.width / 2, canvas.height / 2 - 36);

                const cx = canvas.width / 2;
                const cy = canvas.height / 2 + 6;
                const r = 24;
                ctx.beginPath();
                ctx.moveTo(cx - r / 2, cy - r);
                ctx.lineTo(cx - r / 2, cy + r);
                ctx.lineTo(cx + r * 0.85, cy);
                ctx.closePath();
                ctx.fillStyle = '#7a9c52';
                ctx.fill();

                ctx.font = '12px sans-serif';
                ctx.fillStyle = '#f5ead9';
                ctx.fillText('Tippen zum Start', cx, cy + r + 22);
                ctx.textAlign = 'left';
            }
        };

        const tick = () => {
            if (gameOver || !started) {
                return;
            }

            snapshotPrev();

            direction = nextDirection;
            const head = { x: snake[0].x + direction.x, y: snake[0].y + direction.y };

            const hitsWall = head.x < 0 || head.x >= gridSize || head.y < 0 || head.y >= gridSize;
            const hitsSelf = snake.some((s) => s.x === head.x && s.y === head.y);

            if (hitsWall || hitsSelf) {
                gameOver = true;
                if (score > best) {
                    best = score;
                }
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnSnakeGameOver').catch(() => {});
                }
                return;
            }

            snake.unshift(head);

            if (food && head.x === food.x && head.y === food.y) {
                score++;
                segmentImages.unshift(bodyImages[Math.floor(Math.random() * bodyImages.length)]);
                spawnFood();
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnSnakeFoodEaten').catch(() => {});
                }
            } else {
                snake.pop();
            }

            lastTickTime = performance.now();
        };

        const renderState = { id: null };
        const renderLoop = () => {
            draw();
            if (started && !gameOver) {
                renderState.id = requestAnimationFrame(renderLoop);
            } else {
                renderState.id = null;
            }
        };
        const ensureRenderLoop = () => {
            if (renderState.id === null) {
                renderLoop();
            }
        };

        const setDirection = (dx, dy) => {
            if (gameOver) {
                return;
            }

            if (!started) {
                started = true;
                ensureRenderLoop();
            }

            if (snake.length > 1 && direction.x === -dx && direction.y === -dy) {
                return;
            }

            nextDirection = { x: dx, y: dy };
        };

        const keyHandler = (e) => {
            switch (e.key) {
                case 'ArrowUp':
                case 'w':
                case 'W':
                    setDirection(0, -1);
                    e.preventDefault();
                    break;
                case 'ArrowDown':
                case 's':
                case 'S':
                    setDirection(0, 1);
                    e.preventDefault();
                    break;
                case 'ArrowLeft':
                case 'a':
                case 'A':
                    setDirection(-1, 0);
                    e.preventDefault();
                    break;
                case 'ArrowRight':
                case 'd':
                case 'D':
                    setDirection(1, 0);
                    e.preventDefault();
                    break;
            }
        };

        const clickHandler = () => {
            if (gameOver) {
                reset();
                ensureRenderLoop();
            } else if (!started) {
                started = true;
                ensureRenderLoop();
            }
        };

        document.addEventListener('keydown', keyHandler);
        canvas.addEventListener('click', clickHandler);

        reset();
        ensureRenderLoop();
        const intervalId = setInterval(tick, TICK_MS);

        this._state = { intervalId, keyHandler, canvas, clickHandler, renderState };
    },

    destroy() {
        if (this._state) {
            clearInterval(this._state.intervalId);
            if (this._state.renderState && this._state.renderState.id !== null) {
                cancelAnimationFrame(this._state.renderState.id);
            }
            document.removeEventListener('keydown', this._state.keyHandler);
            this._state.canvas.removeEventListener('click', this._state.clickHandler);
            this._state = null;
        }
    },
};
