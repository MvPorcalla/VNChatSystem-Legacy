using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, "mugi")]  // "mugi" = your extension
public class MugiImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        // Load as text
        string script = System.IO.File.ReadAllText(ctx.assetPath);

        // Store it in a TextAsset Unity can recognize
        TextAsset textAsset = new TextAsset(script);
        ctx.AddObjectToAsset("Mugi Script", textAsset);
        ctx.SetMainObject(textAsset);
    }
}
