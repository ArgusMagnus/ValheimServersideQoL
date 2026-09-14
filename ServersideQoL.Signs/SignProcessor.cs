using System.Text.RegularExpressions;

namespace ServersideQoL.Signs;


[Processor("806bdb85-c857-4154-a246-a0b1d0917987")]
public sealed class SignProcessor : Processor<ProcessorPrefabInfo<Sign>>
{
  internal static IReadOnlyList<string> ClockEmojis { get; } = ["🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠", "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"];
  readonly Regex _clockRegex = new($@"(?:{string.Join("|", ClockEmojis.Select(Regex.Escape))})(?:\s*\d\d\:\d\d)?");

  readonly Regex _defaultColorRegex = new(@"<color=[^>]+ d>");

  string? _timeText;

  protected override void PreProcess(PeersEnumerable peers)
  {
    _timeText = null;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<Sign> prefabInfo)
  {
    var cfg = Config.Instance;
    var result = ProcessResult.Default;
    var text = zdo.Vars.GetText();
    if (cfg.TimeSigns.Value)
    {
      var newText = _clockRegex.Replace(text, _ =>
      {
        result = ScheduleReprocessing(Config.Instance.Advanced.Value.ProcessingDelays.TimeSigns);
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
        return $"<color=\"{Config.Instance.DefaultColor.Value}\" d>";
      }, 1);

      if (!found && !string.IsNullOrEmpty(Config.Instance.DefaultColor.Value))
        newText = $"<color=\"{Config.Instance.DefaultColor.Value}\" d>{text}";

      if (newText != text)
        zdo.Vars.SetText(text = newText);
    }

    return result;
  }
}
