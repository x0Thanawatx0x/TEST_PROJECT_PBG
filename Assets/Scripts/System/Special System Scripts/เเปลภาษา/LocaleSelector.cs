using UnityEngine;
using System.Collections;
using UnityEngine.Localization.Settings;

public class LocaleSelector : MonoBehaviour
{
    private bool active = false;

    // ฟังก์ชันสำหรับเปลี่ยนภาษาด้วย ID (0 = English, 1 = Thai ตามลำดับใน Settings)
    public void ChangeLocale(int localeID)
    {
        if (active) return;
        StartCoroutine(SetLocale(localeID));
    }

    IEnumerator SetLocale(int _localeID)
    {
        active = true;
        // รอจนกว่าระบบ Localization จะพร้อม
        yield return LocalizationSettings.InitializationOperation;

        // เปลี่ยนภาษาไปยังตัวที่เลือก
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[_localeID];

        active = false;
        Debug.Log("เปลี่ยนภาษาสำเร็จ!");
    }
}