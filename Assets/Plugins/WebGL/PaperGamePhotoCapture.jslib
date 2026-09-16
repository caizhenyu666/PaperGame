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
    var stageWidth = 1;
    var stageHeight = 1;
    var cameraPortrait = false;
    var streamRotation = 0;
    var portraitStreamRotation = 0;

    var cameraViewportSize = function () {
      var viewport = window.visualViewport;
      return {
        width: viewport ? viewport.width : window.innerWidth,
        height: viewport ? viewport.height : window.innerHeight
      };
    };

    var syncVideoOrientation = function () {
      if (!video || !video.videoWidth || !video.videoHeight) return;
      if (video.videoHeight > video.videoWidth) {
        // 视口通常比 MediaStream 更早报告旋转；同一段 portrait 流必须保持方向，
        // 避免这段时序差让预览瞬间从 -90° 翻到 +90°。
        if (!portraitStreamRotation) {
          portraitStreamRotation = cameraPortrait ? -90 : 90;
        }
        streamRotation = portraitStreamRotation;
        video.style.inset = 'auto';
        video.style.left = '50%';
        video.style.top = '50%';
        video.style.width = stageHeight + 'px';
        video.style.height = stageWidth + 'px';
        video.style.transform = 'translate(-50%,-50%) rotate(' + streamRotation + 'deg)';
      } else {
        portraitStreamRotation = 0;
        streamRotation = 0;
        video.style.inset = '0';
        video.style.width = '100%';
        video.style.height = '100%';
        video.style.transform = 'none';
      }
    };

    var layoutCameraStage = function () {
      if (!overlay) return;
      var viewport = cameraViewportSize();
      var viewportWidth = Math.max(1, viewport.width);
      var viewportHeight = Math.max(1, viewport.height);
      cameraPortrait = viewportHeight > viewportWidth;

      var scale;
      var offsetX;
      var offsetY;
      if (cameraPortrait) {
        scale = Math.min(viewportWidth / stageHeight, viewportHeight / stageWidth);
        offsetX = (viewportWidth - stageHeight * scale) / 2;
        offsetY = (viewportHeight - stageWidth * scale) / 2;
        overlay.style.transform = 'matrix(' + [
          0, scale, -scale, 0, offsetX + stageHeight * scale, offsetY
        ].join(',') + ')';
      } else {
        scale = Math.min(viewportWidth / stageWidth, viewportHeight / stageHeight);
        offsetX = (viewportWidth - stageWidth * scale) / 2;
        offsetY = (viewportHeight - stageHeight * scale) / 2;
        overlay.style.transform = 'matrix(' + [
          scale, 0, 0, scale, offsetX, offsetY
        ].join(',') + ')';
      }

      var frameWidth = Math.max(1,
        Math.min(560, stageWidth - 176, (stageHeight - 32) * 8 / 5));
      overlay.style.setProperty('--pg-frame-width', frameWidth + 'px');
      syncVideoOrientation();
    };

    var cleanup = function () {
      if (cleaned) return;
      cleaned = true;
      if (stream) stream.getTracks().forEach(function (track) { track.stop(); });
      stream = null;
      if (video) video.srcObject=null;
      window.removeEventListener('resize',layoutCameraStage);
      if (window.visualViewport) {
        window.visualViewport.removeEventListener('resize',layoutCameraStage);
      }
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
      '#pg-camera-overlay{position:fixed;inset:0;z-index:2147483647;background:#000;overflow:hidden;transform:translate3d(0,0,0);transform-origin:top left;backface-visibility:hidden}',
      '#pg-cam-video{position:absolute;inset:0;width:100%;height:100%;object-fit:cover}',
      '#pg-cam-frame{position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);width:var(--pg-frame-width);aspect-ratio:8/5;border:2.5px solid rgba(255,255,255,.88);border-radius:14px;box-shadow:0 0 0 9999px rgba(0,0,0,.42);pointer-events:none}',
      '#pg-cam-close{position:absolute;top:max(16px,env(safe-area-inset-top));left:max(16px,env(safe-area-inset-left));width:44px;height:44px;padding:0;border:0;border-radius:50%;background:rgba(0,0,0,.45);color:#fff;font:300 34px/40px -apple-system,BlinkMacSystemFont,sans-serif;text-align:center;cursor:pointer;-webkit-tap-highlight-color:transparent}',
      '#pg-cam-btn{position:absolute;top:50%;right:max(18px,env(safe-area-inset-right));transform:translateY(-50%);width:72px;height:72px;padding:5px;border:4px solid #fff;border-radius:50%;background:transparent;cursor:pointer;-webkit-tap-highlight-color:transparent}',
      '#pg-cam-shutter-core{display:block;width:100%;height:100%;border-radius:50%;background:#fff;transition:transform .08s ease}',
      '#pg-cam-btn:active #pg-cam-shutter-core{transform:scale(.9)}',
      '#pg-cam-btn:disabled{opacity:.55}'
    ].join('');
    document.head.appendChild(css);
    document.body.appendChild(overlay);

    video = document.getElementById('pg-cam-video');
    var btn   = document.getElementById('pg-cam-btn');
    var close = document.getElementById('pg-cam-close');
    var initialViewport = cameraViewportSize();
    stageWidth = Math.max(initialViewport.width, initialViewport.height);
    stageHeight = Math.min(initialViewport.width, initialViewport.height);
    overlay.style.width = stageWidth + 'px';
    overlay.style.height = stageHeight + 'px';
    layoutCameraStage();
    window.addEventListener('resize',layoutCameraStage);
    if (window.visualViewport) {
      window.visualViewport.addEventListener('resize',layoutCameraStage);
    }
    close.addEventListener('click',cleanup);
    video.addEventListener('loadedmetadata',syncVideoOrientation);
    video.addEventListener('resize',syncVideoOrientation);

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
      var viewWidth = stageWidth;
      var viewHeight = stageHeight;
      var normalizedSource = video;
      if (streamRotation) {
        normalizedSource = document.createElement('canvas');
        normalizedSource.width = video.videoHeight;
        normalizedSource.height = video.videoWidth;
        var normalizedContext = normalizedSource.getContext('2d');
        if (streamRotation > 0) {
          normalizedContext.translate(normalizedSource.width, 0);
          normalizedContext.rotate(Math.PI / 2);
        } else {
          normalizedContext.translate(0, normalizedSource.height);
          normalizedContext.rotate(-Math.PI / 2);
        }
        normalizedContext.drawImage(video, 0, 0);
      }
      var sourceWidth = normalizedSource === video ? video.videoWidth : normalizedSource.width;
      var sourceHeight = normalizedSource === video ? video.videoHeight : normalizedSource.height;

      /* object-fit: cover → 计算视频在容器中的实际绘制区域 */
      var vScale = Math.max(viewWidth / sourceWidth, viewHeight / sourceHeight);
      var drawnW = sourceWidth * vScale;
      var drawnH = sourceHeight * vScale;
      var offsetX = (viewWidth - drawnW) / 2;
      var offsetY = (viewHeight - drawnH) / 2;

      /* 使用横屏舞台内的局部坐标，避免父级旋转后的包围盒交换宽高。 */
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
      canvas.getContext('2d').drawImage(normalizedSource, sx, sy, sw, sh, 0, 0, sw, sh);

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
