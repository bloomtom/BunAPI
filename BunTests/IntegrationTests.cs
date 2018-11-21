using BunAPI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;

namespace BunTests
{
    [TestClass]
    public class IntegrationTests
    {
        private readonly string testFile = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()} test.test";
        private const string testContent = "Hello, world!";

        [TestMethod]
        public void TestFileOperations()
        {
            var client = new BunClient(ConnectionInfo.apiKey, ConnectionInfo.zone);

            // Write a file.
            var writeResponse = client.PutFile(testContent, testFile).Result;
            Assert.AreEqual(HttpStatusCode.Created, writeResponse);

            // Check for our file in the file listing.
            var listResponse = client.ListFiles().Result;
            Assert.AreEqual(HttpStatusCode.OK, listResponse.StatusCode);
            Assert.IsTrue(listResponse.Files.Where(x => x.ObjectName == testFile).Count() == 1);

            // Read back the file.
            var readResponse = client.GetFile(testFile).Result;
            Assert.AreEqual(HttpStatusCode.OK, readResponse.StatusCode);

            string fileContent = null;
            using (var reader = new StreamReader(readResponse.Stream))
            {
                fileContent = reader.ReadToEnd();
            }
            Assert.AreEqual(testContent, fileContent);

            // Test deleting the file.
            var deleteResult = client.DeleteFile(testFile).Result;
            Assert.AreEqual(HttpStatusCode.OK, deleteResult);
        }

        [TestMethod]
        public void TestGetExpectNotFound()
        {
            var client = new BunClient(ConnectionInfo.apiKey, ConnectionInfo.zone);

            var badFile = client.GetFile(ConnectionInfo.badZone).Result;
            Assert.AreEqual(HttpStatusCode.NotFound, badFile.StatusCode);
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
