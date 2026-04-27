using UnityEditor;

[InitializeOnLoad]
public static class EchoArchitectGeniesEditorSetup
{
    const string ShowWizardOnStartupKey = "Genies.Sdk.Bootstrap.Editor.ShowWizardOnStartup";
    const string CheckPrerequisitesOnLoadKey = "Genies.Sdk.Bootstrap.Editor.CheckPrerequisitesOnLoad";

    static EchoArchitectGeniesEditorSetup()
    {
        EditorApplication.delayCall += DisableWizardAutopopup;
    }

    static void DisableWizardAutopopup()
    {
        EditorUserSettings.SetConfigValue(ShowWizardOnStartupKey, "false");
        EditorUserSettings.SetConfigValue(CheckPrerequisitesOnLoadKey, "false");
    }
}
