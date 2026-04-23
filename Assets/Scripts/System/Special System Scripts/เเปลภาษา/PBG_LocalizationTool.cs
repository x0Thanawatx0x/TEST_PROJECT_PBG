using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Events; // 🔥 เพิ่ม
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using System.Text.RegularExpressions;

public class PBG_LocalizationTool : EditorWindow
{
    [MenuItem("PBG Tools/Match From Partial Key")]
    public static void MatchFromPartialKey()
    {
        string tableName = "PGB_Translate";

        var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);

        if (collection == null)
        {
            Debug.LogError("❌ ไม่เจอ Table");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;

        int matchCount = 0;
        int failCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) continue;

            string keyword = Normalize(tmp.text); // เช่น START

            LocalizeStringEvent localizer = obj.GetComponent<LocalizeStringEvent>();
            if (localizer == null)
                localizer = obj.AddComponent<LocalizeStringEvent>();

            SetupUnityEvent(localizer, tmp);

            string bestMatchKey = null;

            foreach (var entry in collection.SharedData.Entries)
            {
                string key = Normalize(entry.Key);

                // 🔥 match แบบ contains
                if (key.Contains(keyword))
                {
                    bestMatchKey = entry.Key;
                    break; // เอาอันแรกที่เจอ
                }
            }

            if (bestMatchKey != null)
            {
                localizer.StringReference.TableReference = tableName;
                localizer.StringReference.TableEntryReference = bestMatchKey;

                Debug.Log($"✅ MATCH: {tmp.text} -> {bestMatchKey}");

                matchCount++;
            }
            else
            {
                Debug.LogWarning($"❌ ไม่เจอ Key ที่มีคำว่า: {tmp.text}");
                failCount++;
            }

            EditorUtility.SetDirty(obj);
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"🎯 เสร็จ | Match: {matchCount} | ไม่เจอ: {failCount}");
    }

    // =========================
    static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        string noRichText = Regex.Replace(input, "<.*?>", "");

        return noRichText
            .Replace("\n", "")
            .Replace("\r", "")
            .Replace(" ", "")
            .Trim()
            .ToLower();
    }

    // =========================
    static void SetupUnityEvent(LocalizeStringEvent localizer, TextMeshProUGUI tmp)
    {
        SerializedObject so = new SerializedObject(localizer);
        SerializedProperty onUpdate = so.FindProperty("m_OnUpdateString");

        if (onUpdate != null)
        {
            SerializedProperty calls = onUpdate.FindPropertyRelative("m_PersistentCalls.m_Calls");

            calls.ClearArray();
            calls.InsertArrayElementAtIndex(0);

            var call = calls.GetArrayElementAtIndex(0);

            call.FindPropertyRelative("m_Target").objectReferenceValue = tmp;
            call.FindPropertyRelative("m_MethodName").stringValue = "set_text";
            call.FindPropertyRelative("m_Mode").enumValueIndex = 3;
            call.FindPropertyRelative("m_CallState").enumValueIndex = 2;

            so.ApplyModifiedProperties();
        }

        // 🔥 เพิ่มส่วนนี้ (ทำให้เป็น Dynamic String จริง)
        localizer.OnUpdateString.RemoveAllListeners();

        UnityEventTools.AddPersistentListener(
            localizer.OnUpdateString,
            tmp.SetText
        );

        EditorUtility.SetDirty(localizer);
    }
}