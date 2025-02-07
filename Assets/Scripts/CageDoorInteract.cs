using System;
using UnityEngine;

public class CageDoorInteract : MonoBehaviour, IInteractable
{
  public Animator CageDoorAnimator;
  private readonly String _Name = "Cage Door";

  public bool Interact()
  {
    CageDoorAnimator.SetBool("opened", !CageDoorAnimator.GetBool("opened"));
    return true;
  }

  public String GetInteractText()
  {
    if (CageDoorAnimator.GetBool("opened"))
    {
      return "Close " + _Name;
    }
    return "Open " + _Name;
  }
}
