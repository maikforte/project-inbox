using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuBGAnimationBuilder
{
    const string SpritePath   = "Assets/Sprites/Light/MainMenuScreen.png";
    const string ClipPath     = "Assets/Animations/MainMenuBG.anim";
    const string CtrlPath     = "Assets/Animations/MainMenuBG.controller";
    const string PrefabPath   = "Assets/Prefabs/UI/MainMenuPanel.prefab";
    const float  Fps          = 12f;

    [MenuItem("InboxZero/Build MainMenu BG Animation")]
    static void Build()
    {
        // Load sprites sorted by frame index
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SpritePath)
            .OfType<Sprite>()
            .OrderBy(s =>
            {
                var parts = s.name.Split('_');
                return int.TryParse(parts[parts.Length - 1], out int n) ? n : 0;
            })
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError("[MainMenuBGAnimationBuilder] No sprites found at " + SpritePath);
            return;
        }

        // Build AnimationClip
        var clip = new AnimationClip { frameRate = Fps };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var binding = EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / Fps, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        // Delete old assets if they exist
        AssetDatabase.DeleteAsset(ClipPath);
        AssetDatabase.DeleteAsset(CtrlPath);

        AssetDatabase.CreateAsset(clip, ClipPath);

        // Build AnimatorController with one looping state
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CtrlPath);
        var rootStateMachine = ctrl.layers[0].stateMachine;
        var state = rootStateMachine.AddState("Play");
        state.motion = clip;
        state.speed = 1f;
        rootStateMachine.defaultState = state;

        AssetDatabase.SaveAssets();

        // Edit prefab
        using (var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath))
        {
            var root = scope.prefabContentsRoot;
            var bgTransform = root.transform.Find("Background");
            if (bgTransform == null)
            {
                Debug.LogError("[MainMenuBGAnimationBuilder] 'Background' child not found in prefab.");
                return;
            }

            var bg = bgTransform.gameObject;

            // Set Image sprite to frame 0 and reset tint to white
            var img = bg.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = sprites[0];
                img.color  = Color.white;
            }

            // Add Animator and assign controller
            var animator = bg.GetComponent<Animator>();
            if (animator == null)
                animator = bg.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MainMenuBGAnimationBuilder] Done — {sprites.Length} frames @ {Fps} fps. Prefab updated.");
    }
}
