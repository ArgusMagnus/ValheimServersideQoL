using BepInEx.Logging;
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
        if (zdo.PrefabInfo.Sign is null)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        var isTimeSign = false;
        string? text = null;
        if (Config.Signs.TimeSigns.Value)
        {
            text = zdo.Vars.GetText();
            var newText = _clockRegex.Replace(text, _ =>
            {
                isTimeSign = true;
                if (_timeText is null)
                {
                    var dayFraction = EnvMan.instance.GetDayFraction();
                    var emojiIdx = (int)Math.Floor(ClockEmojis.Count * 2 * dayFraction) % ClockEmojis.Count;
                    var time = TimeSpan.FromDays(dayFraction);
                    _timeText = $@"{ClockEmojis[emojiIdx]} {time:hh\:mm}";
                }
                return _timeText;
            });
            if (newText != text)
            {
                zdo.Vars.SetText(newText);
                //zdo.Set(ZDOVars.s_author, );
            }
        }

        if (Instance<ContainerProcessor>().ChestsBySigns.TryGetValue(zdo, out var chest))
        {
            text ??= zdo.Vars.GetText();
            Logger.LogWarning($"Set chest text: {text}");
            chest.Vars.SetText(text);
        }

        if (isTimeSign)
            return false;

        return true;
    }
}