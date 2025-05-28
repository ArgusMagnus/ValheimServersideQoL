using BepInEx.Logging;
using System.Text.RegularExpressions;

namespace Valheim.ServersideQoL.Processors;

sealed class SignProcessor : Processor
{
    internal const string MagnetEmoji = "🧲";
    readonly Regex _chestPickupRangeRegex = new($@"{Regex.Escape(MagnetEmoji)}\s*(?<R>\d+)");

    internal const string LeftRightArrowEmoji = "↔️";
    readonly Regex _chestFeedRangeRegex = new($@"{Regex.Escape(LeftRightArrowEmoji)}\s*(?<R>\d+)");
    
    internal const string LinkEmoji = "🔗";
    readonly Regex _incineratorTagRegex = new($@"{Regex.Escape(LinkEmoji)}\s*(?<T>\w+)");

    internal static IReadOnlyList<string> ClockEmojis { get; } = ["🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠", "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"];
    readonly Regex _clockRegex = new($@"(?:{string.Join("|", ClockEmojis.Select(Regex.Escape))})(?:\s*\d\d\:\d\d)?");

    string? _timeText;

    protected override void PreProcessCore()
    {
        base.PreProcessCore();
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
            //Logger.LogWarning($"Set chest text: {text} / {zdo.DataRevision}");
            chest.Vars.SetText(text);
            if (Config.Containers.AutoPickup.Value)
            {
                if (_chestPickupRangeRegex.Match(text) is { Success: true } match)
                    chest.Inventory.PickupRange = int.Parse(match.Groups["R"].Value);
                else
                    chest.Inventory.PickupRange = null;
            }
            if (Config.Smelters.FeedFromContainers.Value)
            {
                if (_chestFeedRangeRegex.Match(text) is { Success: true } match)
                    chest.Inventory.FeedRange = int.Parse(match.Groups["R"].Value);
                else
                    chest.Inventory.FeedRange = null;
            }
            if (Config.Containers.ObliteratorItemTeleporter.Value && chest.PrefabInfo.Container is { Incinerator.Value: not null })
            {
                if (_incineratorTagRegex.Match(text) is { Success: true } match)
                    chest.Inventory.TeleportTag = match.Groups["T"].Value;
                else
                    chest.Inventory.TeleportTag = null;
            }
        }

        if (isTimeSign)
            return false;

        return true;
    }
}