using Mono.Cecil.Cil;
using MonoMod.Cil;
using ServersideQoL.Utilities;
using System.Reflection;
using UnityEngine;

namespace ServersideQoL.WorldOptions;

[Processor(Id)]
public sealed class PieceProcessor : Processor<PieceProcessor.PrefabInfo>
{
  public const string Id = "cd5a0edc-370d-4093-9c79-c9cd6d0853ad";
  public sealed record PrefabInfo(Piece Piece, PieceTable PieceTable, ZNetView ZNetView) : ProcessorPrefabInfo
  {
    public override bool IsValid => Piece.m_resources.Any(static x => x is { m_resItem: not null, m_recover: false })
      && !PrefabInfo.HasComponent<ItemDrop>() && !PrefabInfo.HasComponent<Feast>();
  }

  Action<Piece, HitData?, ZDO>? _dropResources;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!Config.Instance.BuildPiecesReturnAllResourcesWhenDestroyed.Value)
      return ProcessResult.UnregisterProcessor;

    var creator = zdo.Vars.GetCreator();
    if (creator.Value is 0 || creator.IsModPlayerID())
      return ProcessResult.UnregisterProcessor;

    zdo.Destroyed += OnPieceDestroyed;
    zdo.Recreated += (zdo, _) => zdo.Destroyed -= OnPieceDestroyed;
    return ProcessResult.UnregisterProcessor;
  }

  void OnPieceDestroyed(ServersideQoLZDO zdo)
  {
    if (GetProcessorPrefabInfo(zdo) is not { } prefabInfo)
      return;

    if (_dropResources is null)
    {
      var copyBuilder = new MethodCopyBuilder(prefabInfo.Piece.DropResources, "DropNonRecoverableResources");
      copyBuilder.Transpile(static (copyBuilder, instructions) =>
      {
        var zdoParameter = copyBuilder.AddParameter<ZDO>("zdo");
        var recoverField = typeof(Piece.Requirement).GetField(nameof(Piece.Requirement.m_recover), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var isPlacedByPlayerMethod = typeof(Piece).GetMethod(nameof(Piece.IsPlacedByPlayer), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var nviewField = typeof(Piece).GetField("m_nview", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var getZDOMethod = typeof(ZNetView).GetMethod(nameof(ZNetView.GetZDO), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var getTransformMethod = typeof(Component).GetProperty(nameof(Component.transform)).GetMethod;
        var getTransformPositionMethod = typeof(Transform).GetProperty(nameof(Transform.position)).GetMethod;
        var getZDOPositionMethod = typeof(ZDO).GetMethod(nameof(ZDO.GetPosition));
        
        //var ilProcessor = copyBuilder.GetILProcessor();

        for (int i = 2; i < instructions.Count; i++)
        {
          if (instructions[i - 1].MatchLdfld(recoverField) &&
              instructions[i].OpCode == OpCodes.Brfalse)
          {
            instructions[i].OpCode = OpCodes.Brtrue;
          }
          else if (instructions[i - 1].MatchLdarg(0) &&
                   instructions[i].MatchCall(isPlacedByPlayerMethod))
          {
            MakeNoOp(instructions[i - 1]);
            instructions[i] = Instruction.Create(OpCodes.Ldc_I4_1);
          }
          else if (instructions[i - 2].MatchLdarg(0) &&
                   instructions[i - 1].MatchLdfld(nviewField) &&
                   instructions[i].MatchCallOrCallvirt(getZDOMethod))
          {
            MakeNoOp(instructions[i - 2]);
            MakeNoOp(instructions[i - 1]);
            instructions[i] = Instruction.Create(OpCodes.Ldarg, zdoParameter);
          }
          else if (instructions[i - 2].MatchLdarg(0) &&
                   instructions[i - 1].MatchCall(getTransformMethod) &&
                   instructions[i].MatchCallOrCallvirt(getTransformPositionMethod))
          {
            MakeNoOp(instructions[i - 2]);
            instructions[i - 1] = Instruction.Create(OpCodes.Ldarg, zdoParameter);
            instructions[i] = Instruction.Create(OpCodes.Callvirt, copyBuilder.Module.ImportReference(getZDOPositionMethod));
          }
        }

        static void MakeNoOp(Instruction instruction)
        {
          instruction.OpCode = OpCodes.Nop;
          instruction.Operand = null;
        }
      });
      _dropResources = copyBuilder.Build<Action<Piece, HitData?, ZDO>>();
    }

    _dropResources(prefabInfo.Piece, null, zdo.ZDO);
  }
}
