using System;

namespace OmniSharp.PlatformUtilities
{
    public abstract class PlatformLock : IDisposable
    {
        public abstract void Lock(string lockfile);
        public abstract void Dispose();
    }
}