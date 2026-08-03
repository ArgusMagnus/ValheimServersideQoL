using UnityEngine;

namespace ServersideQoL.PrefabConfigurator;

[Processor("d597c8f7-129e-4c43-901a-20cea6520f14",
  Priority = int.MinValue)] // Run before every other processor to allow the others to overwrite the values set by this processor
public sealed class PrefabProcessor : Processor<PrefabProcessor.PrefabInfo>
{
  public sealed record PrefabInfo : ProcessorPrefabInfo
  {
    // todo: initialize lists beforehand and set IsValid accordingly. Would make Skip/Initialized obsolete.
    //public override bool IsValid => base.IsValid;
    internal bool Initialized { get; set; }
    internal bool Skip { get; set; }
    /// <see cref="ZNetView.LoadFields"/>
    internal IReadOnlyList<(int, int, bool)>? IntValues { get; set; }
    internal IReadOnlyList<(int, float, bool)>? FloatValues { get; set; }
    internal IReadOnlyList<(int, Vector3, bool)>? Vector3Values { get; set; }
    internal IReadOnlyList<(int, string, bool)>? StringValues { get; set; }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!prefabInfo.Initialized)
    {
      if (Config.Instance.Prefabs.IsDefault)
        return ProcessResult.UnregisterProcessor;
      Initialize(zdo, prefabInfo);
    }

    if (prefabInfo.Skip)
      return ProcessResult.UnregisterProcessor;

    zdo.SetComponentHasFields();
    var result = ProcessResult.UnregisterProcessor;
    foreach (var (hash, value, isDefault) in prefabInfo.IntValues ?? [])
    {
      if (isDefault ? zdo.ZDO.RemoveInt(hash) : ZDOExtraData.Set(zdo.ZDO.m_uid, hash, value))
        result |= ProcessResult.RecreateZDO;
    }
    foreach (var (hash, value, isDefault) in prefabInfo.FloatValues ?? [])
    {
      if (isDefault ? zdo.ZDO.RemoveFloat(hash) : ZDOExtraData.Set(zdo.ZDO.m_uid, hash, value))
        result |= ProcessResult.RecreateZDO;
    }
    foreach (var (hash, value, isDefault) in prefabInfo.Vector3Values ?? [])
    {
      if (isDefault ? zdo.ZDO.RemoveVec3(hash) : ZDOExtraData.Set(zdo.ZDO.m_uid, hash, value))
        result |= ProcessResult.RecreateZDO;
    }
    foreach (var (hash, value, isDefault) in prefabInfo.StringValues ?? [])
    {
      if (isDefault ? zdo.ZDO.RemoveString(hash) : ZDOExtraData.Set(zdo.ZDO.m_uid, hash, value))
        result |= ProcessResult.RecreateZDO;
    }

