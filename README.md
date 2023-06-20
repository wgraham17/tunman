# tunman - super-simple reverse tunnel manager
`tunman` is basically `autossh`, except written in .NET and Dockerized.

## Usage
Behavior is driven completely by configuration. All fields are required. Multiple tunnels can be configured.

| Property                  | Type      | Description |
| --------                  | --------  | ----------- |
| `TunMan:User`                 | string    | The `ssh` user on the remote host. To bind to ports below 1024, must be `root`.
| `TunMan:RemoteHost`           | string    | The remote host to connect to.
| `TunMan:RemoteHostKeys`       | string[]  | Contents will be dropped into a temporary `known_hosts` file for `ssh`.
| `TunMan:PrivateKeyPath`       | string    | The path to the mounted private key file. Specify this _or_ `PrivateKeyContent`.
| `TunMan:PrivateKeyContent`    | string    | The contents of the private key file to use for connecting.
| `TunMan:Tunnels:N:RemotePort` | int       | The remote port to bind. Traffic will be forwarded to `LocalHost:LocalPort`
| `TunMan:Tunnels:N:LocalHost`  | string    | The local (target) host for traffic.
| `TunMan:Tunnels:N:LocalPort`  | int       | The local (target) port for traffic.

`TunMan:RemoteHostKeys` is an array. Use `TunMan:RemoteHostKeys:0`, `TunMan:RemoteHostKeys:1`, etc.

`TunMan:Tunnels` is an array. Multiple values can be assigned via `N`, e.g. `TunMan:Tunnels:0:RemotePort`, `TunMan:Tunnels:1:RemotePort`, `TunMan:Tunnels:2:RemotePort`.

> If using environment variables for configuration, use double underscore (`__`) instead of `:` per the [.NET documentation on configuration providers](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider).
