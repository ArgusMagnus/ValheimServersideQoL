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
    public ItemDrop? LastUsedItem { get; }
    public BuildModifiers BuildModifiers { get; }
    public LevelGroundModes LevelGroundMode { get; }

}