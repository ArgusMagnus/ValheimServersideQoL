namespace ServersideQoL.ZDOExtender;

public static class Extensions
{
  extension(ZDO zdo)
  {
    public T GetExtension<T>() where T : class, IExtendedZDO
        => zdo as T ?? throw new InvalidOperationException($"Did you forget to subscribe to {nameof(ZDOExtenderPlugin)}.{nameof(ZDOExtenderPlugin.RegisterInterfaces)} and call {nameof(IZDOInterfaceCollection)}.{nameof(IZDOInterfaceCollection.Add)}<{typeof(T).FullName}>()?");

    public T? GetOptionalExtension<T>() where T : class, IExtendedZDO
        => zdo as T;
  }
}
