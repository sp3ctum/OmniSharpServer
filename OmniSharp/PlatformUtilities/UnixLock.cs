using System;
using System.IO;

namespace OmniSharp.PlatformUtilities
{
    public class UnixLock : PlatformLock
    {
        private Stream _stream;

        public Stream OpenExclusive(string path, FileAccess mode) 
        {
            return OpenExclusive(path, mode, (int)Mono.Unix.Native.FilePermissions.DEFFILEMODE);
        }

        public Stream OpenExclusive(string path, FileAccess mode, int filemode) 
        {
            Mono.Unix.Native.Flock lck;
            lck.l_len = 0;
            lck.l_pid = Mono.Unix.Native.Syscall.getpid();
            lck.l_start = 0;
            lck.l_type = Mono.Unix.Native.LockType.F_WRLCK;
            lck.l_whence = Mono.Unix.Native.SeekFlags.SEEK_SET;
			
            var flags = Mono.Unix.Native.OpenFlags.O_CREAT;
            if (mode == FileAccess.Read) 
            {
                lck.l_type = Mono.Unix.Native.LockType.F_RDLCK;
                flags |= Mono.Unix.Native.OpenFlags.O_RDONLY;
            } else if (mode == FileAccess.Write) {
                flags |= Mono.Unix.Native.OpenFlags.O_WRONLY;
            } else {
                flags |= Mono.Unix.Native.OpenFlags.O_RDWR;
            }
			
            int fd = Mono.Unix.Native.Syscall.open(path, flags, (Mono.Unix.Native.FilePermissions)filemode);
            if (fd > 0) 
            {
                int res = Mono.Unix.Native.Syscall.fcntl(fd, Mono.Unix.Native.FcntlCommand.F_SETLK, ref lck);

                //If we have the lock, return the stream
                if (res == 0)
                    return new Mono.Unix.UnixStream(fd);
			
                Mono.Unix.Native.Syscall.close(fd);
                throw new LockedFileException(path, mode);
            }
			
            throw new BadFileException(path);
        }

        [Serializable]
        private class BadFileException : IOException
        {
            public BadFileException(string filename)
                : base(string.Format("Unable to open the file \"{0}\", error: {1} ({2})", filename, Mono.Unix.Native.Stdlib.GetLastError(), (int)Mono.Unix.Native.Stdlib.GetLastError()))
            {
            }
        }

        [Serializable]
        private class LockedFileException : IOException
        {
            public LockedFileException(string filename, FileAccess mode)
                : base(string.Format("Unable to open the file \"{0}\" in mode {1}", filename, mode))
            {
            }
        }

        public override void Lock(string lockfile)
        {
            _stream = OpenExclusive(lockfile, FileAccess.Read);
        }

        public override void Dispose()
        {
            _stream.Dispose();
        }
    }
}