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
    readonly Regex _incineratorTagRegex = new($@"{Regex.Escape(LinkEmoji)}\s*(?<T>\w*)");

    internal static IReadOnlyList<string> ClockEmojis { get; } = ["🕛", "🕧", "🕐", "🕜", "🕑", "🕝", "🕒", "🕞", "🕓", "🕟", "🕔", "🕠", "🕕", "🕡", "🕖", "🕢", "🕗", "🕣", "🕘", "🕤", "🕙", "🕥", "🕚", "🕦"];
    readonly Regex _clockRegex = new($@"(?:{string.Join("|", ClockEmojis.Select(Regex.Escape))})(?:\s*\d\d\:\d\d)?");

    readonly Regex _defaultColorRegex = new(@"<color=#?(?<V>\w+)>");
    Regex _contentListRegex = default!;

    string? _timeText;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        var bullet = Regex.Escape(Config.Containers.ChestSignsContentListBullet.Value);
        var separator = Regex.Escape(Config.Containers.ChestSignsContentListSeparator.Value);
        var rest = Regex.Escape(Config.Containers.ChestSignsContentListNameRest.Value);
        //var entry = $@"(?:(?:[A-Za-z\s]+)|(?:{rest})) \d+";
        var names = Config.Containers.ItemNames.Values.Append(Config.Containers.ChestSignsContentListNameRest.Value).ToHashSet();
        var entry = $@"(?:{string.Join('|', names.Select(x => $"(?:{Regex.Escape(x)})"))}) +\d+";
        _contentListRegex = new($@"(?:{bullet}{entry}{separator})*{bullet}(?:{entry})?");

        if (!firstTime)
            return;

        Instance<ContainerProcessor>().ContainerChanged += OnContainerChanged;
    }

    void OnContainerChanged(ExtendedZDO zdo)
    {
        if (!Instance<ContainerProcessor>().SignsByChests.TryGetValue(zdo, out var signs))
            return;
        foreach (var sign in signs)
            sign.ResetProcessorDataRevision(this);
    }

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        _timeText = null;
    }

    public override bool ClaimExclusive(ExtendedZDO zdo)
        => base.ClaimExclusive(zdo) || Instance<ContainerProcessor>().ChestsBySigns.ContainsKey(zdo);

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (zdo.PrefabInfo.Sign is null)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        var isTimeSign = false;
        var text = zdo.Vars.GetText();
        if (Config.Signs.TimeSigns.Value)
        {
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
                zdo.Vars.SetText(text = newText);
                //zdo.Set(ZDOVars.s_author, );
            }
        }

        {
            var found = false;
            var newText = _defaultColorRegex.Replace(text, match =>
            {
                found = true;
                var value = match.Groups["V"].Value;
                if (!string.Equals(value, zdo.Vars.GetDefaultColor(), StringComparison.OrdinalIgnoreCase))
                    return match.Value;
                if (string.IsNullOrEmpty(Config.Signs.DefaultColor.Value))
                    return "";
                return $"<color={Config.Signs.DefaultColor.Value}>";
            }, 1);

            if (!found && !string.IsNullOrEmpty(Config.Signs.DefaultColor.Value))
                newText = $"<color={Config.Signs.DefaultColor.Value}>{text}";

            if (newText != text)
            {
                zdo.Vars.SetText(text = newText);
                zdo.Vars.SetDefaultColor(Config.Signs.DefaultColor.Value);
            }
        }

        if (Instance<ContainerProcessor>().ChestsBySigns.TryGetValue(zdo, out var chest))
        {
#if DEBUG
            if (text.Length > 200)
                zdo.Vars.SetText(text = "");
#endif
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
            if (Config.Containers.ObliteratorItemTeleporter.Value is not ModConfig.ContainersConfig.ObliteratorItemTeleporterOptions.Disabled
                && chest.PrefabInfo.Container is { Incinerator.Value: not null })
            {
                if (_incineratorTagRegex.Match(text) is { Success: true } match)
                    chest.Inventory.TeleportTag = match.Groups["T"].Value;
                else
                    chest.Inventory.TeleportTag = null;
            }

            var newText = _contentListRegex.Replace(text, match =>
            {
                if (Config.Containers.ChestSignsContentListMaxCount.Value <= 0 || chest.InventoryReadOnly.Items.Count is 0)
                    return Config.Containers.ChestSignsContentListBullet.Value;

                var list = chest.InventoryReadOnly.Items
                    .GroupBy(x => x.m_dropPrefab.name, (k, g) => (Name: Config.Containers.ItemNames[k], Count: g.Sum(x => x.m_stack)))
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var items = list.AsEnumerable();
                if (list.Count > Config.Containers.ChestSignsContentListMaxCount.Value)
                {
                    items = list
                        .Take(Config.Containers.ChestSignsContentListMaxCount.Value - 1)
                        .Append((Config.Containers.ChestSignsContentListNameRest.Value, list.Skip(Config.Containers.ChestSignsContentListMaxCount.Value - 1).Sum(x => x.Count)));
                }

                return string.Join(Config.Containers.ChestSignsContentListSeparator.Value, items
                    .Select(x => $"{Config.Containers.ChestSignsContentListBullet.Value}{x.Name} {x.Count}"));
            }, count: 1);

            if (newText != text)
                zdo.Vars.SetText(text = newText);

            var defaultColor = zdo.Vars.GetDefaultColor();
            if (text != chest.Vars.GetText() || defaultColor != chest.Vars.GetDefaultColor())
            {
                if (!chest.IsOwnerOrUnassigned())
                {
                    Instance<ContainerProcessor>().RequestOwnership(chest, 0);
                    return false;
                }

                chest.Vars.SetText(text);
                chest.Vars.SetDefaultColor(defaultColor);
            }
        }
        
        if (isTimeSign)
            return false;

        return true;
    }
}