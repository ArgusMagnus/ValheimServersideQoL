using BepInEx.Configuration;
using Valheim.ServersideQoL.Processors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class AdvancedConfig
    {
        public TamesConfig Tames { get; init; } = new();
        public HostileSummonsConfig HostileSummons { get; init; } = new();
        public ContainerConfig Containers { get; init; } = new();

        public sealed class TamesConfig
        {
            public TeleportFollowPositioningConfig TeleportFollowPositioning { get; init; } = new(2, 4, 0, 1, 45);
            public sealed record TeleportFollowPositioningConfig(
            float MinDistXZ, float MaxDistXZ, float MinOffsetY, float MaxOffsetY, float HalfArcXZ)
            { TeleportFollowPositioningConfig() : this(default, default, default, default, default) { } }

            Dictionary<string, bool> TeleportFollow { get; init; } = new();
            IReadOnlyList<int>? _teleportFollowExcluded;
            [YamlIgnore]
            public IReadOnlyList<int> TeleportFollowExcluded => _teleportFollowExcluded ??= [.. TeleportFollow
                .Where(static x => !x.Value).Select(static x => x.Key.GetStableHashCode())];

            Dictionary<string, bool> TakeIntoDungeon { get; init; } = new();
            IReadOnlyList<int>? _takeIntoDungeonExcluded;
            [YamlIgnore]
            public IReadOnlyList<int> TakeIntoDungeonExcluded => _takeIntoDungeonExcluded ??= [.. TakeIntoDungeon
                .Where(static x => !x.Value).Select(static x => x.Key.GetStableHashCode())];

            public TamesConfig()
            {
                foreach (var prefab in ZNetScene.instance.m_prefabs)
                {
                    if (prefab.GetComponent<Tameable>() is not { } || prefab.GetComponent<BaseAI>() is not { } /*baseAI*/)
                        continue;

                    TeleportFollow.Add(prefab.name, true);
                    TakeIntoDungeon.Add(prefab.name, true);
                    //TakeIntoDungeon.Add(prefab.name, baseAI.m_pathAgentType is not Pathfinding.AgentType.TrollSize);
                }
            }
        }

        public sealed class HostileSummonsConfig
        {
            public sealed record FollowSummonerConfig(float MoveInterval, float MaxDistance) { FollowSummonerConfig() : this(default, default) { } }

            public FollowSummonerConfig FollowSummoners { get; init; } = new(4, 20);
        }

        public sealed class ContainerConfig
        {
            public sealed record ChestSignOffset(float Left, float Right, float Front, float Back, float Top) { ChestSignOffset() : this(float.NaN, float.NaN, float.NaN, float.NaN, float.NaN) { } }

            [YamlMember(Alias = nameof(ChestSignOffsets))]
            Dictionary<string, ChestSignOffset> ChestSignOffsetsYaml { get; init; } = new()
            {
                [Processor.PrefabNames.WoodChest] = new(0.8f, 0.8f, 0.4f, 0.4f, 0.8f),
                [Processor.PrefabNames.ReinforcedChest] = new(0.85f, 0.85f, 0.5f, 0.5f, 1.1f),
                [Processor.PrefabNames.BlackmetalChest] = new(0.95f, 0.95f, 0.7f, 0.7f, 0.95f),
                [Processor.PrefabNames.Barrel] = new(0.4f, 0.4f, 0.4f, 0.4f, 0.9f),
                [Processor.PrefabNames.Incinerator] = new(float.NaN, float.NaN, 0.1f, float.NaN, 3f)
            };

            IReadOnlyDictionary<int, ChestSignOffset>? _chestSignOffsets;

            [YamlIgnore]
            public IReadOnlyDictionary<int, ChestSignOffset> ChestSignOffsets => _chestSignOffsets ??= ChestSignOffsetsYaml.ToDictionary(static x => x.Key.GetStableHashCode(), static x => x.Value);
        }
    }

    static AdvancedConfig InitializeAdvancedConfig(ConfigFile cfg)
    {
        var configDir = Path.Combine(Path.GetDirectoryName(cfg.ConfigFilePath), Path.GetFileNameWithoutExtension(cfg.ConfigFilePath));
        var configPath = Path.Combine(configDir, "Advanced.yml");

        var result = new AdvancedConfig();

        var serializer = new SerializerBuilder()
            .IncludeNonPublicProperties()
            .WithTypeInspector(static x => new MyTypeInspector(x))
            .Build();

        {
            Directory.CreateDirectory(configDir);
            var defaultConfigPath = Path.ChangeExtension(configPath, "default.yml");
            using var file = new StreamWriter(defaultConfigPath, append: false);
            file.WriteLine($"# {Path.GetFileName(defaultConfigPath)} contains the default values and is overwritten regularly.");
            file.WriteLine($"# Rename it to {Path.GetFileName(configPath)} if you want to change values.");
            file.WriteLine();
            WriteYamlHeader(file);
            serializer.Serialize(file, result);
        }

        if (File.Exists(configPath))
        {
            try
            {
                using var stream = new StreamReader(configPath);
                result = new DeserializerBuilder()
                    .IncludeNonPublicProperties()
                    .EnablePrivateConstructors()
                    //.WithObjectFactory(new MyObjectFactory())
                    .WithTypeInspector(static x => new MyTypeInspector(x))
                    .Build().Deserialize<AdvancedConfig>(stream);
                Main.Instance.Logger.LogInfo($"Advanced config loaded from {Path.GetFileName(configPath)}:{Environment.NewLine}{serializer.Serialize(result)}");
            }
            catch (Exception ex)
            {
                Main.Instance.Logger.LogWarning($"{Path.GetFileName(configPath)}: {ex}");
            }
        }

        return result;
    }

    static void WriteYamlHeader(StreamWriter writer)
    {
        writer.WriteLine($"# IMPORTANT:");
        writer.WriteLine($"#   This file is for advanced tweaks. You are expected to be familiar with YAML and its pitfalls if you decide to edit it.");
        writer.WriteLine($"#   Check the log for warnings related to this file and DO NOT open issues asking for help on how to format this file.");
        writer.WriteLine();
    }

    sealed class MyTypeInspector(ITypeInspector inner) : TypeInspectorSkeleton
    {
        readonly ITypeInspector _inner = inner;

        public override string GetEnumName(Type enumType, string name) => _inner.GetEnumName(enumType, name);
        public override string GetEnumValue(object enumValue) => _inner.GetEnumValue(enumValue);

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
        {
            foreach (var prop in _inner.GetProperties(type, container))
            {
                if (prop.Type == typeof(Type) && prop.Name is "EqualityContract")
                    continue;
                yield return prop;
            }
        }
    }
}