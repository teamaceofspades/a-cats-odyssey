using UnityEngine;

namespace RealityWard.SpawnSystem {
  public abstract class EntitySpawnManager : MonoBehaviour {
    [SerializeField]
    protected SpawnPointStrategyType
      _spawnPointStrategyType = SpawnPointStrategyType.Linear;
    [SerializeField] protected Transform[] _spawnPoints;

    protected ISpawnPointStrategy _spawnPointStrategy;

    protected enum SpawnPointStrategyType {
      Linear,
      Random
    }

    protected virtual void Awake() {
      _spawnPointStrategy = _spawnPointStrategyType switch {
        SpawnPointStrategyType.Linear => new LinearSpawnPointStrategy(_spawnPoints),
        SpawnPointStrategyType.Random => new RandomSpawnPointStrategy(_spawnPoints),
        _ => _spawnPointStrategy
      };
    }

    public abstract void Spawn();
  }
}