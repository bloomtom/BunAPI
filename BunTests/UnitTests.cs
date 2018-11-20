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
    public class UnitTests
    {
        private readonly string testFile = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()} test.test";
        private const string testContent = "Hello, world!";

        [TestMethod]
        public void TestFileOperations()
        {
            var client = new BunClient(ConnectionInfo.apiKey, ConnectionInfo.zone);

            // Write a file.
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms))
            {
                writer.Write(testContent);
                writer.Flush();
                var writeResponse = client.PutFile(ms, testFile).Result;
                Assert.AreEqual(HttpStatusCode.Created, writeResponse);
            }

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
    }
}
