using UnityEngine;
using System.Collections;
using UnityEngine.Localization.Settings;

public class LocaleSelector : MonoBehaviour
{
    private bool active = false;

    // ต้องเป็น public void และรับ int ตัวเดียวแบบนี้เป๊ะๆ นะปิ๊บ
    public void ChangeLocale(int localeID)
    {
        if (active) return;
        Debug.Log("กำลังเปลี่ยนเป็นภาษา ID: " + localeID);
        StartCoroutine(SetLocale(localeID));
    }

    IEnumerator SetLocale(int _localeID)
    {
        active = true;
        yield return LocalizationSettings.InitializationOperation;

        // เช็คก่อนว่า ID ที่ส่งมามันไม่เกินจำนวนภาษาที่มี
        if (_localeID < LocalizationSettings.AvailableLocales.Locales.Count)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[_localeID];
            PlayerPrefs.SetInt("LocaleKey", _localeID);
            Debug.Log("เปลี่ยนภาษาสำเร็จ!");
        }
        else
        {
            Debug.LogError("ID ภาษาเกินจำนวนที่มีครับปิ๊บ!");
        }

        active = false;
    }
}