mergeInto(LibraryManager.library, {
  PaperGame_OpenPhotoCapture: function (receiverNamePointer) {
    var receiverName = UTF8ToString(receiverNamePointer);

    var fail = function (message) {
      SendMessage(receiverName, 'ReceivePhotoError', message || '未能获取照片，请重试');
    };

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      fail('当前浏览器不支持相机访问，请使用 Safari 或 Chrome');
      return;
    }

    var stream = null;
    var overlay = null;
    var video = null;
    var css = null;
    var cleaned = false;

    var cameraViewportSize = function () {
      var viewport = window.visualViewport;
      return {
        width: viewport ? viewport.width : window.innerWidth,
        height: viewport ? viewport.height : window.innerHeight
      };
    };

    var cleanup = function () {
      if (cleaned) return;
      cleaned = true;
      if (stream) stream.getTracks().forEach(function (track) { track.stop(); });
      stream = null;
      if (video) video.srcObject=null;
      if (overlay && overlay.parentNode) overlay.parentNode.removeChild(overlay);
      if (css && css.parentNode) css.parentNode.removeChild(css);
      overlay = null;
      video = null;
      css = null;
    };

    var sendPhoto = function (dataUrl) {
      cleanup();
      SendMessage(receiverName, 'ReceivePhotoDataUrl', dataUrl);
    };

    // ── 创建相机覆盖层 ──────────────────────────────────────────
    overlay = document.createElement('div');
    overlay.id = 'pg-camera-overlay';
    overlay.innerHTML = [
      '<video id="pg-cam-video" autoplay playsinline muted></video>',
      '<div id="pg-cam-frame" aria-hidden="true"></div>',
      '<button id="pg-cam-close" type="button" aria-label="关闭相机">×</button>',
      '<button id="pg-cam-btn" type="button" aria-label="拍照">',
      '<span id="pg-cam-shutter-core" aria-hidden="true"></span>',
      '</button>'
    ].join('');

    // ── 样式 ───────────────────────────────────────────────────
    css = document.createElement('style');
    css.textContent = [
      '#pg-camera-overlay{position:fixed;inset:0;width:100vw;height:100vh;width:100dvw;height:100dvh;z-index:2147483647;background:#000;overflow:hidden;backface-visibility:hidden}',
      '#pg-cam-video{position:absolute;inset:0;width:100%;height:100%;object-fit:cover}',
      '#pg-cam-frame{position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);width:var(--pg-frame-width);aspect-ratio:8/5;border:2.5px solid rgba(255,255,255,.88);border-radius:14px;box-shadow:0 0 0 9999px rgba(0,0,0,.42);pointer-events:none}',
      '#pg-cam-close{position:absolute;top:max(16px,env(safe-area-inset-top));left:max(16px,env(safe-area-inset-left));width:44px;height:44px;padding:0;border:0;border-radius:50%;background:rgba(0,0,0,.45);color:#fff;font:300 34px/40px -apple-system,BlinkMacSystemFont,sans-serif;text-align:center;cursor:pointer;-webkit-tap-highlight-color:transparent}',
      '#pg-cam-btn{position:absolute;top:50%;right:max(18px,env(safe-area-inset-right));transform:translateY(-50%);width:72px;height:72px;padding:5px;border:4px solid #fff;border-radius:50%;background:transparent;cursor:pointer;-webkit-tap-highlight-color:transparent}',
      '#pg-cam-shutter-core{display:block;width:100%;height:100%;border-radius:50%;background:#fff;transition:transform .08s ease}',
      '#pg-cam-btn:active #pg-cam-shutter-core{transform:scale(.9)}',
      '#pg-cam-btn:disabled{opacity:.55}',
      '@media (orientation:portrait){#pg-cam-btn{top:auto;left:50%;right:auto;bottom:max(24px,env(safe-area-inset-bottom));transform:translateX(-50%)}}'
    ].join('');
    document.head.appendChild(css);
    document.body.appendChild(overlay);

    video = document.getElementById('pg-cam-video');
    var btn   = document.getElementById('pg-cam-btn');
    var close = document.getElementById('pg-cam-close');
    var initialViewport = cameraViewportSize();
    var frameWidth = Math.max(1,
      Math.min(560, Math.min(initialViewport.width, initialViewport.height) - 48));
    overlay.style.setProperty('--pg-frame-width', frameWidth + 'px');
    close.addEventListener('click',cleanup);

    // ── 请求后置摄像头 ──────────────────────────────────────────
    navigator.mediaDevices.getUserMedia({
      video: { facingMode: { ideal: 'environment' }, width: { ideal: 1920 }, height: { ideal: 1080 }, aspectRatio: { ideal: 8 / 5 } },
      audio: false
    }).then(function (s) {
      if (cleaned) {
        s.getTracks().forEach(function (track) { track.stop(); });
        return;
      }
      stream = s;
      video.srcObject = s;
    }).catch(function () {
      if (cleaned) return;
      cleanup();
      fail('无法访问相机，请检查浏览器权限设置');
    });

    // ── 拍照（只裁引导框内的区域） ────────────────────────────────
    btn.addEventListener('click', function () {
      if (!video.videoWidth || !video.videoHeight) return;
      btn.disabled = true;

      var frame = document.getElementById('pg-cam-frame');
      var viewWidth = video.clientWidth;
      var viewHeight = video.clientHeight;
      var sourceWidth = video.videoWidth;
      var sourceHeight = video.videoHeight;

      /* object-fit: cover → 计算视频在容器中的实际绘制区域 */
      var vScale = Math.max(viewWidth / sourceWidth, viewHeight / sourceHeight);
      var drawnW = sourceWidth * vScale;
      var drawnH = sourceHeight * vScale;
      var offsetX = (viewWidth - drawnW) / 2;
      var offsetY = (viewHeight - drawnH) / 2;

      /* 使用物理视口内的局部坐标，预览与最终裁剪保持一致。 */
      var fw = frame.clientWidth;
      var fh = frame.clientHeight;
      var fx = frame.offsetLeft - fw / 2 - offsetX;
      var fy = frame.offsetTop - fh / 2 - offsetY;

      /* 映射到视频原始像素坐标并裁剪到合法范围 */
      var sx = Math.max(0, Math.round(fx * sourceWidth / drawnW));
      var sy = Math.max(0, Math.round(fy * sourceHeight / drawnH));
      var sw = Math.min(sourceWidth - sx, Math.round(fw * sourceWidth / drawnW));
      var sh = Math.min(sourceHeight - sy, Math.round(fh * sourceHeight / drawnH));

      var canvas = document.createElement('canvas');
      canvas.width  = sw;
      canvas.height = sh;
      canvas.getContext('2d').drawImage(video, sx, sy, sw, sh, 0, 0, sw, sh);

      try {
        var dataUrl = canvas.toDataURL('image/jpeg', 0.92);
        if (dataUrl && dataUrl.length > 100) { sendPhoto(dataUrl); return; }
      } catch (_) {}

      btn.disabled = false;
    });

    // ── 视频加载失败回退 ────────────────────────────────────────
    video.addEventListener('error', function () {
      if (cleaned) return;
      cleanup();
      fail('相机启动失败，请重试');
    });
  }
});
