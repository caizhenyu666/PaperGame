mergeInto(LibraryManager.library, {
  PaperGame_OpenPhotoCapture: function (receiverNamePointer) {
    var receiverName = UTF8ToString(receiverNamePointer);
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/jpeg,image/png';
    input.setAttribute('capture', 'environment');
    input.style.position = 'fixed';
    input.style.left = '-10000px';
    input.style.top = '-10000px';
    document.body.appendChild(input);

    var cleanup = function () {
      input.onchange = null;
      if (input.parentNode) {
        input.parentNode.removeChild(input);
      }
    };

    var fail = function (message) {
      SendMessage(receiverName, 'ReceivePhotoError', message);
      cleanup();
    };

    input.onchange = function () {
      if (!input.files || input.files.length === 0) {
        fail('未选择照片');
        return;
      }

      var file = input.files[0];
      if (file.type !== 'image/jpeg' && file.type !== 'image/png') {
        fail('仅支持 JPEG 或 PNG 照片');
        return;
      }

      if (file.size > 15 * 1024 * 1024) {
        fail('照片不能超过 15 MB');
        return;
      }

      var reader = new FileReader();
      reader.onerror = function () {
        fail('读取照片失败，请重试');
      };
      reader.onload = function () {
        SendMessage(receiverName, 'ReceivePhotoDataUrl', reader.result);
        cleanup();
      };
      reader.readAsDataURL(file);
    };

    input.click();
  }
});
