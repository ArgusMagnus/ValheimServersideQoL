using UnityEngine;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

static class ExtensionMethods
{
    public static ExtendedZDO? GetExtendedZDO(this ZDOMan instance, ZDOID id) => (ExtendedZDO?)instance.GetZDO(id);
}