using System.IO;

namespace OmniSharp.PlatformUtilities
{
    public class WindowsLock : PlatformLock
    {
        private FileStream _fileStream;

        public override void Lock(string lockfile)
        {
            _fileStream = new FileStream(lockfile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
        }

        public override void Dispose()
        {
            _fileStream.Dispose();
        }
    }
}