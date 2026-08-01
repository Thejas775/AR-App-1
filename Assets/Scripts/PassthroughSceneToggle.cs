using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Swaps between passthrough (real room) and a virtual environment.
/// The environment scene is loaded additively so the camera rig, hand
/// tracking and the Omnitrix all survive the transition.
/// </summary>
public class PassthroughSceneToggle : MonoBehaviour
{
    [Header("Passthrough")]
    public OVRPassthroughLayer passthroughLayer;
    public Camera centerEyeCamera;

    [Header("Scene")]
    public string sceneName = "OmnitrixWorld";

    public bool WorldIsLoaded { get; private set; }

    private CameraClearFlags passthroughClearFlags;
    private Color passthroughClearColor;
    private bool busy;

    void Awake()
    {
        if (centerEyeCamera != null)
        {
            passthroughClearFlags = centerEyeCamera.clearFlags;
            passthroughClearColor = centerEyeCamera.backgroundColor;
        }
    }

    public void Toggle()
    {
        if (busy)
            return;

        StartCoroutine(WorldIsLoaded ? ExitWorld() : EnterWorld());
    }

    private IEnumerator EnterWorld()
    {
        busy = true;

        var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (load != null && !load.isDone)
            yield return null;

        // Make the world scene active so its skybox / fog / ambient light apply.
        var world = SceneManager.GetSceneByName(sceneName);
        if (world.IsValid() && world.isLoaded)
            SceneManager.SetActiveScene(world);

        if (passthroughLayer != null)
            passthroughLayer.enabled = false;

        if (centerEyeCamera != null)
            centerEyeCamera.clearFlags = CameraClearFlags.Skybox;

        WorldIsLoaded = true;
        busy = false;

        Debug.Log("Passthrough off - entered " + sceneName);
    }

    private IEnumerator ExitWorld()
    {
        busy = true;

        if (centerEyeCamera != null)
        {
            centerEyeCamera.clearFlags = passthroughClearFlags;
            centerEyeCamera.backgroundColor = passthroughClearColor;
        }

        if (passthroughLayer != null)
            passthroughLayer.enabled = true;

        var world = SceneManager.GetSceneByName(sceneName);
        if (world.IsValid() && world.isLoaded)
        {
            SceneManager.SetActiveScene(gameObject.scene);

            var unload = SceneManager.UnloadSceneAsync(world);
            while (unload != null && !unload.isDone)
                yield return null;
        }

        WorldIsLoaded = false;
        busy = false;

        Debug.Log("Passthrough on - left " + sceneName);
    }
}
