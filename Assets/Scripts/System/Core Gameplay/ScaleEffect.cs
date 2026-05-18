using UnityEngine;
using System.Collections;

public class ScaleEffect : MonoBehaviour
{
    public float speed = 5f;

    private Vector3 targetScale;

    private bool isShrinking = false;

    private Coroutine currentRoutine;

    private void Start()
    {
        targetScale = transform.localScale;
    }

    public void StartGrow()
    {
        isShrinking = false;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        transform.localScale = Vector3.zero;

        currentRoutine = StartCoroutine(Grow());
    }

    private IEnumerator Grow()
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * speed
            );

            yield return null;
        }

        transform.localScale = targetScale;
    }

    public void StartShrink()
    {
        if (isShrinking) return;

        isShrinking = true;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(Shrink());
    }

    private IEnumerator Shrink()
    {
        while (Vector3.Distance(transform.localScale, Vector3.zero) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                Vector3.zero,
                Time.deltaTime * speed
            );

            yield return null;
        }

        transform.localScale = Vector3.zero;
    }
}