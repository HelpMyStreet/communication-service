using CommunicationService.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CommunicationService.SendGridManagement
{
    public class DirectoryService : IDirectoryService
    {
        public string[] GetFiles(string path)
        {
            return Directory.GetFiles(path);
        }

        public string ReadAllText(string fileName)
        {
            return File.ReadAllText(fileName);
        }
    }
}
