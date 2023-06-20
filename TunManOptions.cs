namespace tunman
{
    public class TunManOptions : IEquatable<TunManOptions>
    {
        public static int DefaultServerAliveInterval => 30;

        public static int DefaultServerAliveCountMax => 10;

        public static bool DefaultExitOnForwardFailure => true;

        public int? ServerAliveInterval { get; set; }

        public int? ServerAliveCountMax { get; set; }

        public bool? ExitOnForwardFailure { get; set; }

        public string? User { get; set; }

        public string? RemoteHost { get; set; }

        public string[]? RemoteHostKeys { get; set; }

        public string? PrivateKeyContent { get; set; }

        public string? PrivateKeyPath { get; set; }

        public TunnelOptions[]? Tunnels { get; set; }

        public bool Equals(TunManOptions? other) =>
            other != null &&
            ServerAliveInterval == other.ServerAliveInterval &&
            ServerAliveCountMax == other.ServerAliveCountMax &&
            ExitOnForwardFailure == other.ExitOnForwardFailure &&
            User == other.User &&
            RemoteHost == other.RemoteHost &&
            RemoteHostKeys?.Length == other.RemoteHostKeys?.Length &&
            AreArraysEqual(RemoteHostKeys, other.RemoteHostKeys) &&
            PrivateKeyContent == other.PrivateKeyContent &&
            PrivateKeyPath == other.PrivateKeyPath &&
            Tunnels?.Length == other.Tunnels?.Length &&
            AreArraysEqual(Tunnels, other.Tunnels);

        private static bool AreArraysEqual<T>(T[]? a, T[]? b)
            where T : IEquatable<T>
        {
            if (a is null && b is null)
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (!a[i].Equals(b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as TunManOptions);

        public override int GetHashCode() => HashCode.Combine(
            ServerAliveInterval,
            ServerAliveCountMax,
            ExitOnForwardFailure,
            User,
            RemoteHost,
            PrivateKeyPath,
            PrivateKeyContent,
            Tunnels);
    }
}
