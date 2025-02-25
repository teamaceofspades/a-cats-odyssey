using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using static System.IO.Directory;
using static System.IO.Path;
using static UnityEditor.AssetDatabase;

public static class Setup {
  [MenuItem("Tools/Setup/Create Default Folders")]
  public static void CreateDefaultFolders() {
    Folders.CreateDefault("_Project", "Animation", "Art", "Materials", "Prefabs",
      "ScriptableObjects", "Scripts", "Settings");
    Refresh();
  }

  [MenuItem("Tools/Setup/Import My Favorite Assets")]
  public static void ImportMyFavoriteAssets() {
    Assets.ImportAsset("DOTween HOTween v2.unitypackage", "Demigiant/Editor ExtensionsAnimation");
    Assets.ImportAsset("Selection History.unitypackage", "Staggart Creations/Editor ExtensionsUtilities");
    Assets.ImportAsset("More Effective Coroutines FREE.unitypackage", "Trinary Software\\ScriptingAnimation");
  }

  static class Folders {
    public static void CreateDefault(string root, params string[] folders) {
      var fullpath = Combine(Application.dataPath, root);
      foreach (var folder in folders) {
        var path = Combine(fullpath, folder);
        if (!Exists(path)) {
          CreateDirectory(path);
        }
      }
    }
  }

  static class Assets {
    public static void ImportAsset(string asset, string subfolder, string folder = "C:/Users/Trist/AppData/Roaming/Unity/Asset Store-5.x") {
      ImportPackage(Combine(folder, subfolder, asset), false);
    }
  }
}