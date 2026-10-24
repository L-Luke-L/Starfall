using UnityEditor;
using UnityEditor.SceneManagement;
public static class StarfallArtReview {
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Starfall.unity");EditorApplication.isPlaying=true;}
}
