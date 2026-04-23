using System.Collections;
using UnityEngine;

public class ArmorAmuletPickup : MonoBehaviour, IPickupEffect
{
    [SerializeField] private float shieldDuration = 5f;
    [SerializeField] private float auraYOffset = -0.3f;
    [SerializeField] private GameObject auraPrefab;

    public void OnPickup(GameObject player)
    {
        if (player.GetComponent<ArmorAmuletBuff>() != null) return;
        var buff = player.AddComponent<ArmorAmuletBuff>();
        buff.Init(shieldDuration, auraYOffset, auraPrefab);
    }
}

public class ArmorAmuletBuff : MonoBehaviour
{
    private float _duration;
    private float _auraYOffset;
    private GameObject _auraPrefab;

    private PlayerHealth _health;
    private GameObject _auraInstance;
    private Coroutine _shieldRoutine;

    public void Init(float duration, float auraYOffset, GameObject auraPrefab)
    {
        _duration = duration;
        _auraYOffset = auraYOffset;
        _auraPrefab = auraPrefab;
    }

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        _health.OnDamageTaken += OnDamageTaken;
        _health.OnShieldBroken += DeactivateShield;
    }

    private void Start()
    {
        if (_auraPrefab != null)
        {
            _auraInstance = Instantiate(_auraPrefab, transform);
            _auraInstance.transform.localPosition = new Vector3(0f, _auraYOffset, 0f);
            _auraInstance.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_health == null) return;
        _health.OnDamageTaken -= OnDamageTaken;
        _health.OnShieldBroken -= DeactivateShield;
    }

    private void OnDamageTaken()
    {
        if (_shieldRoutine != null) StopCoroutine(_shieldRoutine);
        _shieldRoutine = StartCoroutine(ShieldRoutine());
    }

    private IEnumerator ShieldRoutine()
    {
        _health.GrantShield();
        if (_auraInstance != null) _auraInstance.SetActive(true);

        yield return new WaitForSeconds(_duration);

        _health.RemoveShield();
        if (_auraInstance != null) _auraInstance.SetActive(false);
    }

    private void DeactivateShield()
    {
        if (_shieldRoutine != null) { StopCoroutine(_shieldRoutine); _shieldRoutine = null; }
        if (_auraInstance != null) _auraInstance.SetActive(false);
    }
}
