

using System.Text.RegularExpressions;

namespace Valheim.ServersideQoL.Signs;

sealed record PrefabInfo(Sign Sign) : PrefabInfoBase;

sealed class SignProcessor : Processor<PrefabInfo>
{
    protected override Guid Id { get; } = Guid.Parse("806bdb85-c857-4154-a246-a0b1d0917987");

    internal static IReadOnlyList<string> ClockEmojis { get; } = ["🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠", "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"];
    readonly Regex _clockRegex = new($@"(?:{string.Join("|", ClockEmojis.Select(Regex.Escape))})(?:\s*\d\d\:\d\d)?");

    readonly Regex _defaultColorRegex = new(@"<color=[^>]+ d>");
    string _defaultColor = "";

    string? _timeText;

    protected override void PreProcess(PeersEnumerable peers)
    {
        _timeText = null;
    }

    protected override ProcessResult Process(ZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
    {
        var cfg = Config.Instance;
        var result = ProcessResult.WaitForZDORevisionChange;
        var text = zdo.Vars.GetText();
        if (cfg.TimeSigns.Value)
        {
            var newText = _clockRegex.Replace(text, _ =>
            {
                result = ProcessResult.Default;
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
                zdo.Vars.SetText(text = newText);
                //zdo.Set(ZDOVars.s_author, );
            }
        }

        {
            var found = false;
            var newText = _defaultColorRegex.Replace(text, match =>
            {
                found = true;
                return $"<color={_defaultColor} d>";
            }, 1);

            if (!found && !string.IsNullOrEmpty(_defaultColor))
                newText = $"<color={_defaultColor} d>{text}";

            if (newText != text)
                zdo.Vars.SetText(text = newText);
        }

        return result;
    }
}
