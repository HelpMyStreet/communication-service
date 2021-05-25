using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using MimeTypes;
using System.Net.Http.Headers;

namespace CommunicationService.AzureFunction
{
    public static class Home
    {
        const string staticFilesFolder = "www";
        static string defaultPage =
            string.IsNullOrEmpty(GetEnvironmentVariable("DEFAULT_PAGE")) ?
            "index.html" : GetEnvironmentVariable("DEFAULT_PAGE");

        [FunctionName("Home")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            try
            {
                var filePath = GetFilePath(req, log);

                var response = new HttpResponseMessage(HttpStatusCode.OK);
                var stream = new FileStream(filePath, FileMode.Open);
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentType =
                    new MediaTypeHeaderValue(GetMimeType(filePath));
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        private static string GetMimeType(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return MimeTypeMap.GetMimeType(fileInfo.Extension);
        }

        private static string GetScriptPath()
    => Path.Combine("", "");

        private static string GetEnvironmentVariable(string name)
            => System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

        private static bool IsInDirectory(string parentPath, string childPath)
        {
            var parent = new DirectoryInfo(parentPath);
            var child = new DirectoryInfo(childPath);

            var dir = child;
            do
            {
                if (dir.FullName == parent.FullName)
                {
                    return true;
                }
                dir = dir.Parent;
            } while (dir != null);

            return false;
        }

        private static string GetFilePath(HttpRequestMessage req, ILogger log)
        {
            var qs = req.RequestUri.ParseQueryString();
            var pathValue = qs.Get("file");

            var path = pathValue ?? "";

            var staticFilesPath =
                Path.GetFullPath(Path.Combine(GetScriptPath(), staticFilesFolder));
            var fullPath = Path.GetFullPath(Path.Combine(staticFilesPath, path));

            if (!IsInDirectory(staticFilesPath, fullPath))
            {
                throw new ArgumentException("Invalid path");
            }

            var isDirectory = Directory.Exists(fullPath);
            if (isDirectory)
            {
                fullPath = Path.Combine(fullPath, defaultPage);
            }

            return fullPath;
        }
    }
}
