mergeInto(LibraryManager.library, {
  PaperGame_SyncLevelStorage: function (receiverPointer) {
    var receiver = UTF8ToString(receiverPointer);
    try {
      FS.syncfs(false, function (error) {
        SendMessage(receiver, 'ReceiveStorageResult', error ? String(error) : '');
      });
    } catch (error) {
      SendMessage(receiver, 'ReceiveStorageResult', String(error));
    }
  }
});
