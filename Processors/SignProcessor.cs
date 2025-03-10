using BepInEx.Logging;
using System.Text.RegularExpressions;

namespace Valheim.ServersideQoL.Processors;

sealed class SignProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    internal static IReadOnlyList<string> ClockEmojis { get; } = ["🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠", "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"];
    readonly Regex _clockRegex = new($@"(?:{string.Join("|", ClockEmojis.Select(Regex.Escape))})(?:\s*\d\d\:\d\d)?");

    string? _timeText;

    public override void PreProcess()
    {
        base.PreProcess();
        _timeText = null;
    }

    protected override void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (!Config.Signs.TimeSigns.Value || prefabInfo.Sign is null)
            return;

        var text = zdo.GetString(ZDOVars.s_text);
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
            Logger.LogDebug($"Changing sign text from '{text}' to '{newText}'");
            zdo.Set(ZDOVars.s_text, newText);
            //zdo.Set(ZDOVars.s_author, );
        }
    }
}