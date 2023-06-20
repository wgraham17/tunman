namespace tunman
{
    public class TunnelOptions: IEquatable<TunnelOptions>
    {
        public int? RemotePort { get; set; }

        public string? LocalHost { get; set; }

        public int? LocalPort { get; set; }

        public bool Equals(TunnelOptions? other) => RemotePort == other?.RemotePort && LocalHost == other?.LocalHost && LocalPort == other?.LocalPort;

        public override bool Equals(object? obj) => Equals(obj as TunnelOptions);

        public override int GetHashCode() => HashCode.Combine(RemotePort, LocalHost, LocalPort);
    }
}
