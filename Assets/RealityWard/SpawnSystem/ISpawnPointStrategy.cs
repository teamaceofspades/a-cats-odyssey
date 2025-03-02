using NUnit.Framework;
using UnityEngine;

namespace RealityWard.SpawnSystem {
  public interface ISpawnPointStrategy {
    Transform NextSpawnPoint();
  }
}