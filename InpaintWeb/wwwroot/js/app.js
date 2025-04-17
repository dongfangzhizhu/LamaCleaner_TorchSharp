let canvas = document.getElementById('imageCanvas');
let ctx = canvas.getContext('2d');
let currentImage = null;
let isDrawing = false;
let brushSize = 20;
let isEraser = false;
let maskCanvas = document.createElement('canvas');
let maskCtx = maskCanvas.getContext('2d');
let lastX = 0;
let lastY = 0;

// 初始化画笔大小控制
const brushSizeRange = document.getElementById('brushSizeRange');
const brushSizeDisplay = document.getElementById('brushSize');
const decreaseBrush = document.getElementById('decreaseBrush');
const increaseBrush = document.getElementById('increaseBrush');
const clearBtn = document.getElementById('clearBtn');
const eraserBtn = document.getElementById('eraserBtn');
const saveBtn = document.getElementById('saveBtn');
const uploadMaskBtn = document.getElementById('uploadMaskBtn');
const maskFileInput = document.getElementById('maskFileInput');

brushSizeRange.addEventListener('input', (e) => {
    brushSize = parseInt(e.target.value);
    brushSizeDisplay.textContent = brushSize;
});

decreaseBrush.addEventListener('click', () => {
    brushSize = Math.max(1, brushSize - 5);
    brushSizeRange.value = brushSize;
    brushSizeDisplay.textContent = brushSize;
});

increaseBrush.addEventListener('click', () => {
    brushSize = Math.min(100, brushSize + 5);
    brushSizeRange.value = brushSize;
    brushSizeDisplay.textContent = brushSize;
});

clearBtn.addEventListener('click', () => {
    if (currentImage) {
        currentImage = null;
        canvas.width = 0;
        canvas.height = 0;
        maskCanvas.width = 0;
        maskCanvas.height = 0;
        canvas.style.display = 'none';
        document.getElementById('uploadText').style.display = 'block';
        uploadMaskBtn.disabled = true;
        maskFileInput.value = '';
    }
});

eraserBtn.addEventListener('click', () => {
    isEraser = !isEraser;
    eraserBtn.textContent = isEraser ? '画笔 (E)' : '橡皮擦 (E)';
    eraserBtn.classList.toggle('active');
});

saveBtn.addEventListener('click', () => {
    if (!currentImage) return;
    const link = document.createElement('a');
    link.download = 'edited_image.png';
    link.href = canvas.toDataURL('image/png');
    link.click();
});

uploadMaskBtn.addEventListener('click', () => {
    if (currentImage) {
        maskFileInput.click();
    }
});

// 初始状态下禁用上传遮罩按钮
uploadMaskBtn.disabled = true;

maskFileInput.addEventListener('change', (e) => {
    if (!currentImage || !e.target.files[0]) return;
    const reader = new FileReader();
    reader.onload = (event) => {
        const img = new Image();
        img.onload = () => {
            maskCtx.clearRect(0, 0, maskCanvas.width, maskCanvas.height);
            maskCtx.drawImage(img, 0, 0, maskCanvas.width, maskCanvas.height);
            updateCanvas();
        };
        img.src = event.target.result;
    };
    reader.readAsDataURL(e.target.files[0]);
});

// 文件上传处理
function handleFileSelect(file) {
    if (!file.type.startsWith('image/')) {
        alert('请选择图片文件');
        return;
    }

    const reader = new FileReader();
    reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
            // 调整canvas大小以适应图片
            const containerWidth = canvas.parentElement.clientWidth;
            const containerHeight = canvas.parentElement.clientHeight;
            const scale = Math.min(
                containerWidth / img.width,
                containerHeight / img.height
            );

            canvas.width = img.width * scale;
            canvas.height = img.height * scale;
            maskCanvas.width = canvas.width;
            maskCanvas.height = canvas.height;

            // 绘制图片
            ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
            currentImage = img;
            canvas.style.display = 'block';
            document.getElementById('uploadText').style.display = 'none';
            uploadMaskBtn.disabled = false;

            // 清除遮罩并重置为画笔模式
            maskCtx.clearRect(0, 0, maskCanvas.width, maskCanvas.height);
            isEraser = false;
            eraserBtn.textContent = '橡皮擦 (E)';
            eraserBtn.classList.remove('active');
        };
        img.src = e.target.result;
    };
    reader.readAsDataURL(file);
}

// 拖放上传
const dropZone = document.getElementById('dropZone');
dropZone.addEventListener('dragover', (e) => {
    e.preventDefault();
    e.stopPropagation();
    dropZone.style.borderColor = '#007bff';
});

dropZone.addEventListener('dragleave', (e) => {
    e.preventDefault();
    e.stopPropagation();
    dropZone.style.borderColor = '#ccc';
});

dropZone.addEventListener('drop', (e) => {
    e.preventDefault();
    e.stopPropagation();
    dropZone.style.borderColor = '#ccc';
    const file = e.dataTransfer.files[0];
    if (file) handleFileSelect(file);
});

