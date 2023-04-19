using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using MiniJSON;

namespace LoLExt {
    public class LoLLanguageJSONValueExtractor : EditorWindow {
        private TextAsset lookupTextAsset;
        private TextAsset sourceTextAsset;
        private TextAsset outputTextAsset;

        [MenuItem("Tools/LoL Language JSON Value Extractor")]
        static void Execute() {
            EditorWindow.GetWindow(typeof(LoLLanguageJSONValueExtractor));
        }

        void OnGUI() {
            lookupTextAsset = EditorGUILayout.ObjectField("Original Text", lookupTextAsset, typeof(TextAsset), false) as TextAsset;
            sourceTextAsset = EditorGUILayout.ObjectField("Modified Text", sourceTextAsset, typeof(TextAsset), false) as TextAsset;
            outputTextAsset = EditorGUILayout.ObjectField("Output Text", outputTextAsset, typeof(TextAsset), false) as TextAsset;

            GUI.enabled = lookupTextAsset && sourceTextAsset && outputTextAsset;

            if(GUILayout.Button("Execute")) {
                var lookUpString = lookupTextAsset.text;
                var sourceString = sourceTextAsset.text;

                var lookUpDefs = Json.Deserialize(lookUpString) as Dictionary<string, object>;
                var sourceDefs = Json.Deserialize(sourceString) as Dictionary<string, object>;

                object enDefsObj;
                if(lookUpDefs.TryGetValue("en", out enDefsObj)) {
                    var outputDict = new Dictionary<string, System.Text.StringBuilder>();

                    var enDefs = enDefsObj as Dictionary<string, object>;
                    foreach(var lookUpPair in enDefs) {
                        foreach(var sourcePair in sourceDefs) {
                            System.Text.StringBuilder sb;
                            if(outputDict.ContainsKey(sourcePair.Key))
                                sb = outputDict[sourcePair.Key];
                            else {
                                sb = new System.Text.StringBuilder();
                                outputDict[sourcePair.Key] = sb;
                            }

                            var sourceSubDefs = sourcePair.Value as Dictionary<string, object>;
                            if(sourceSubDefs.ContainsKey(lookUpPair.Key)) {
                                var str = sourceSubDefs[lookUpPair.Key] as string;
                                foreach(char c in str) {
                                    if(c == '\n')
                                        sb.Append("\\n");
                                    else if(c == '\t')
                                        sb.Append("\\t");
                                    else if(c == '\b')
                                        sb.Append("\\b");
                                    else if(c == '\r')
                                        continue;
                                    else
                                        sb.Append(c);
                                }

                                sb.AppendLine();
                            }
                        }
                    }

                    var outputSb = new System.Text.StringBuilder();
                    foreach(var outputPair in outputDict) {
                        outputSb.AppendLine(outputPair.Key);
                        outputSb.AppendLine();

                        outputSb.Append(outputPair.Value);

                        outputSb.AppendLine();
                        outputSb.AppendLine();
                    }

                    var outputPath = AssetDatabase.GetAssetPath(outputTextAsset);
                    System.IO.File.WriteAllText(outputPath, outputSb.ToString());
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                else
                    Debug.LogWarning("Unable to find Key 'en' in: " + lookUpString);
            }
        }
    }
}