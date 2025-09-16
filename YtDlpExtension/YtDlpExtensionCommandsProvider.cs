// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using YtDlpExtension.Helpers;

namespace YtDlpExtension;

public partial class YtDlpExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly IconInfo _logoIcon = IconHelpers.FromRelativePath("Assets\\CmdPal-YtDlp.png");
    private static SettingsManager _settings = new();
    private DownloadHelper _ytDlp = new(_settings);

    public YtDlpExtensionCommandsProvider()
    {

        DisplayName = "Video Downloader";
        Settings = _settings.Settings;
        var settingsPage = _settings.Settings.SettingsPage;
        Icon = _logoIcon;
        var (isAvailable, version) = SettingsManager.IsYtDlpBinaryAvailable();
        if (isAvailable && version != "0")
        {
            _ytDlp.IsAvailable = isAvailable;
            _ytDlp.Version = version;
        }
        _commands = [
            new CommandItem(new YtDlpExtensionPage(_settings, _ytDlp))
            {
                Title = DisplayName,
                Icon = _logoIcon,
                MoreCommands =
                [
                    new CommandContextItem(settingsPage)
                    {
                        Title = "Settings".ToLocalized(),
                        Subtitle = "Configure the video downloader settings",
                    },
                ],
            },
        ];
    }

    public override void InitializeWithHost(IExtensionHost host)
    {
        YtDlpExtensionHost.Register(host);
        base.InitializeWithHost(host);
    }

    public override ICommandItem[] TopLevelCommands() => _commands;

    public override IFallbackCommandItem[] FallbackCommands() => [];
}
