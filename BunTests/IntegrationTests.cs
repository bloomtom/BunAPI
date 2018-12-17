using BunAPI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using HttpProgress;

namespace BunTests
{
    [TestClass]
    public class IntegrationTests
    {
        private readonly string testFile = $"{DateTime.Now.ToString("yyyy-MM-dd")} {DateTime.Now.ToShortTimeString()} test.test";
        private const string testContent = "Hello, world!";

        [TestMethod]
        public void TestFileOperations()
        {
            var client = new BunClient(ConnectionInfo.apiKey, ConnectionInfo.zone);

            // Write a file.
            var progress = new Action<ICopyProgress>(x =>
            {
                ;
            });
            var writeResponse = client.PutFile(testContent, testFile, progress).Result;
            Assert.AreEqual(HttpStatusCode.Created, writeResponse);

            // Check for our file in the file listing.
            var listResponse = client.ListFiles().Result;
            Assert.AreEqual(HttpStatusCode.OK, listResponse.StatusCode);
            Assert.IsTrue(listResponse.Files.Where(x => x.ObjectName == testFile).Count() == 1);

            // Read back the file.
            using (var readStream = new MemoryStream())
            {
                var readResponse = client.GetFile(testFile, readStream).Result;
                Assert.AreEqual(HttpStatusCode.OK, readResponse);

                string fileContent = null;
                using (var reader = new StreamReader(readStream))
                {
                    fileContent = reader.ReadToEnd();
                }
                Assert.AreEqual(testContent, fileContent);
            }

            // Test deleting the file.
            var deleteResult = client.DeleteFile(testFile).Result;
            Assert.AreEqual(HttpStatusCode.OK, deleteResult);
        }

        [TestMethod]
        public void TestGetExpectZoneNotFound()
        {
            var client = new BunClient(ConnectionInfo.apiKey, ConnectionInfo.zone);

            Stream s = new MemoryStream();
            var badStatus = client.GetFile(ConnectionInfo.badZone, s).Result;
            Assert.AreEqual(HttpStatusCode.NotFound, badStatus);
        }

        [TestMethod]
        public void TestGetExpectObjectNotFound()
        {
            var client = new BunClient(ConnectionInfo.apiKey, ConnectionInfo.zone);

            Stream s = new MemoryStream();
            var r = client.GetFile(ConnectionInfo.badZone).Result;
            Assert.AreEqual(HttpStatusCode.NotFound, r.StatusCode);
        }

        [TestMethod]
        public void TestDeleteExpectNotFound()
        {
            var client = new BunClient(ConnectionInfo.apiKey, ConnectionInfo.zone);

            var badFile = client.DeleteFile(ConnectionInfo.badZone).Result;
            Assert.AreEqual(HttpStatusCode.NotFound, badFile);
        }

        [TestMethod]
        public void TestBadZone()
        {
            var client = new BunClient(ConnectionInfo.apiKey, ConnectionInfo.badZone);

            var badResult = client.ListFiles().Result;
            Assert.AreEqual(HttpStatusCode.Unauthorized, badResult.StatusCode);
            Assert.AreEqual(0, badResult.Files.Count());
        }

        [TestMethod]
        public void TestBadPut()
        {
            var client = new BunClient(ConnectionInfo.apiKey, ConnectionInfo.badZone);

            var writeResponse = client.PutFile(testContent, testFile).Result;
            Assert.AreEqual(HttpStatusCode.Unauthorized, writeResponse);
        }

        /// <summary>
        /// This test takes a long time, seemingly because BunnyCDN holds the connection open for a bit when a bad key is given?
        /// </summary>
        [TestMethod]
        public void TestBadKey()
        {
            var client = new BunClient(ConnectionInfo.badKey, ConnectionInfo.zone);

            var badResult = client.ListFiles().Result;
            Assert.AreEqual(HttpStatusCode.Unauthorized, badResult.StatusCode);
            Assert.AreEqual(0, badResult.Files.Count());
        }
    }
}
