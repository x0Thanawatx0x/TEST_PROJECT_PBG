using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

// ถอด MonoBehaviour ออก เพราะเราจะรันใน Editor เท่านั้น
public static class LocalizationDownloader
{
    private static string googleSheetUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vS68i7Xrrxietvs30lzl3ZtuY4ai4ENopugFszfM9JyQJBNfjMDDvEXacChCfDwTqAKo57VYNp6nqmJ/pub?gid=1690756378&single=true&output=csv";
    private static string tableName = "PGB_Translate"; // ปรับชื่อตารางให้ตรงกับโปรเจกต์

    [MenuItem("PBG Tools/Download & Update Localization")]
    public static void UpdateLocalization()
    {
        Debug.Log("<color=cyan>PBG Localization:</color> เริ่มต้นดึงข้อมูลจาก Google Sheet...");
        // ใช้ EditorCoroutine แทน MonoBehaviour Coroutine
        EditorCoroutineRunner.Start(DownloadCSV());
    }

    static IEnumerator DownloadCSV()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(googleSheetUrl))
        {
            var operation = webRequest.SendWebRequest();

            // รอจนกว่าจะโหลดเสร็จ (วิธีสำหรับ Editor)
            while (!operation.isDone) yield return null;

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

    static void ProcessCSV(string csv)
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
        if (collection == null)
        {
            Debug.LogError($"<color=red>Error:</color> ไม่พบ Table ชื่อ '{tableName}' ในโปรเจกต์!");
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

            var sharedEntry = collection.SharedData.GetEntry(key) ?? collection.SharedData.AddKey(key);

            UpdateTableEntry(collection, "en", key, enVal);
            UpdateTableEntry(collection, "th", key, thVal);
        }

        EditorUtility.SetDirty(collection.SharedData);
        foreach (var table in collection.StringTables) EditorUtility.SetDirty(table);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>✨ PBG: อัปเดตคำแปลสำเร็จแล้ว!</color>");
    }

    static void UpdateTableEntry(StringTableCollection collection, string localeCode, string key, string value)
    {
        var table = collection.GetTable(localeCode) as StringTable;
        if (table != null)
        {
            var entry = table.GetEntry(key) ?? table.AddEntry(key, value);
            entry.Value = value;
        }
    }
}

// ตัวช่วยรัน Coroutine ใน Editor โดยไม่ต้องพึ่ง GameObject
public static class EditorCoroutineRunner
{
    public static void Start(IEnumerator routine)
    {
        EditorApplication.CallbackFunction update = null;
        update = () =>
        {
            if (!routine.MoveNext())
            {
                EditorApplication.update -= update;
            }
        };
        EditorApplication.update += update;
    }
}