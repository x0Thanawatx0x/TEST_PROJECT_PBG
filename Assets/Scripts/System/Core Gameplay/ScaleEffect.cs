using UnityEngine;
using System.Collections;

public class ScaleEffect : MonoBehaviour
{
    public float speed = 5f;

    private Vector3 targetScale;
    private bool isGrowing = false;
    private bool isShrinking = false;

    void Start()
    {
        targetScale = transform.localScale;
    }

    // ✅ เรียกตอน Spawn
    public void StartGrow()
    {
        transform.localScale = Vector3.zero;
        isGrowing = true;
        StartCoroutine(Grow());
    }

    IEnumerator Grow()
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
            yield return null;
        }

        transform.localScale = targetScale;
        isGrowing = false;
    }

    // ✅ เรียกตอนลบ
    public void StartShrink()
    {
        if (isShrinking) return;

        isShrinking = true;
        StartCoroutine(Shrink());
    }

    IEnumerator Shrink()
    {
        Vector3 startScale = transform.localScale;

        while (Vector3.Distance(transform.localScale, Vector3.zero) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * speed);
            yield return null;
        }

        Destroy(gameObject);
    }
}