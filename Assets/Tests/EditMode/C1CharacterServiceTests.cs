using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PaperGame.C1.Tests
{
    public sealed class C1CharacterServiceTests
    {
        [UnityTest]
        public IEnumerator HttpWorkflow_UploadsFilePollsAndDownloadsBothSheets()
        {
            var source = new Texture2D(12, 8, TextureFormat.RGBA32, false);
            var png = source.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(source);
            var portProbe = new TcpListener(IPAddress.Loopback, 0); portProbe.Start();
            var port = ((IPEndPoint)portProbe.LocalEndpoint).Port; portProbe.Stop();
            var listener = new HttpListener(); listener.Prefixes.Add("http://127.0.0.1:" + port + "/"); listener.Start();
            var uploadBody = "";
            var pollCount = 0;
            var serverError = "";
            var animation = "{\"spriteSheetUrl\":\"/artifacts/char_test/ACTION.png\",\"frameCount\":3,\"fps\":15,\"frameWidth\":4,\"frameHeight\":8,\"footAnchor\":{\"x\":2,\"y\":8}}";
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    for (var i = 0; i < 5; i++)
                    {
                        var context = listener.GetContext();
                        byte[] response;
                        if (context.Request.HttpMethod == "POST")
                        {
                            using (var reader = new StreamReader(context.Request.InputStream)) uploadBody = reader.ReadToEnd();
                            context.Response.StatusCode = 202;
                            response = Encoding.UTF8.GetBytes("{\"jobId\":\"char_test\"}");
                        }
                        else if (context.Request.Url.AbsolutePath.StartsWith("/artifacts/")) response = png;
                        else
                        {
                            pollCount++;
                            response = Encoding.UTF8.GetBytes(pollCount == 1 ? "{\"status\":\"processing\"}" :
                                "{\"status\":\"ready\",\"characterId\":\"char_test\",\"animations\":{\"run\":" + animation.Replace("ACTION", "run") + ",\"jump\":" + animation.Replace("ACTION", "jump") + "}}");
                        }
                        context.Response.ContentLength64 = response.Length;
                        context.Response.OutputStream.Write(response, 0, response.Length); context.Response.Close();
                    }
                }
                catch (Exception e) { serverError = e.Message; }
            });
            var root = new GameObject("HTTP Character Test");
            var service = root.AddComponent<C1CharacterService>(); service.BaseUrl = "http://127.0.0.1:" + port;
            C1CharacterFrames frames = null;
            try
            {
                string job = null, error = null;
                C1CharacterRecord record = null;
                yield return Drive(service.Upload(png, "image/png", false, id => job = id, message => error = message));
                Assert.That(error, Is.Null); Assert.That(job, Is.EqualTo("char_test"));
                StringAssert.Contains("name=\"file\"", uploadBody);
                yield return Drive(service.Poll(job, message => { }, result => record = result, message => error = message));
                Assert.That(error, Is.Null); Assert.That(record.status, Is.EqualTo("ready"));
                yield return Drive(service.Download(record, result => frames = result, message => error = message));
                Assert.That(error, Is.Null); Assert.That(serverError, Is.Empty);
                Assert.That(frames.run.Length, Is.EqualTo(3)); Assert.That(frames.jumpFps, Is.EqualTo(15));
                Assert.That(pollCount, Is.EqualTo(2));
            }
            finally { listener.Close(); frames?.Dispose(); UnityEngine.Object.DestroyImmediate(root); }
        }
        private static IEnumerator Drive(IEnumerator routine)
        {
            using (routine as IDisposable)
            {
                while (routine.MoveNext())
                {
                    if (routine.Current is AsyncOperation operation)
                        while (!operation.isDone) yield return null;
                    else if (routine.Current is CustomYieldInstruction instruction)
                        while (instruction.keepWaiting) yield return null;
                    else if (routine.Current is IEnumerator child) yield return Drive(child);
                    else yield return null;
                }
            }
        }
        [Test]
        public void ReadyContract_ParsesBothAnimationsWithoutDictionary()
        {
            var record = C1CharacterService.Parse("{\"status\":\"ready\",\"characterId\":\"char_test\",\"animations\":{\"run\":{\"frameCount\":10,\"fps\":15},\"jump\":{\"frameCount\":7,\"fps\":15}}}");
            Assert.That(record.animations.run.frameCount, Is.EqualTo(10));
            Assert.That(record.animations.jump.frameCount, Is.EqualTo(7));
        }
        [Test]
        public void Slice_UsesHorizontalFramesAndTopDownFootAnchor()
        {
            var texture = new Texture2D(12, 8);
            var meta = new C1AnimationMeta { frameCount = 3, frameWidth = 4, frameHeight = 8, fps = 15, footAnchor = new C1FootAnchor { x = 2, y = 8 } };
            var frames = C1CharacterFrames.Slice(texture, meta);
            Assert.That(frames[2].rect, Is.EqualTo(new Rect(8,0,4,8)));
            Assert.That(frames[0].pivot, Is.EqualTo(new Vector2(2,0)));
            foreach (var frame in frames) UnityEngine.Object.DestroyImmediate(frame);
            UnityEngine.Object.DestroyImmediate(texture);
        }
        [Test]
        public void Slice_RejectsMismatchedSheetDimensions()
        {
            var texture = new Texture2D(10,8);
            var meta = new C1AnimationMeta { frameCount = 3, frameWidth = 4, frameHeight = 8, fps = 15, footAnchor = new C1FootAnchor { x = 2,y = 8 } };
            Assert.Throws<ArgumentException>(() => C1CharacterFrames.Slice(texture,meta));
            UnityEngine.Object.DestroyImmediate(texture);
        }
        [Test]
        public void LibraryJson_PreservesSelectedAndPendingJobs()
        {
            var original = new C1CharacterLibrary { selectedId = "char_saved", pendingJobId = "char_pending" };
            original.characters.Add(new C1CharacterRecord { characterId = "char_saved" });
            var restored = JsonUtility.FromJson<C1CharacterLibrary>(JsonUtility.ToJson(original));
            Assert.That(restored.selectedId, Is.EqualTo("char_saved"));
            Assert.That(restored.pendingJobId, Is.EqualTo("char_pending"));
            Assert.That(restored.characters.Count, Is.EqualTo(1));
        }
    }
}
