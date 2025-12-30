namespace Valheim.ServersideQoL.Signs;

sealed record PrefabInfo(Sign Sign) : PrefabInfoBase;

sealed class SignProcessor : Processor<PrefabInfo>
{
    protected override void Process(Inputs inputs, Outputs outputs)
    {
    }
}
