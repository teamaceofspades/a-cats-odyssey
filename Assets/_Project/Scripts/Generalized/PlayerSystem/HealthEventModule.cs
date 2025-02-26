using RealityWard.EventSystem;
using System;
using UnityEngine;

namespace RealityWard.PlayerController {
  public class HealthEventModule : MonoBehaviour {
    [SerializeField] int _maxHealth = 100;
    [SerializeField] FloatEventChannel _playerHealthChannel;

    int _currentHealth;

    public bool IsDead => _currentHealth <= 0;

    private void Awake() {
      _currentHealth = _maxHealth;
    }

    private void Start() {
      PublishHealthPercentage();
    }

    public void TakeDamage(int damage) {
      _currentHealth -= damage;
      PublishHealthPercentage();
    }

    void PublishHealthPercentage() {
      if (_playerHealthChannel != null) {
        _playerHealthChannel.Invoke(_currentHealth / (float)_maxHealth);
      }
    }
  }
}