namespace tunman
{
    internal class TempFileRef : IDisposable
    {
        private string? _path;
        private bool disposedValue;

        public TempFileRef()
        {
            _path = System.IO.Path.GetTempFileName();
        }

        public string Path => _path ?? throw new ObjectDisposedException(nameof(TempFileRef));

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (!string.IsNullOrEmpty(_path))
                {
                    File.Delete(_path);
                    _path = null;
                }

                disposedValue = true;
            }
        }
        ~TempFileRef()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
