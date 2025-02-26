namespace RealityWard.StateMachineSystem {
  public interface ITransition {
    IState To { get; }
    IPredicate Condition { get; }
  }
}