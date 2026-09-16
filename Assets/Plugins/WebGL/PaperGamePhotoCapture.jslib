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

    var cleanup = function () {
      if (stream) {
        stream.getTracks().forEach(function (t) { t.stop(); });
        stream = null;
      }
      if (overlay && overlay.parentNode) {
        overlay.parentNode.removeChild(overlay);
      }
      overlay = null;
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
      '<div id="pg-cam-dim"></div>',
      '<div id="pg-cam-frame"></div>',
      '<button id="pg-cam-btn">拍 照</button>'
    ].join('');

    // ── 样式 ───────────────────────────────────────────────────
    var css = document.createElement('style');
    css.textContent = [
      '#pg-camera-overlay{position:fixed;top:0;left:0;right:0;bottom:0;z-index:2147483647;background:#000;overflow:hidden}',
      '#pg-cam-video{position:absolute;top:0;left:0;width:100%;height:100%;object-fit:cover}',
      '#pg-cam-dim{position:absolute;top:0;left:0;right:0;bottom:0;pointer-events:none}',
      '#pg-cam-frame{position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);width:560px;height:350px;max-width:calc(100vw - 48px);max-height:calc(100vh - 48px);border:2.5px solid rgba(255,255,255,.88);border-radius:14px;box-shadow:0 0 0 9999px rgba(0,0,0,.42);pointer-events:none}',
      '#pg-cam-btn{position:absolute;top:50%;right:16px;transform:translateY(-50%);width:56px;height:56px;border-radius:50%;border:3px solid #fff;background:rgba(255,255,255,.18);color:#fff;font:bold 13px/56px -apple-system,sans-serif;text-align:center;cursor:pointer;-webkit-tap-highlight-color:transparent;outline:none}'
    ].join('');
    document.head.appendChild(css);
    document.body.appendChild(overlay);

    var video = document.getElementById('pg-cam-video');
    var btn   = document.getElementById('pg-cam-btn');

    // ── 请求后置摄像头 ──────────────────────────────────────────
    navigator.mediaDevices.getUserMedia({
      video: { facingMode: { ideal: 'environment' }, width: { ideal: 1920 }, height: { ideal: 1080 } },
      audio: false
    }).then(function (s) {
      stream = s;
      video.srcObject = s;
    }).catch(function () {
      cleanup();
      document.head.removeChild(css);
      fail('无法访问相机，请检查浏览器权限设置');
    });

    // ── 拍照（只裁引导框内的区域） ────────────────────────────────
    btn.addEventListener('click', function () {
      if (!video.videoWidth || !video.videoHeight) return;
      btn.disabled = true;
      btn.textContent = '…';

      var frame = document.getElementById('pg-cam-frame');
      var fRect = frame.getBoundingClientRect();
      var vRect = video.getBoundingClientRect();

      /* object-fit: cover → 计算视频在容器中的实际绘制区域 */
      var vScale = Math.max(vRect.width / video.videoWidth, vRect.height / video.videoHeight);
      var drawnW = video.videoWidth * vScale;
      var drawnH = video.videoHeight * vScale;
      var offsetX = (vRect.width - drawnW) / 2;
      var offsetY = (vRect.height - drawnH) / 2;

      /* 引导框相对于视频绘制区域的 CSS 像素坐标 */
      var fx = fRect.left - vRect.left - offsetX;
      var fy = fRect.top - vRect.top - offsetY;
      var fw = fRect.width;
      var fh = fRect.height;

      /* 映射到视频原始像素坐标并裁剪到合法范围 */
      var sx = Math.max(0, Math.round(fx * video.videoWidth / drawnW));
      var sy = Math.max(0, Math.round(fy * video.videoHeight / drawnH));
      var sw = Math.min(video.videoWidth - sx, Math.round(fw * video.videoWidth / drawnW));
      var sh = Math.min(video.videoHeight - sy, Math.round(fh * video.videoHeight / drawnH));

      var canvas = document.createElement('canvas');
      canvas.width  = sw;
      canvas.height = sh;
      canvas.getContext('2d').drawImage(video, sx, sy, sw, sh, 0, 0, sw, sh);

      try {
        var dataUrl = canvas.toDataURL('image/jpeg', 0.92);
        if (dataUrl && dataUrl.length > 100) { sendPhoto(dataUrl); document.head.removeChild(css); return; }
      } catch (_) {}

      btn.disabled = false;
      btn.textContent = '重 试';
    });

    // ── 视频加载失败回退 ────────────────────────────────────────
    video.addEventListener('error', function () {
      cleanup();
      document.head.removeChild(css);
      fail('相机启动失败，请重试');
    });
  }
});
