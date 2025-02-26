namespace RealityWard.StateMachineSystem {
  public interface IState {
    void OnEnter();
    void Update();
    void FixedUpdate();
    void OnExit();
  }
}