// 点击上传
const fileInput = document.getElementById('fileInput');
const uploadBtn = document.getElementById('uploadBtn');
uploadBtn.addEventListener('click', () => fileInput.click());
fileInput.addEventListener('change', (e) => {
    if (e.target.files[0]) handleFileSelect(e.target.files[0]);
});

// 画布绘制功能
canvas.addEventListener('mousedown', startDrawing);
canvas.addEventListener('mousemove', draw);
canvas.addEventListener('mouseup', stopDrawing);
canvas.addEventListener('mouseleave', stopDrawing);

function startDrawing(e) {
    if (!currentImage) return;
    isDrawing = true;
    const rect = canvas.getBoundingClientRect();
    lastX = e.clientX - rect.left;
    lastY = e.clientY - rect.top;
    draw(e);
}

function draw(e) {
    if (!isDrawing) return;

    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    if (isEraser) {
        maskCtx.globalCompositeOperation = 'destination-out';
    } else {
        maskCtx.globalCompositeOperation = 'source-over';
    }

    maskCtx.lineWidth = brushSize;
    maskCtx.lineCap = 'round';
    maskCtx.strokeStyle = '#ffff00';
    maskCtx.beginPath();
    maskCtx.moveTo(lastX, lastY);
    maskCtx.lineTo(x, y);
    maskCtx.stroke();

    lastX = x;
    lastY = y;

    updateCanvas();
}

function updateCanvas() {
    // 重绘原图
    ctx.drawImage(currentImage, 0, 0, canvas.width, canvas.height);

    // 绘制半透明黄色遮罩
    ctx.save();
    ctx.globalAlpha = 0.5;
    ctx.fillStyle = '#ffff00';
    ctx.drawImage(maskCanvas, 0, 0);
    ctx.restore();
}

function stopDrawing() {
    isDrawing = false;
}

// Inpaint功能
document.getElementById('inpaintBtn').addEventListener('click', performInpaint);

async function performInpaint() {
    if (!currentImage) {
        alert('请先上传图片');
        return;
    }

    // 准备数据
    const imageData = canvas.toDataURL('image/png');
    const maskData = maskCanvas.toDataURL('image/png');

    try {
        const response = await fetch('/inpaint', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                image: imageData,
                mask: maskData
            })
        });

        if (!response.ok) throw new Error('Inpaint请求失败');

        const result = await response.json();
        if (result.image) {
            const img = new Image();
            img.onload = () => {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                currentImage = img;
                maskCtx.clearRect(0, 0, maskCanvas.width, maskCanvas.height);
            };
            img.src = result.image;
        }
    } catch (error) {
        console.error('Inpaint错误:', error);
        alert('图像处理失败，请重试');
    }
}

// 快捷键支持
document.addEventListener('keydown', (e) => {
    // Ctrl + O: 上传图片
    if (e.ctrlKey && e.key === 'o') {
        e.preventDefault();
        fileInput.click();
    }
    // [: 减小画笔
    else if (e.key === '[') {
        brushSize = Math.max(1, brushSize - 5);
        brushSizeRange.value = brushSize;
        brushSizeDisplay.textContent = brushSize;
    }
    // ]: 增大画笔
    else if (e.key === ']') {
        brushSize = Math.min(100, brushSize + 5);
        brushSizeRange.value = brushSize;
        brushSizeDisplay.textContent = brushSize;
    }
    // Enter: 执行inpaint
    else if (e.key === 'Enter') {
        performInpaint();
    }
    // E: 切换画笔/橡皮擦
    else if (e.key.toLowerCase() === 'e') {
        isEraser = !isEraser;
        eraserBtn.textContent = isEraser ? '画笔 (E)' : '橡皮擦 (E)';
        eraserBtn.classList.toggle('active');
    }
});

// 窗口大小改变时调整canvas
window.addEventListener('resize', () => {
    if (currentImage) {
        const containerWidth = canvas.parentElement.clientWidth;
        const containerHeight = canvas.parentElement.clientHeight;
        const scale = Math.min(
            containerWidth / currentImage.width,
            containerHeight / currentImage.height
        );

        canvas.width = currentImage.width * scale;
        canvas.height = currentImage.height * scale;
        maskCanvas.width = canvas.width;
        maskCanvas.height = canvas.height;

        updateCanvas();
    }
});

// 添加鼠标移动事件监听器，用于显示画笔大小预览
canvas.addEventListener('mousemove', (e) => {
    if (!currentImage) return;
    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    // 在绘制过程中不显示预览
    if (!isDrawing) {
        ctx.save();
        updateCanvas();
        ctx.beginPath();
        ctx.arc(x, y, brushSize / 2, 0, Math.PI * 2);
        ctx.fillStyle = isEraser ? 'rgba(128, 128, 128, 0.3)' : 'rgba(255, 255, 0, 0.3)';
        ctx.fill();
        ctx.restore();
    }
});

// 鼠标离开画布时恢复原始图像
canvas.addEventListener('mouseleave', () => {
    if (!currentImage) return;
    updateCanvas();
});