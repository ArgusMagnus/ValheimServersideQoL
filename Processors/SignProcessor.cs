using BepInEx.Logging;
using System.Text.RegularExpressions;

namespace Valheim.ServersideQoL.Processors;

sealed class SignProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    internal static IReadOnlyList<string> ClockEmojis { get; } = ["🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠", "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"];
    readonly Regex _clockRegex = new($@"(?:{string.Join("|", ClockEmojis.Select(Regex.Escape))})(?:\s*\d\d\:\d\d)?");
    readonly HashSet<ExtendedZDO> _timeSigns = [];

    string? _prevTimeText;
    string? _timeText;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _timeSigns.Remove(zdo);
    }

    public override void PreProcess()
    {
        base.PreProcess();
        _timeText = null;

        var dayFraction = EnvMan.instance.GetDayFraction();
        var emojiIdx = (int)Math.Floor(ClockEmojis.Count * 2 * dayFraction) % ClockEmojis.Count;
        var time = TimeSpan.FromDays(dayFraction);
        var timeText = $@"{ClockEmojis[emojiIdx]} {time:hh\:mm}";
        if (timeText != _prevTimeText)
            _prevTimeText = _timeText = timeText;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        if (zdo.PrefabInfo.Sign is null || !Config.Signs.TimeSigns.Value)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (_timeText is not null)
        {
            var text = zdo.Vars.GetText();
            var newText = _clockRegex.Replace(text, _timeText);
            if (text != newText)
            {
                zdo.Vars.SetText(newText);
                //zdo.Set(ZDOVars.s_author, );
                _timeSigns.Add(zdo);
                return false;
            }
        }

        return !_timeSigns.Contains(zdo);
    }
}