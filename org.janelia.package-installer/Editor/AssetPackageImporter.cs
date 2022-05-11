using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace Janelia
{
    public class AssetPackageImporter
    {
        public delegate void AfterImportingDelegate(bool success, string error);

        public void Import(List<string> pkgPathsToImport, AfterImportingDelegate afterImporting)
        {
            _packagePathsToImport = pkgPathsToImport;
            _afterImporting = afterImporting;
            StartPreservingExistingAssets();
            StartImportingNextPackage();
        }

        private void StartImportingNextPackage()
        {
            if (_packagePathsToImport.Count > 0)
            {
                int i = _packagePathsToImport.Count - 1;
                string path = _packagePathsToImport[i];
                _packagePathsToImport.RemoveAt(i);

                AssetDatabase.importPackageStarted += ImportPackageStarted;
                AssetDatabase.importPackageFailed += ImportPackageFailed;
                Debug.Log("Preparing to import package '" + path + "'");
                AssetDatabase.ImportPackage(path, false);
            }
            else
            {
                FinishPreservingExistingAssets();
                _afterImporting(true, "");
            }
        }

        private void ImportPackageStarted(string pkgName)
        {
            Debug.Log("Started importing package '" + pkgName + "'");
            AssetDatabase.importPackageStarted -= ImportPackageStarted;
            AssetDatabase.importPackageCompleted += ImportPackageCompleted;
        }

        private void ImportPackageCompleted(string pkgName)
        {
            Debug.Log("Finished importing package '" + pkgName + "'");
            AssetDatabase.importPackageCompleted -= ImportPackageCompleted;
            AssetDatabase.importPackageFailed -= ImportPackageFailed;
            StartImportingNextPackage();
        }

        private void ImportPackageFailed(string pkgName, string error)
        {
            Debug.LogError("Error importing package '" + pkgName + "': " + error);
            AssetDatabase.importPackageCompleted -= ImportPackageCompleted;
            AssetDatabase.importPackageFailed -= ImportPackageFailed;
            FinishPreservingExistingAssets();
            _afterImporting(false, error);
        }

        private void StartPreservingExistingAssets()
        {
            // An annoying feature of Unity asset packages (with the `.unitypackage` extension) is that 
            // the materials and textures in a package may well be in folders with the same names as
            // original assets already in the scene, but the process of importing the package will not
            // correctly merge those folders.  To avoid problems with the original assets, they must be
            // moved aside to different folders before importing, and then moved back after importing.

            MoveAssetFolder("Materials");
            MoveAssetFolder("Textures");
        }

        private void FinishPreservingExistingAssets()
        {
            RestoreMovedAssetFolder("Materials");
            RestoreMovedAssetFolder("Textures");
        }

        private void MoveAssetFolder(string folder)
        {
            string origFolderPath = "Assets/" + folder;
            if (!AssetDatabase.IsValidFolder(origFolderPath))
            {
                return;
            }

            Debug.Log("Temporarily moving '" + origFolderPath + "'");

            string movedFolderPath = "Assets/" + folder + "-BeforeImport";
            AssetDatabase.MoveAsset(origFolderPath, movedFolderPath);

            AssetDatabase.DeleteAsset(origFolderPath);
        }

        private void RestoreMovedAssetFolder(string folder)
        {
            string movedFolderPath = "Assets/" + folder + "-BeforeImport";
            if (!AssetDatabase.IsValidFolder(movedFolderPath))
            {
                return;
            }

            string origFolderPath = "Assets/" + folder;
            Debug.Log("Restoring temporarily moved '" + origFolderPath + "'");

            if (!AssetDatabase.IsValidFolder(origFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", folder);
            }

            string[] searchInFolders = new string[] { movedFolderPath };
            string[] movedAssetGuids = AssetDatabase.FindAssets("*", searchInFolders);
            foreach (string guid in movedAssetGuids)
            {
                string movedPath = AssetDatabase.GUIDToAssetPath(guid);
                string restoredPath = movedPath.Replace("-BeforeImport", "");
                AssetDatabase.MoveAsset(movedPath, restoredPath);
            }

            AssetDatabase.DeleteAsset(movedFolderPath);
        }

        private List<string> _packagePathsToImport;
        private AfterImportingDelegate _afterImporting;
    }
}
#endif
