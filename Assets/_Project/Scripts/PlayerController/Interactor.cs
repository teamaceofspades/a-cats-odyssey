using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace PlayerController {
  internal interface IInteractable {
    /// <summary>
    /// Gets the player facing context of the Interact Action.
    /// For example: "Open Door", "Pick Up Collar"
    /// </summary>
    /// <returns>Interact Context</returns>
    String GetInteractText();
    /// <summary>
    /// Method to allow the player the ability to interact with the object.
    /// </summary>
    bool Interact();
  }

  public class Interactor : MonoBehaviour {
    [SerializeField] private Transform _center;
    [SerializeField] private float _radius;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private bool _isInDebugMode = true;
    private readonly Collider[] _colliders = new Collider[4];
    private int _numFound = 0;

    // Update is called once per frame
    private void Update() {
      _numFound = Physics.OverlapSphereNonAlloc(_center.position, _radius, _colliders, _layerMask);
    }

#nullable enable
    public void OnInteract() {
      IInteractable? interactable = CheckForInteract();
      if (interactable != null) {
        interactable.Interact();
      }
    }

    private IInteractable? CheckForInteract() {
      if (_numFound > 0) {
        int indexOfClosest = -1;
        float closestDistance = float.MaxValue;
        float currentDistance;
        for (int i = 0; i < _numFound; i++) {
          if (_colliders[i].GetComponentInParent<IInteractable>(false) is IInteractable) {
            currentDistance = Vector3.Distance(_colliders[i].ClosestPointOnBounds(_center.position), _center.position);
            if (currentDistance < closestDistance) {
              closestDistance = currentDistance;
              indexOfClosest = i;
            }
          }
        }
        if (indexOfClosest >= 0) {
          return _colliders[indexOfClosest]
            .GetComponentInParent<IInteractable>(false);
        }
      }
      return null;
    }

    /// <summary>
    /// Updates InteractTextMeshPro.text to reflect what
    /// the player can activate using Interact button.
    /// </summary>
    public void UpdateInteractTextUI(TextMeshProUGUI InteractTextMeshPro) {
      IInteractable? interactObj = CheckForInteract();
      if (interactObj != null) {
        InteractTextMeshPro.text = interactObj.GetInteractText();
      }
      else {
        InteractTextMeshPro.text = "";
      }
    }
#nullable disable

    private void OnDrawGizmos() {
      if (_isInDebugMode) {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_center.position, _radius);
      }
    }
  }
}