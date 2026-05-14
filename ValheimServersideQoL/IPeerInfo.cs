using static Skills;
using static Valheim.ServersideQoL.Processors.PlayerProcessor;

namespace Valheim.ServersideQoL;

interface IPeerInfo
{
    long Owner { get; }
    ExtendedZDO PlayerZDO { get; }
    long PlayerID { get; }
    string PlayerName { get; }
    bool IsAdmin { get; }
    float ConnectionQuality { get; }
    float GetEstimatedSkillLevel(SkillType skillType);
    ItemDrop? LastUsedItem { get; }
    BuildModifiers BuildModifiers { get; }
    LevelGroundModes LevelGroundMode { get; }
    IReadOnlyDictionary<GlobalKey, bool> GlobalKeyModifications { get; }
    void AddGlobalKeyModification(GlobalKey key, bool add);
    void RemoveGlobalKeyModification(GlobalKey key);
}
