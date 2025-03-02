using System;

namespace RealityWard.StateMachineSystem {
  public class Transition : ITransition {
    public IState To { get; }
    public IPredicate Condition { get; }

    public Transition(IState to, IPredicate condition) {
      To = to;
      Condition = condition;
    }

    public Transition(IState to, Func<bool> condition) {
      FuncPredicate predicate = new(condition);
      To = to;
      Condition = predicate;
    }
  }
}