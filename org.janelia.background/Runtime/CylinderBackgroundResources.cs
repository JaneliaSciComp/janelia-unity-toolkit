namespace Janelia
{
    // Unity requires specific naming conventions for assets and resources, and these constants
    // define names following those conventions.  These constants are shared with code in the `Editor`
    // folder, and to get the `.asmdef` files and `[RuntimeInitializeOnLoadMethod]` attribute to work,
    // they must be defined here in `Runtime` folder and not in `Editor`.

    public static class CylinderBackgroundResources
    {
        public const string RequiredParentFolder = "Assets";
        public const string RequiredResourceFolder = "Resources";
        public const string AssetDatabaseFolder = RequiredParentFolder + "/" + RequiredResourceFolder;

        public const string MaterialName = "UnlitTexture";

        // An asset must be created with the filename extension, but loaded without the extension.
        public const string MaterialCreationName = AssetDatabaseFolder + "/" + MaterialName + ".mat";
        public const string MaterialLoadingName = MaterialName;

        public const string RequiredEditorFolder = "Editor";
        public const string EditorAssetDatabaseFolder = AssetDatabaseFolder + "/" + RequiredEditorFolder;

        public const string WindowStateName = "SetupCylinderSaved";

        // Generic assets like the window state must be created with the filename extension ".asset", but
        // loaded without the extension.
        public const string WindowStateCreationName = EditorAssetDatabaseFolder + "/" + WindowStateName + ".asset";
        public const string WindowStateLoadingName = RequiredEditorFolder + "/" + WindowStateName;
    }
}
