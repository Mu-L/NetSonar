# ![NetSonar Logo](https://raw.githubusercontent.com/sn4k3/NetSonar/refs/heads/main/media/NetSonar-32.png) NetSonar

[![License](https://img.shields.io/github/license/sn4k3/NetSonar?style=for-the-badge)](https://github.com/sn4k3/UVtools/blob/master/LICENSE)
[![GitHub repo size](https://img.shields.io/github/repo-size/sn4k3/NetSonar?style=for-the-badge)](#)
[![Code size](https://img.shields.io/github/languages/code-size/sn4k3/NetSonar?style=for-the-badge)](#)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/sn4k3/NetSonar?include_prereleases&style=for-the-badge)](https://github.com/sn4k3/NetSonar/releases)
[![Downloads](https://img.shields.io/github/downloads/sn4k3/NetSonar/total?style=for-the-badge)](https://github.com/sn4k3/NetSonar/releases)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/sn4k3?color=red&style=for-the-badge)](https://github.com/sponsors/sn4k3)
<!--[![Chocolatey](https://img.shields.io/chocolatey/dt/NetSonar?color=brown&label=Chocolatey&style=for-the-badge)](https://community.chocolatey.org/packages/NetSonar)!-->

NetSonar is a network diagnostics tool for pinging hosts (ICMP/TCP/UDP/HTTP), managing network interfaces, and discovering local devices/services.  
Features multiprotocol latency checks, subnet scanning, port/service detection, and real-time interface configuration.  
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

- **Network Pings**: Perform ICMP/TCP/UDP/HTTP pings to check the availability and latency of network devices.
- **Interface Management**: View and manage network interfaces, including IP configuration and statistics.
- **Cross-Platform**: Built with [C# dotnet](https://dotnet.microsoft.com/en-us/), runs on Windows, macOS, and Linux. 
- **Modern UI**: Built with [Avalonia](https://avaloniaui.net) and [SukiUI](https://github.com/kikipoulet/SukiUI), featuring Fluent themes.
- **Charts and Visualizations**: Uses LiveCharts for real-time data visualization.
- **Customizable**: Supports themes and UI customization.
- **Open Source**: Contributions are welcome!

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
