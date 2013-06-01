namespace OmniSharp.PlatformUtilities
{
    public static class PlatformLockFactory
    {
        public static PlatformLock GetPlatformLock()
        {
            return PlatformService.IsUnix ? (PlatformLock) new UnixLock() : new WindowsLock();
        }
    }
}