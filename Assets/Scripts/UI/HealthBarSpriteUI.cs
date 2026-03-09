using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarSpriteUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Image image;

    [Header("Sprites (5 states)")]
    [Tooltip("0=пусто, 1=1 деление, 2=2, 3=3, 4=полная")]
    [SerializeField] private Sprite[] states;

    private bool subscribed;

    private void Awake()
    {
        if (image == null) image = GetComponent<Image>();

        if (states != null && states.Length >= 5 && image != null)
            image.sprite = states[0];
    }

    public void SetTarget(Health newHealth)
    {
        Unsubscribe();
        health = newHealth;
        Subscribe();
        UpdateBar();
    }

    private void OnEnable()
    {
        Subscribe();
        UpdateBar();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        if (health == null) return;

        health.OnHealthChanged += Health_OnHealthChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (health != null)
            health.OnHealthChanged -= Health_OnHealthChanged;

        subscribed = false;
    }

    private void Health_OnHealthChanged(object sender, EventArgs e)
    {
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (health == null || image == null || states == null || states.Length < 5) return;

        int max = health.GetMaxHealth();
        int cur = health.GetHealth();

        float t = (max <= 0) ? 0f : Mathf.Clamp01((float)cur / max);

        // 5 состояний: 0..4
        int index = Mathf.FloorToInt(t * 5f); // 0..5
        if (index >= 5) index = 4;
        index = Mathf.Clamp(index, 0, 4);

        image.sprite = states[index];
    }
}
