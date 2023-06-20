namespace tunman
{
    using Microsoft.Extensions.Options;
    using System.Diagnostics;
    using System.Text;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IOptionsMonitor<TunManOptions> _optionsMonitor;
        private TempFileRef? _privateKeyFile;
        private TunManOptions? _currentOptions;
        private CancellationToken _workerToken;
        private CancellationTokenSource? _optionsCancellationTokenSource;

        public Worker(ILogger<Worker> logger, IOptionsMonitor<TunManOptions> optionsMonitor)
        {
            _logger = logger;
            _optionsMonitor = optionsMonitor;
            _optionsMonitor.OnChange(OnOptionsChange);

            OnOptionsChange(_optionsMonitor.CurrentValue);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _workerToken = cancellationToken;
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int sequentialFailures = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(Math.Min(sequentialFailures * 500, 5000), stoppingToken);

                _logger.LogInformation("Writing to trusted hosts");
                using var knownHostsFile = new TempFileRef();
                var knownHosts = BuildKnownHosts(_optionsMonitor.CurrentValue);
                File.WriteAllText(knownHostsFile.Path, knownHosts);

                _logger.LogInformation("Building command line arguments");
                var (launchArgs, optionsCancellationToken) = BuildLaunchArguments(_optionsMonitor.CurrentValue, knownHostsFile.Path);

                _logger.LogInformation("Launch args: {LaunchArgs}", launchArgs);
                var psi = new ProcessStartInfo()
                {
                    FileName = "/bin/bash",
                    Arguments = string.Format("-c \"ssh {0}\"", launchArgs),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                _logger.LogInformation("Starting SSH process");
                var process = Process.Start(psi);

                if (process == null)
                {
                    _logger.LogError("Failed to start SSH process");
                    break;
                }

                await DumpStream(process.StandardError, LogLevel.Error, optionsCancellationToken);
                await DumpStream(process.StandardOutput, LogLevel.Information, optionsCancellationToken);

                _logger.LogInformation("Waiting for SSH termination or app shutdown");
                await process.WaitForExitAsync(optionsCancellationToken);

                sequentialFailures = process.ExitCode == 0 ? 0 : sequentialFailures + 1;
                _logger.LogInformation("SSH process exited with code {ExitCode}", process.ExitCode);
            }
        }

        private void OnOptionsChange(TunManOptions newValue)
        {
            _logger.LogInformation("Options change event raised");
            if (newValue.Equals(_currentOptions))
            {
                _logger.LogInformation("Did not detect any changes, ignoring");
                return;
            }

            _currentOptions = newValue;
            _optionsCancellationTokenSource?.Cancel();
            _optionsCancellationTokenSource?.Dispose();
        }

        private (string, CancellationToken) BuildLaunchArguments(TunManOptions options, string knownHostsPath)
        {
            _privateKeyFile?.Dispose();
            _privateKeyFile = null;

            if (options.Tunnels is null || options.Tunnels.Length == 0)
            {
                throw new InvalidOperationException("No tunnels defined in configuration");
            }

            var serverAliveInterval = options.ServerAliveInterval.GetValueOrDefault(TunManOptions.DefaultServerAliveInterval);
            var serverAliveCountMax = options.ServerAliveCountMax.GetValueOrDefault(TunManOptions.DefaultServerAliveCountMax);
            var exitOnForwardFailure = options.ExitOnForwardFailure.GetValueOrDefault(TunManOptions.DefaultExitOnForwardFailure) ? "yes" : "no";
            var remoteHost = options.RemoteHost ?? throw new ArgumentNullException(nameof(options), "RemoteHost not set");
            var user = options.User ?? throw new ArgumentNullException(nameof(options), "User not set");
            var privateKeyPath = options.PrivateKeyPath;

            if (string.IsNullOrEmpty(privateKeyPath) && !string.IsNullOrEmpty(options.PrivateKeyContent))
            {
                _privateKeyFile = new();
                File.WriteAllText(_privateKeyFile.Path, options.PrivateKeyContent);

                privateKeyPath = _privateKeyFile.Path;
            }

            var args = new List<string>
            {
                "-NT",
                $"-o ServerAliveInterval={serverAliveInterval}",
                $"-o ServerAliveCountMax={serverAliveCountMax}",
                $"-o ExitOnForwardFailure={exitOnForwardFailure}",
                $"-o UserKnownHostsFile={knownHostsPath}",
                $"-i {privateKeyPath}",
            };

            _logger.LogInformation("Defining {TunnelCount} tunnel(s)", options.Tunnels.Length);

            foreach (var tunnel in options.Tunnels)
            {
                _logger.LogInformation("Tunnel found: {RemotePort} --> {LocalHost}:{LocalPort}", tunnel.RemotePort, tunnel.LocalHost, tunnel.LocalPort);
                var localHost = tunnel.LocalHost ?? throw new ArgumentNullException(nameof(tunnel), "LocalHost not set on tunnel options");
                var localPort = tunnel.LocalPort ?? throw new ArgumentNullException(nameof(tunnel), "LocalPort not set on tunnel options");
                var remotePort = tunnel.RemotePort ?? throw new ArgumentNullException(nameof(tunnel), "RemotePort not set on tunnel options");
                args.Add($"-R {remotePort}:{localHost}:{localPort}");
            }

            args.Add($"{user}@{remoteHost}");

            _optionsCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_workerToken);

            return (string.Join(' ', args), _optionsCancellationTokenSource.Token);
        }

        private string BuildKnownHosts(TunManOptions options)
        {
            var remoteHost = options.RemoteHost ?? throw new ArgumentNullException(nameof(options), "RemoteHost not set");

            if (options.RemoteHostKeys is null || options.RemoteHostKeys.Length == 0)
            {
                throw new InvalidOperationException("RemoteHostKeys is not configured so no ssh tunnel can be established");
            }

            var sb = new StringBuilder();

            foreach (var hostKey in options.RemoteHostKeys)
            {
                sb.AppendLine($"{remoteHost} {hostKey}");
            }

            return sb.ToString();
        }

        private async Task DumpStream(StreamReader streamReader, LogLevel logLevel, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await streamReader.ReadLineAsync();

                if (line == null)
                {
                    break;
                }

                _logger.Log(logLevel, "{RedirectedMessage}", line);
            }
        }
    }
}