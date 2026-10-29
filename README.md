# Vexstrap

Vexstrap is a third-party Windows bootstrapper for Roblox, with options for customizing the experience. It is not affiliated with Roblox.

## Features

- Discord Rich Presence to show what you're playing.
- Support for customizing content such as sounds and mouse cursors.
- Server location information provided by [ipinfo.io](https://ipinfo.io).
- Settings for graphics and the user interface.

## Installation

Download an available build from this repository's [Releases page](https://github.com/LeonardoDecap/Vexstrap/releases) and run it on Windows. The current source requires the [.NET 6 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0). Follow the installer's prompts to configure Vexstrap, then use its Start menu entry to change settings later.

Changes on development branches are source code and are not necessarily included in a published release. Check the release notes to see what a download contains.

## Building from source

The repository uses the .NET 6 SDK, with the SDK version specified in `global.json`, and includes WPF UI as a Git submodule. On Windows, clone the repository with its submodules, then build the solution:

```powershell
git clone --recurse-submodules https://github.com/LeonardoDecap/Vexstrap.git
cd Vexstrap
dotnet build Vexstrap.sln
```

If you've already cloned the repository, run `git submodule update --init --recursive` before building.

## Help and issues

For problems or suggestions, open an issue in the [Vexstrap repository](https://github.com/LeonardoDecap/Vexstrap/issues). Include the relevant steps and error details, and remove personal information from any logs before sharing them.

Third-party tools can be affected by changes to Roblox or its policies. Check Roblox's current terms and policies when deciding whether to use them.

## License

See [LICENSE](LICENSE).
