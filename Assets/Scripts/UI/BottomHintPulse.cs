using System.Collections;
using TMPro;
using UnityEngine;

public class BottomHintPulse : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float showSeconds = 5f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float minAlpha = 0.25f;
    [SerializeField] private float maxAlpha = 1f;

    private Coroutine routine;
    private bool hasShownOnce;

    private void Awake()
    {
        if (text == null) text = GetComponent<TMP_Text>();

        var c = text.color;
        c.a = 0f;
        text.color = c;
    }

    public void ShowHintOnce(string message)
    {
        if (hasShownOnce) return;
        hasShownOnce = true;
        ShowForSeconds(message, showSeconds);
    }

    public void ShowForSeconds(string message, float seconds)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(message, seconds));
    }

    private IEnumerator ShowRoutine(string message, float seconds)
    {
        text.text = message;

        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;

            float s = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float a = Mathf.Lerp(minAlpha, maxAlpha, s);

            var c = text.color;
            c.a = a;
            text.color = c;

            yield return null;
        }

        var c2 = text.color;
        c2.a = 0f;
        text.color = c2;

        routine = null;
    }
}
