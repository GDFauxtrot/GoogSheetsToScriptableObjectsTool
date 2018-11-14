using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Uri = System.Uri;

public class CardDataImporterWindow : EditorWindow {

    static string url = "";
    static string cardDataDestination = "Assets/Cards";
    static bool ignoreFirstRow = false, ignoreFirstColumn = false;

    #region MenuItems

    [MenuItem("Fauxtrot/Convert CSV to CardData...")]
    static void Convert_CSVToCardData() {
        GetWindow<CardDataImporterWindow>(true, "CSV to CardData", true);
    }

    // TODO support other formats? JSON? XML?

    #endregion

    #region GUI/Panels

    void OnGUI() {
        DrawCSVPanel();
    }

    void DrawCSVPanel() {
        EditorGUILayout.LabelField("Enter URL of Google Sheets sheet:");
        EditorGUILayout.LabelField("Note: CSV must be made visible to anyone with the link! (File -> Share... -> Advanced)");
        url = EditorGUILayout.TextField(url);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Output folder for CardObjects (creates one if it doesn't exist)");
        cardDataDestination = EditorGUILayout.TextField(cardDataDestination);

        ignoreFirstRow = EditorGUILayout.Toggle("Ignore first row?", ignoreFirstRow);
        ignoreFirstColumn = EditorGUILayout.Toggle("Ignore first column?", ignoreFirstColumn);

        bool seeminglyValidURL = url.StartsWith("https://docs.google.com/spreadsheets/d/");

        GUI.enabled = seeminglyValidURL;
        if (GUILayout.Button("Convert URL")) {
            if (cardDataDestination.EndsWith("/"))
                cardDataDestination = cardDataDestination.Remove(cardDataDestination.Length-1);

            ConvertURLToCardData();
        }
        GUI.enabled = true;
    }

    #endregion

    #region CardData Conversions

    void ConvertURLToCardData() {
        // https://stackoverflow.com/q/33713084 to capture GoogSheet as CSV download + discussion
        // This downloads the first sheet only

        // Find document ID
        string[] segments = new Uri(url).Segments;
        string docID = "";
        for (int i = 0; i < segments.Length; ++i) {
            if (segments[i].Equals("d/")) {
                docID = segments[i+1].Remove(segments[i+1].Length-1);
                break;
            }
        }
        if (docID.Equals("")) {
            Debug.LogError("URL's doc ID could not be determined! CSV not parsed.");
            return;
        }

        Uri uri = new Uri(string.Format("https://docs.google.com/spreadsheets/d/{0}/export?format=csv", docID));
        WebRequest request = FileWebRequest.Create(uri);

        using (WebResponse response = request.GetResponse()) {
            // Confirmation that this response is valid -- is it a CSV?
            string contentType = response.Headers["Content-Type"];
            if (!contentType.Equals("text/csv")) {
                Debug.LogError(string.Format("URL response was not a CSV file ({0})! CSV not parsed.", contentType));
                return;
            }

            Stream responseStream = response.GetResponseStream();
            using (StreamReader reader = new StreamReader(responseStream)) {
                string line;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null) {
                    /// ! CUSTOMIZE THIS SECTION TO PARSE THE CSV DIFFERENTLY ! ///

                    // CSV header -- don't parse (comment to remove)
                    if (ignoreFirstRow && lineNumber == 0) {
                        ++lineNumber;
                        continue;
                    }

                    // line parsing here, one row at a time
                    CardData card = ScriptableObject.CreateInstance<CardData>();
                    string[] parsedLine = SplitCsvLine(line);

                    for (int i = 0; i < parsedLine.Length; ++i) {
                        if (ignoreFirstColumn && i == 0)
                            continue;
                        string lineItem = parsedLine[i];
                        switch (i) {
                            case 0:
                                card.cardName = lineItem;
                                break;
                            case 1:
                                card.level = int.Parse(lineItem);
                                break;
                            case 2:
                                card.attack = int.Parse(lineItem);
                                break;
                            case 3:
                                card.defense = int.Parse(lineItem);
                                break;
                        }
                    }

                    // Create CardItem as an asset

                    // Create folder(s) up to the destination folder
                    // (Why doesn't Unity do this automatically during CreateFolder? Hmm)
                    string[] folderPath = cardDataDestination.Split('/');
                    string startFolder = "", nextFolder = "";
                    for (int i = 0; i < folderPath.Length; ++i) {
                        if (i == 0) {
                            startFolder = folderPath[i]; // This should be "Assets"
                        } else {
                            nextFolder = folderPath[i];
                            string stringTogether = string.Format("{0}/{1}", startFolder, nextFolder);
                            if (!AssetDatabase.IsValidFolder(stringTogether)) {
                                AssetDatabase.CreateFolder(startFolder, nextFolder);
                            }
                            startFolder = stringTogether;
                        }
                    }
                    AssetDatabase.CreateAsset(card, string.Format("{0}/{1}.asset", startFolder, card.cardName));

                    ++lineNumber;
                }
            }
        }
    }

    #endregion

    #region Helper Functions

    // https://answers.unity.com/questions/144200/are-there-any-csv-reader-for-unity3d-without-needi.html
    // splits a CSV row 
    private static string[] SplitCsvLine(string line)
    {
        string pattern = @"
        # Match one value in valid CSV string.
        (?!\s*$)                                      # Don't match empty last value.
        \s*                                           # Strip whitespace before value.
        (?:                                           # Group for value alternatives.
        '(?<val>[^'\\]*(?:\\[\S\s][^'\\]*)*)'       # Either $1: Single quoted string,
        | ""(?<val>[^""\\]*(?:\\[\S\s][^""\\]*)*)""   # or $2: Double quoted string,
        | (?<val>[^,'""\s\\]*(?:\s+[^,'""\s\\]+)*)    # or $3: Non-comma, non-quote stuff.
        )                                             # End group of value alternatives.
        \s*                                           # Strip whitespace after value.
        (?:,|$)                                       # Field ends on comma or EOS.
        ";
        string[] values = (from Match m in Regex.Matches(line, pattern,
            RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline)
            select m.Groups[1].Value).ToArray();
        return values;        
    }

    #endregion
}
