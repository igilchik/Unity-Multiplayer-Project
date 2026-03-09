using System.Collections;
using UnityEngine;

public class FlashBlink : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private int flashCount = 3;

    [Header("Auto bind")]
    [SerializeField] private Health health;

    private Color originalColor;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        originalColor = spriteRenderer.color;

        if (health == null)
            health = GetComponentInParent<Health>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged += Health_OnDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= Health_OnDamaged;
    }

    private void Health_OnDamaged(object sender, Health.OnDamageEventArgs e)
    {
        PlayFlash();
    }

    public void PlayFlash()
    {
        if (spriteRenderer == null) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        flashRoutine = null;
    }
}
