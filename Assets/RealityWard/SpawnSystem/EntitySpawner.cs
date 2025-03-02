namespace RealityWard.SpawnSystem {
  public class EntitySpawner<T> where T : Entity {
    IEntityFactory<T> _entityFactory;
    ISpawnPointStrategy _spawnPointStrategy;

    public EntitySpawner(IEntityFactory<T> entityFactory, ISpawnPointStrategy spawnPointStrategy) {
      _entityFactory = entityFactory;
      _spawnPointStrategy = spawnPointStrategy;
    }

    public T Spawn() {
      return _entityFactory.Create(_spawnPointStrategy.NextSpawnPoint());
    }
  }
}