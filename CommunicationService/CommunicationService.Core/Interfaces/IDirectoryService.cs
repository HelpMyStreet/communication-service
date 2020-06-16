using System;
using System.Collections.Generic;
using System.Text;

namespace CommunicationService.Core.Interfaces
{
    public interface IDirectoryService
    {
        string[] GetFiles(string path);
        string ReadAllText(string fileName);
    }
}
