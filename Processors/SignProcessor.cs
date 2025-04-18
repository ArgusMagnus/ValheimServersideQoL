﻿using BepInEx.Logging;
using System.Text.RegularExpressions;

namespace Valheim.ServersideQoL.Processors;

sealed class SignProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    internal static IReadOnlyList<string> ClockEmojis { get; } = ["🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠", "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"];
    readonly Regex _clockRegex = new($@"(?:{string.Join("|", ClockEmojis.Select(Regex.Escape))})(?:\s*\d\d\:\d\d)?");

    string? _timeText;

    public override void PreProcess()
    {
        base.PreProcess();
        _timeText = null;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        if (zdo.PrefabInfo.Sign is null || !Config.Signs.TimeSigns.Value)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        var text = zdo.Vars.GetText();
        var newText = _clockRegex.Replace(text, match =>
        {
            if (_timeText is null)
            {
                var dayFraction = EnvMan.instance.GetDayFraction();
                var emojiIdx = (int)Math.Floor(ClockEmojis.Count * 2 * dayFraction) % ClockEmojis.Count;
                var time = TimeSpan.FromDays(dayFraction);
                _timeText = $@"{ClockEmojis[emojiIdx]} {time:hh\:mm}";
            }
            return _timeText;
        });

        if (text != newText)
        {
            zdo.Vars.SetText(newText);
            //zdo.Set(ZDOVars.s_author, );
        }

        return false;
    }
}