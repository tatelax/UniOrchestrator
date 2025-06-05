using System;
using UnityEngine;

[Serializable]
public class TypeReference
{
  [SerializeField] private string _typeName;
  [SerializeField] private bool _enabled = true;

  public Type Type => string.IsNullOrEmpty(_typeName) ? null : Type.GetType(_typeName);
  public string Name => _typeName;
  public bool Enabled
  {
    get => _enabled;
    set => _enabled = value;
  }

  public void SetType(Type type)
  {
    _typeName = type?.AssemblyQualifiedName;
  }

  public override string ToString() => Type?.Name ?? "<None>";
}