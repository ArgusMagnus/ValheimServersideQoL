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

    readonly Regex _defaultColorRegex = new(@"<color=[^>]+ d>");
    string _defaultColor = "";

    const string ContentListStart = "<i ls></i>";
    const string ContentListEnd = "<i le></i>";
    readonly Regex _contentListRegex = new($@"{ContentListStart}.*?{ContentListEnd}");
    Regex _contentListRegex2 = default!;

    string? _timeText;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        _contentListRegex2 = new(Regex.Escape(Config.Containers.ChestSignsContentListPlaceholder.Value));

        _defaultColor = Config.Signs.DefaultColor.Value.StartsWith('#') ? Config.Signs.DefaultColor.Value :
            string.IsNullOrEmpty(Config.Signs.DefaultColor.Value) ? "" : $"\"{Config.Signs.DefaultColor.Value}\"";

        if (!firstTime)
            return;

        Instance<ContainerProcessor>().ContainerChanged -= OnContainerChanged;
        Instance<ContainerProcessor>().ContainerChanged += OnContainerChanged;
    }

    void OnContainerChanged(ExtendedZDO zdo)
    {
        if (!Instance<ContainerProcessor>().SignsByChests.TryGetValue(zdo, out var signs))
            return;
        var text = zdo.Vars.GetText();
        foreach (var sign in signs)
        {
            sign.Vars.SetText(text);
            sign.ResetProcessorDataRevision(this);
        }
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
                return $"<color={_defaultColor} d>";
            }, 1);

            if (!found && !string.IsNullOrEmpty(_defaultColor))
                newText = $"<color={_defaultColor} d>{text}";

            if (newText != text)
                zdo.Vars.SetText(text = newText);
        }

        if (Instance<ContainerProcessor>().ChestsBySigns.TryGetValue(zdo, out var chest))
        {
            var newText = text;
            if (Config.Containers.AutoPickup.Value)
            {
                chest.Inventory.PickupRange = null;
                newText = _chestPickupRangeRegex.Replace(newText, match =>
                {
                    var result = match.Value;
                    var range = int.Parse(match.Groups["R"].Value);
                    if (range > Config.Containers.AutoPickupMaxRange.Value)
                    {
                        range = Config.Containers.AutoPickupMaxRange.Value;
                        result = Invariant($"{MagnetEmoji}{range}");
                    }
                    chest.Inventory.PickupRange = range;
                    return result;
                });
            }
            if (Config.Smelters.FeedFromContainers.Value)
            {
                chest.Inventory.FeedRange = null;
                newText = _chestFeedRangeRegex.Replace(newText, match =>
                {
                    var result = match.Value;
                    var range = int.Parse(match.Groups["R"].Value);
                    if (range > Config.Smelters.FeedFromContainersMaxRange.Value)
                    {
                        range = Config.Smelters.FeedFromContainersMaxRange.Value;
                        result = Invariant($"{LeftRightArrowEmoji}{range}");
                    }
                    chest.Inventory.FeedRange = range;
                    return result;
                });
            }

            int tag = 0;
            if (Config.Containers.ObliteratorItemTeleporter.Value is not ModConfig.ContainersConfig.ObliteratorItemTeleporterOptions.Disabled
                && chest.PrefabInfo.Container is { Incinerator.Value: not null })
            {
                if (_incineratorTagRegex.Match(newText) is { Success: true } match)
                    tag = match.Groups["T"].Value.GetStableHashCode();
            }

            var found = false;
            string EvaluateMatch(Match match)
            {
                found = true;
                if (Config.Containers.ChestSignsContentListMaxCount.Value <= 0 || chest.InventoryReadOnly.Items.Count is 0)
                    return Config.Containers.ChestSignsContentListPlaceholder.Value;

                var list = chest.InventoryReadOnly.Items
                    .GroupBy(static x => x.m_dropPrefab.name, (k, g) => (Name: Config.Containers.ItemNames[k], Count: g.Sum(static x => x.m_stack)))
                    .OrderByDescending(static x => x.Count)
                    .ToList();

                var items = list.AsEnumerable();
                if (list.Count > Config.Containers.ChestSignsContentListMaxCount.Value)
                {
                    items = list
                        .Take(Config.Containers.ChestSignsContentListMaxCount.Value - 1)
                        .Append((Config.Containers.ChestSignsContentListNameRest.Value, list.Skip(Config.Containers.ChestSignsContentListMaxCount.Value - 1).Sum(static x => x.Count)));
                }

                var listStr = string.Join(Config.Containers.ChestSignsContentListSeparator.Value, items
                    .Select(x => string.Format(Config.Containers.ChestSignsContentListEntryFormat.Value, x.Name, x.Count)));

                return $"{ContentListStart}{listStr}{ContentListEnd}";
            }

            newText = _contentListRegex.Replace(newText, EvaluateMatch, 1);
            if (!found)
                newText = _contentListRegex2.Replace(newText, EvaluateMatch, 1);

            if (newText != text)
                zdo.Vars.SetText(text = newText);

            if (text != chest.Vars.GetText() ||tag != chest.Vars.GetIntTag())
            {
                if (!chest.IsOwnerOrUnassigned())
                {
                    Instance<ContainerProcessor>().RequestOwnership(chest, 0);
                    return false;
                }

                chest.Vars.SetText(text);
                if (tag is 0)
                    chest.Vars.RemoveIntTag();
                else
                    chest.Vars.SetIntTag(tag);
            }
        }
        
        if (isTimeSign)
            return false;

        return true;
    }
}