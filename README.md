# ![NetSonar Logo](https://raw.githubusercontent.com/sn4k3/NetSonar/refs/heads/main/media/NetSonar-32.png) NetSonar

[![License](https://img.shields.io/github/license/sn4k3/NetSonar?style=for-the-badge)](https://github.com/sn4k3/UVtools/blob/master/LICENSE)
[![GitHub repo size](https://img.shields.io/github/repo-size/sn4k3/NetSonar?style=for-the-badge)](#)
[![Code size](https://img.shields.io/github/languages/code-size/sn4k3/NetSonar?style=for-the-badge)](#)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/sn4k3/NetSonar?include_prereleases&style=for-the-badge)](https://github.com/sn4k3/NetSonar/releases)
[![Downloads](https://img.shields.io/github/downloads/sn4k3/NetSonar/total?style=for-the-badge)](https://github.com/sn4k3/NetSonar/releases)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/sn4k3?color=red&style=for-the-badge)](https://github.com/sponsors/sn4k3)
<!--[![Chocolatey](https://img.shields.io/chocolatey/dt/NetSonar?color=brown&label=Chocolatey&style=for-the-badge)](https://community.chocolatey.org/packages/NetSonar)!-->

NetSonar is a network diagnostics and monitoring tool for probing hosts and services with ICMP, TCP, UDP, TLS, DNS, NTP, HTTP, WebSocket, SSH, SMTP, IMAP, MQTT, STUN, and SIP.
It provides protocol-aware latency checks, live single-service and multi-service response-time charts, network-interface inspection and configuration, and internet speed testing.
Designed for administrators and developers needing lightweight, cross-platform network analysis.

## Download the latest version at:

## To auto-install on Windows (package manager):

- **Winget:** `winget install -e --id PTRTECH.NetSonar`
- Winget is included on Windows 10 with recent updates and Windows 11 by default.

## To auto-install on Linux:

```bash
[ "$(command -v apt)" -a -z "$(command -v curl)" ] && sudo apt-get install -y curl 
[ "$(command -v dnf)" -a -z "$(command -v curl)" ] && sudo dnf install -y curl
[ "$(command -v pacman)" -a -z "$(command -v curl)" ] && sudo pacman -S curl
[ "$(command -v zypper)" -a -z "$(command -v curl)" ] && sudo zypper install -y curl
bash -c "$(curl -fsSL https://raw.githubusercontent.com/sn4k3/NetSonar/main/scripts/install-netsonar.sh)"
```

## To auto-install on macOS:

```bash
bash -c "$(curl -fsSL https://raw.githubusercontent.com/sn4k3/NetSonar/main/scripts/install-netsonar.sh)"
```

## To downgrade to a previous version:

```bash
# Replace x.x.x by the version you want to install
bash -c "$(curl -fsSL https://raw.githubusercontent.com/sn4k3/NetSonar/main/scripts/install-netsonar.sh)" -- x.x.x
```

## Features

- **Network Probes**: Monitor host, transport, and application-protocol availability and latency with configurable intervals, timeouts, and reply history.
- **Response-Time Analysis**: Compare live single-service and multi-service charts with rolling averages, percentiles, jitter, loss, failures, scaling, and selectable sample windows.
- **Interface Management**: View and manage network interfaces, including IP configuration and statistics.
- **Speed Test**: Run manual or scheduled internet speed tests with latency, download, and upload results.
- **Cross-Platform**: Built with [C# dotnet](https://dotnet.microsoft.com/en-us/), runs on Windows, macOS, and Linux. 
- **Modern UI**: Built with [Avalonia](https://avaloniaui.net) and [SukiUI](https://github.com/kikipoulet/SukiUI), featuring Fluent themes.
- **Charts and Visualizations**: Uses LiveCharts for real-time data visualization.
- **Customizable**: Supports themes and UI customization.
- **Open Source**: Contributions are welcome!

## Network probe protocols

Choose the highest-level protocol that represents the service you need to monitor. An ICMP reply only confirms that a host answers echo requests, while a protocol-aware probe also confirms that the expected application is responding correctly.

After selecting a protocol in the Add Ping Services dialog, enter its target in the format shown below.

| Protocol | Target and default | What NetSonar checks | Choose it when |
| --- | --- | --- | --- |
| **ICMP** | `router.local` or `192.168.1.1`; no port | Sends an ICMP echo request and measures its reply. Payload size, TTL, and the Don't Fragment option are configurable where the operating system supports them. | You need basic host reachability or path latency. A failed probe does not necessarily mean the host is offline because firewalls commonly block ICMP. |
| **TCP** | `host:port`; port required | Opens a TCP connection and reports success when the connection is established. An optional payload can be sent, but no application response is validated. | You only need to know whether a TCP port accepts connections, or NetSonar has no protocol-specific probe for the service. |
| **UDP** | `host:port`; port required | Sends the configured payload and waits for a reply from the remote endpoint. Sending alone is not considered success. | The service uses a request/reply UDP protocol. Do not use it for services that normally send no response to an arbitrary payload. |
| **TLS** | `host` or `host:port`; default `443/TCP` | Performs an SNI-aware TLS handshake using the operating system's certificate trust and hostname validation, then reports the negotiated TLS version. | You need to validate TLS independently of an application protocol, including certificate trust, hostname, and validity failures. |
| **DNS** | `host` or `host:port`; default `53/UDP` | Queries the server for the `A` record of `example.com` and validates the transaction ID, response flags and code, echoed question, and answer count. | You need to confirm that a DNS resolver can answer a real DNS query, rather than only checking whether its host is reachable. |
| **NTP** | `host` or `host:port`; default `123/UDP` | Sends an NTP client request and validates the response mode, version, synchronization state, stratum, timestamps, and request correlation. | You need to confirm that a time server is synchronized and returning valid NTP responses. |
| **HTTP** | `http://host/path` or `https://host/path`; HTTP is assumed if the scheme is omitted | Sends a GET request and completes after receiving the response headers without downloading the body. HTTP success status codes are treated as successful probes. | You need to validate a website, API route, reverse proxy, or TLS-enabled HTTPS endpoint. |
| **WebSocket** | `ws://host/path` or `wss://host/path`; defaults `80` and `443` | Performs the WebSocket HTTP upgrade and requires the connection to reach the `Open` state. Paths, custom ports, and TLS through `wss://` are supported. | You need to validate a WebSocket endpoint rather than only its underlying HTTP or TCP listener. |
| **SSH** | `host` or `host:port`; default `22/TCP` | Sends a NetSonar SSH 2.0 identification, tolerates permitted server pre-banner lines, and requires a valid `SSH-2.0-` server identification. It does not authenticate or start key exchange. | You need to confirm that an SSH 2.0 service is responding instead of only checking whether port 22 is open. |
| **SMTP** | `host` or `host:port`; default `25/TCP` | Connects and validates a complete single-line or multiline SMTP `220` greeting. It does not start TLS or authenticate. | You need to confirm that a plain SMTP listener is ready. Custom ports such as `587` work when the server sends its greeting before STARTTLS. |
| **IMAP** | `host` or `host:port`; defaults `143/TCP`, with implicit TLS on `993/TCP` | Requires an OK or PREAUTH greeting, sends a tagged CAPABILITY command, and validates both the capability data and successful tagged completion. Port `993` negotiates TLS with certificate and hostname validation before the IMAP exchange. It does not issue STARTTLS or authenticate. | You need to validate a cleartext or implicit-TLS IMAP listener without accessing a mailbox. |
| **MQTT** | `host` or `host:port`; default `1883/TCP` | Sends an MQTT 3.1.1 CONNECT packet with a randomized client ID and clean session, then requires an accepted CONNACK response. It does not use TLS or credentials. | You need to validate an anonymous, plain-TCP MQTT broker. Brokers that require authentication correctly reject this probe. |
| **STUN** | `host` or `host:port`; default `3478/UDP` | Sends a Binding request and validates the response type, declared length, magic cookie, transaction ID, attribute framing, and mapped address. | You need to validate a STUN server or diagnose UDP and NAT traversal availability. |
| **SIP** | `host` or `host:port`; default `5060/UDP` | Sends an unauthenticated OPTIONS request, correlates Via branch, Call-ID, and CSeq fields, ignores provisional responses, and requires a successful final response. | You need to monitor a SIP server, PBX, proxy, or VoIP endpoint that accepts OPTIONS over UDP. |

For text-list imports, use `icmp://`, `tcp://`, `udp://`, `tls://`, `dns://`, `ntp://`, `ssh://`, `smtp://`, `imap://`, `mqtt://`, `stun://`, and `sip://` to identify non-HTTP protocols. HTTP and WebSocket entries retain their normal `http://`, `https://`, `ws://`, or `wss://` URLs. Examples:

```text
icmp://router.local
tcp://database.local:5432
udp://service.local:9000
tls://service.example.com:443
dns://1.1.1.1
ntp://time.cloudflare.com
https://example.com/health
wss://example.com/events
ssh://server.local
smtp://mail.example.com:25
imap://mail.example.com
mqtt://broker.local:1883
stun://stun.example.com
sip://pbx.local
```

The Add Ping Services dialog provides a separate public-host import for TLS, DNS, NTP, HTTP, WebSocket, SSH, SMTP, IMAP, MQTT, STUN, and SIP. DNS and NTP use specialized provider catalogues; the other protocols use the shared public-host catalogue. These endpoints are connectivity examples and external-service checks, so availability and access policies remain controlled by each provider.

## Response-time charts

Selecting one service displays its latency history, failure markers, rolling average over the latest 10 successful samples, and statistics for the current reply, P50 median, P95, jitter, and loss. The chart supports 50, 100, and 250-sample windows, the configured cache size, or all retained replies. Automatic scale emphasizes typical latency while marking values above the visible range; a full scale includes every displayed value.

Selecting multiple services switches to a comparison chart with a row for each host and markers for minimum, average, current, maximum, and failure states. Hovering a row shows the consolidated values, loss, and latest reply status for that host.

The graph can be frozen without stopping probes and opened in a separate window. Main and pop-out controls change the sample window, scale, and enabled state of all graphed services. The pop-out **Graph options** section also applies probe interval and timeout changes to the current selection.

# Screenshots

![NetSonar Pings](https://raw.githubusercontent.com/sn4k3/NetSonar/refs/heads/main/media/screenshots/NetSonar_screenshot_pings.png)
![NetSonar Pings Multi](https://raw.githubusercontent.com/sn4k3/NetSonar/refs/heads/main/media/screenshots/NetSonar_screenshot_pings_multi.png)
![NetSonar Pings Chart](https://raw.githubusercontent.com/sn4k3/NetSonar/refs/heads/main/media/screenshots/NetSonar_screenshot_pings_chart.png)
![NetSonar Interfaces](https://raw.githubusercontent.com/sn4k3/NetSonar/refs/heads/main/media/screenshots/NetSonar_screenshot_interfaces.png)


# Requirements

- Windows 10 or greater
- macOS 13 Monterey or greater
- Linux (Debian, Ubuntu, Fedora, Arch, etc.)
- 64-bit System (x64 / arm64)
- 4GB RAM or higher
- 1920 x 1080 @ 100% scale as minimum resolution

# Run arguments

NetSonar can be run with the following arguments:

- `--portable [level]`: Run in portable mode, configurations are saved near the executable. Use level to specify the directory level, e.g. `0` for the current directory, `1` for the parent directory, etc.
  - `NetSonar.exe --portable` will save the configuration in the same directory as the executable.
- `--profile-path <path>`: Specify the path to the profile file.
  - `NetSonar.exe --profile-path D:\NetSonarConfigs` will use the profile file at the specified path. 
  - `NetSonar.exe --profile-path NetSonarConfigs` will use the profile path relative to the executable path.

Note: Both `--portable` and `--profile-path` can be used together, but `--profile-path` will take precedence if both are specified.

# Support my work / Donate

All my work here is given for free (OpenSource), it took some hours to build, test, and polish the program.
If you're happy to contribute to a better program and for my work, I will appreciate the tip.  
Use one of the following methods:

[![GitHub Sponsors](https://img.shields.io/badge/Donate-Sponsor-red?style=for-the-badge)](https://github.com/sponsors/sn4k3)
[![Donate PayPal](https://img.shields.io/badge/Donate-PayPal-blue?style=for-the-badge)](https://www.paypal.com/donate/?hosted_button_id=5YCNRCYFRS4GG)

# Contributors 

[![GitHub contributors](https://img.shields.io/github/contributors/sn4k3/NetSonar?style=for-the-badge)](https://github.com/sn4k3/NetSonar/graphs/contributors)  
[![Contributors](https://contrib.rocks/image?repo=sn4k3/NetSonar)](https://github.com/sn4k3/NetSonar/graphs/contributors)
