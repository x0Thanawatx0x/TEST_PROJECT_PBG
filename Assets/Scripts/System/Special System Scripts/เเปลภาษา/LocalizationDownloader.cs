using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

public class LocalizationDownloader : MonoBehaviour
{
    [Header("Google Sheet Settings")]
    public string googleSheetUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vS68i7Xrrxietvs30lzl3ZtuY4ai4ENopugFszfM9JyQJBNfjMDDvEXacChCfDwTqAKo57VYNp6nqmJ/pub?gid=1690756378&single=true&output=csv";

    [Header("Localization Settings")]
    public string tableName = "PlanBuilderStrings";

    [ContextMenu("Update Localization")]
    public void UpdateLocalization()
    {
        Debug.Log("<color=cyan>PBG Localization:</color> เริ่มต้นดึงข้อมูล...");
        // เรียกใช้ StaticCoroutine ที่อยู่ด้านล่าง
        StaticCoroutine.Start(DownloadCSV());
    }

    IEnumerator DownloadCSV()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(googleSheetUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                ProcessCSV(webRequest.downloadHandler.text);
            }
            else
            {
                Debug.LogError("<color=red>PBG Localization Error:</color> " + webRequest.error);
            }
        }
    }

    void ProcessCSV(string csv)
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
        if (collection == null)
        {
            Debug.LogError($"<color=red>Error:</color> ไม่พบ Table ชื่อ '{tableName}'");
            return;
        }

        string[] lines = csv.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(',');
            if (cols.Length < 3) continue;

            string key = cols[0].Trim();
            string enVal = cols[1].Trim();
            string thVal = cols[2].Trim();

            var sharedEntry = collection.SharedData.GetEntry(key);
            if (sharedEntry == null) collection.SharedData.AddKey(key);

            UpdateTableEntry(collection, "en", key, enVal);
            UpdateTableEntry(collection, "th", key, thVal);
        }

        EditorUtility.SetDirty(collection.SharedData);
        foreach (var table in collection.StringTables) EditorUtility.SetDirty(table);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>✨ PBG Localization Updated Successfully!</color>"); 
    }

    void UpdateTableEntry(StringTableCollection collection, string localeCode, string key, string value)
    {
        var table = collection.GetTable(localeCode) as StringTable;
        if (table != null)
        {
            var entry = table.GetEntry(key) ?? table.AddEntry(key, value);
            entry.Value = value;
        }
    }
} // ปิด Class LocalizationDownloader

// --- Class ตัวช่วยรัน Coroutine ใน Editor ---
public static class StaticCoroutine
{
    private class CoroutineHolder : MonoBehaviour { }
    private static CoroutineHolder _holder;

    public static void Start(IEnumerator routine)
    {
        if (_holder == null)
        {
            _holder = new GameObject("PBG_StaticCoroutine").AddComponent<CoroutineHolder>();
            _holder.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }
        _holder.StartCoroutine(routine);
    }
}