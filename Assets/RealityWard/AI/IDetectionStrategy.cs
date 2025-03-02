using UnityEngine;
using RealityWard.EventSystem;

namespace RealityWard.AI {
  public interface IDetectionStrategy {
    bool Execute(Transform player, Transform detector, CountdownTimer timer);
  }
}