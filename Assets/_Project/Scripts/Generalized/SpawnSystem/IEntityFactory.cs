using UnityEngine;

namespace RealityWard.SpawnSystem {
  public interface IEntityFactory<T> where T : Entity {
    T Create(Transform spawnPoint);
  }
}