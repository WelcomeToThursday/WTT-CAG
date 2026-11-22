using EFT;
using UnityEngine;

namespace WTT_CAG.Client.Content.Thermals.Bodies;

// Cools a dead player's skin towards the surroundings, so bodies slowly fade on thermal. The game
// otherwise leaves them at the last living temperature. Newton's law of cooling on skin surface
// temperature; skin cools far faster than the core does.
public class CorpseCooling : MonoBehaviour
{
    private const float AmbientCelsius = 15f;

    // Time for the gap to ambient to shrink by about two thirds.
    private const float TimeConstantSeconds = 3600f;

    private const float UpdateIntervalSeconds = 5f;

    private Player? _player;
    private float _startCelsius;
    private float _deathTime;
    private float _nextUpdate;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _startCelsius = _player ? _player!.HealthController.Temperature.Current : 36.6f;
        _deathTime = Time.time;
        _nextUpdate = Time.time + UpdateIntervalSeconds;
    }

    private void Update()
    {
        if (Time.time < _nextUpdate) { return; }

        _nextUpdate = Time.time + UpdateIntervalSeconds;

        if (!_player || _player!.PlayerBody == null)
        {
            Destroy(this);
            return;
        }

        var elapsed = Time.time - _deathTime;
        var celsius = AmbientCelsius + (_startCelsius - AmbientCelsius) * Mathf.Exp(-elapsed / TimeConstantSeconds);

        _player.PlayerBody.SetTemperatureForBody(celsius);
    }
}
