using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] GameObject titleScreen;
    [SerializeField] GameObject stack;
    [SerializeField] Button loginButton;

    const float FadeDuration = 0.4f;

    CanvasGroup titleGroup;
    CanvasGroup stackGroup;
    SpriteRenderer titleSprite;

    void Awake()
    {
        // Auto-resolve by name if not wired in inspector
        if (titleScreen == null)
            titleScreen = transform.Find("TitleScreen").gameObject;
        if (stack == null)
            stack = transform.Find("Stack").gameObject;
        if (loginButton == null)
            loginButton = titleScreen.transform.Find("Login").GetComponent<Button>();

        titleGroup = GetOrAdd(titleScreen);
        stackGroup = GetOrAdd(stack);
        titleSprite = titleScreen.GetComponentInChildren<SpriteRenderer>(true);

        // Stack starts invisible and non-interactive
        stackGroup.alpha = 0f;
        stackGroup.interactable = false;
        stackGroup.blocksRaycasts = false;

        loginButton.onClick.AddListener(OnLogin);
    }

    void OnLogin()
    {
        loginButton.interactable = false;
        StartCoroutine(CrossFade());
    }

    IEnumerator CrossFade()
    {
        float t = 0f;
        Color spriteColor = titleSprite != null ? titleSprite.color : Color.white;
        while (t < FadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / FadeDuration);
            titleGroup.alpha = 1f - p;
            stackGroup.alpha = p;
            if (titleSprite != null)
                titleSprite.color = new Color(spriteColor.r, spriteColor.g, spriteColor.b, 1f - p);
            yield return null;
        }

        titleScreen.SetActive(false);
        stackGroup.interactable = true;
        stackGroup.blocksRaycasts = true;
    }

    static CanvasGroup GetOrAdd(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }
}