    return result;
  }

  void Initialize(ServersideQoLZDO zdo, PrefabInfo prefabInfo)
  {
    prefabInfo.Initialized = true;

    List<(int, int, bool)>? intValues = null;
    List<(int, float, bool)>? floatValues = null;
    List<(int, Vector3, bool)>? vector3Values = null;
    List<(int, string, bool)>? stringValues = null;

    foreach (var item in Config.Instance.Prefabs.Value.Entries)
    {
      if (!Config.Instance.Prefabs.Value.ValidComponents.TryGetValue(item.Component, out var componentInfo))
      {
        Logger.LogWarning($"Invalid component: {item.Component}");
        continue;
      }

      var (componentType, fields) = componentInfo;

      if (!GetPrefabInfo(zdo).Components.TryGetValue(componentType, out var componentList))
        continue;

      var component = componentList[0];
      componentType = component.GetType(); // Get the actual type of the component in case it is a subclass of the expected type

      var prefabFound = item.PrefabNames is not { Length: > 0 };
      if (!prefabFound)
      {
        foreach (var prefabName in item.PrefabNames)
        {
          var hash = prefabName.Trim().GetStableHashCode();
          if (hash != zdo.ZDO.GetPrefab())
          {
            if (ZNetScene.instance.GetPrefab(hash) is null)
              Logger.LogWarning($"Invalid prefab name '{prefabName}' for component '{item.Component}'");
            continue;
          }
          prefabFound = true;
          break;
        }
      }

      if (!prefabFound)
        continue;

      var hasNonDefaultFields = false;
      foreach (var (fieldName, valueObj) in item.Fields)
      {
        if (!fields.TryGetValue(fieldName, out var fieldInfo))
        {
          Logger.LogWarning($"Invalid field: {item.Component}.{fieldName}");
          continue;
        }

        var success = false;

        if (fieldInfo.FieldType == typeof(int))
        {
          var valueStr = (valueObj as string)?.Trim();
          if (!string.IsNullOrEmpty(valueStr))
          {
            char modifier = default;
            int defaultValue = default;
            if (valueStr[0] is '+' or '-' or '*' or '/')
              modifier = valueStr[0];
            if (success = int.TryParse(valueStr.AsSpan(modifier == default ? 0 : 1), out var value))
            {
              defaultValue = (int)fieldInfo.GetValue(component);
              value = modifier switch
              {
                '+' => defaultValue + value,
                '-' => defaultValue - value,
                '*' => defaultValue * value,
                '/' => defaultValue / value,
                _ => value
              };
              var isDefault = value == defaultValue;
              if (!isDefault)
                hasNonDefaultFields = true;
              (intValues ??= []).Add(($"{componentType.Name}.{fieldName}".GetStableHashCode(), value, isDefault));
            }
          }
        }
        else if (fieldInfo.FieldType == typeof(float))
        {
          var valueStr = (valueObj as string)?.Trim();
          if (!string.IsNullOrEmpty(valueStr))
          {
            char modifier = default;
            float defaultValue = default;
            if (valueStr[0] is '+' or '-' or '*' or '/')
              modifier = valueStr[0];
            if (success = float.TryParse(valueStr.AsSpan(modifier == default ? 0 : 1), out var value))
            {
              defaultValue = (float)fieldInfo.GetValue(component);
              value = modifier switch
              {
                '+' => defaultValue + value,
                '-' => defaultValue - value,
                '*' => defaultValue * value,
                '/' => defaultValue / value,
                _ => value
              };
              var isDefault = value == defaultValue;
              if (!isDefault)
                hasNonDefaultFields = true;
              (floatValues ??= []).Add(($"{componentType.Name}.{fieldName}".GetStableHashCode(), value, isDefault));
            }
          }
        }
        else if (fieldInfo.FieldType == typeof(bool))
        {
          var valueStr = (valueObj as string)?.Trim();
          if (success = bool.TryParse(valueStr, out var value))
          {
            var defaultValue = (bool)fieldInfo.GetValue(component);
            var isDefault = value == defaultValue;
            if (!isDefault)
              hasNonDefaultFields = true;
            (intValues ??= []).Add(($"{componentType.Name}.{fieldName}".GetStableHashCode(), value ? 1 : 0, isDefault));
          }
        }
        else if (fieldInfo.FieldType == typeof(Vector3))
        {
          if (valueObj is Dictionary<string, object> valueDict &&
              valueDict.TryGetValue("x", out var obj) && float.TryParse(obj as string, out var x) &&
              valueDict.TryGetValue("y", out obj) && float.TryParse(obj as string, out var y) &&
              valueDict.TryGetValue("z", out obj) && float.TryParse(obj as string, out var z))
          {
            success = true;
            var defaultValue = (Vector3)fieldInfo.GetValue(component);
            var value = new Vector3(x, y, z);
            var isDefault = value == defaultValue;
            if (!isDefault)
              hasNonDefaultFields = true;
            (vector3Values ??= []).Add(($"{componentType.Name}.{fieldName}".GetStableHashCode(), value, isDefault));
          }
        }
        else if (fieldInfo.FieldType == typeof(string))
        {
          if (valueObj is string value)
          {
            success = true;
            var defaultValue = (string)fieldInfo.GetValue(component);
            var isDefault = value == defaultValue;
            if (!isDefault)
              hasNonDefaultFields = true;
            (stringValues ??= []).Add(($"{componentType.Name}.{fieldName}".GetStableHashCode(), value, isDefault));
          }
        }
        else if (fieldInfo.FieldType == typeof(GameObject)) /// <see cref="ZNetView.LoadFields"/>
        {
          if (valueObj is string value && ZNetScene.instance.GetPrefab(value = value.Trim()) is not null)
          {
            success = true;
            var defaultValue = ((GameObject)fieldInfo.GetValue(component))?.name ?? "";
            var isDefault = value == defaultValue;
            if (!isDefault)
              hasNonDefaultFields = true;
            (stringValues ??= []).Add(($"{componentType.Name}.{fieldName}".GetStableHashCode(), value, isDefault));
          }
        }
        else if (fieldInfo.FieldType == typeof(ItemDrop)) /// <see cref="ZNetView.LoadFields"/>
        {
          if (valueObj is string str && ZNetScene.instance.GetPrefab(str = str.Trim())?.GetComponent<ItemDrop>() is not null)
          {
            success = true;
            var defaultValue = ((ItemDrop)fieldInfo.GetValue(component))?.name ?? "";
            (stringValues ??= []).Add(($"{componentType.Name}.{fieldName}".GetStableHashCode(), str, str == defaultValue));
          }
        }
        else
        {
          Logger.LogWarning($"Unsupported field type: {fieldInfo.FieldType.Name} ({item.Component}.{fieldName})");
          success = false;
        }

        if (!success)
          Logger.LogWarning($"Failed to parse field value as {fieldInfo.FieldType.Name}: {item.Component}.{fieldName} = {valueObj}");
      }

      if (hasNonDefaultFields)
      {
        intValues ??= [];
        intValues.Add(($"{ZNetView.CustomFieldsStr}{componentType.Name}".GetStableHashCode(), 1, false));
      }
    }

    prefabInfo.IntValues = intValues;
    prefabInfo.FloatValues = floatValues;
    prefabInfo.Vector3Values = vector3Values;
    prefabInfo.StringValues = stringValues;
    prefabInfo.Skip = intValues is null && floatValues is null && vector3Values is null && stringValues is null;
  }
}
