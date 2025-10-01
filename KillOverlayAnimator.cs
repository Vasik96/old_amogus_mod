using UnityEngine;

public class KillOverlayAnimator
{
    public static KillOverlayAnimator Instance;

    private Transform quadParent;
    private Transform fullscreenTransform;
    private SpriteRenderer fullscreenRenderer;

    private bool isAnimating = false;
    private bool isShrinking = false;
    private float pauseTime = 0f;
    private float pauseDuration = 2f;

    private Queue<Vector3> shrinkSteps;

    private Vector3 scaleStart = new Vector3(1f, 0f, 1f);
    private Vector3 scaleMid1 = new Vector3(1f, 0.3f, 1f);
    private Vector3 scaleMid2 = new Vector3(1f, 0.5f, 1f);
    private Vector3 scaleFull = new Vector3(1f, 1f, 1f);

    private Quaternion rotStart = Quaternion.Euler(0, 0, 0);
    private Quaternion rotMid1 = Quaternion.Euler(0, 0, 25);
    private Quaternion rotMid2 = Quaternion.Euler(0, 0, 345);

    private float animStepTime = 0.083f;
    private float shrinkStepTime = 0.007f;
    private float animAccumulator = 0f;
    private int animPhase = 0;

    // FullScreen flash
    private float flashTimer = 0f;
    private const float flashDuration = 0.09f; // 90ms from logs

    public enum AnimationPhase
    {
        None,
        Expanding,
        Paused,
        Shrinking,
        Idle
    }

    public AnimationPhase CurrentPhase { get; private set; } = AnimationPhase.None;

    public KillOverlayAnimator()
    {
        Instance = this;
        Reset();
    }

    private void FindObjects()
    {
        var cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            if (quadParent == null)
            {
                var target = cam.transform.Find("Hud/KillOverlay/QuadParent");
                if (target != null)
                    quadParent = target;
            }

            if (fullscreenTransform == null)
            {
                var fs = cam.transform.Find("Hud/KillOverlay/FullScreen");
                if (fs != null)
                {
                    fullscreenTransform = fs;
                    fullscreenRenderer = fs.GetComponent<SpriteRenderer>();
                }
            }
        }
    }

    public void Reset()
    {
        isAnimating = false;
        isShrinking = false;
        pauseTime = 0f;
        animAccumulator = 0f;
        animPhase = 0;
        flashTimer = 0f;

        CurrentPhase = AnimationPhase.None;

        shrinkSteps = new Queue<Vector3>();

        FindObjects();
        if (quadParent != null)
        {
            quadParent.gameObject.SetActive(false);
            quadParent.localScale = scaleStart;
            quadParent.localRotation = rotStart;
        }

        if (fullscreenRenderer != null)
            fullscreenRenderer.enabled = false;
    }

    public void StartAnimation(float waitSeconds = 2f)
    {
        FindObjects();
        if (quadParent == null) return;

        Reset();
        pauseDuration = waitSeconds;
        isAnimating = true;
        animPhase = 0;
        CurrentPhase = AnimationPhase.Expanding;

        quadParent.localScale = scaleStart;
        quadParent.localRotation = rotStart;
        quadParent.gameObject.SetActive(true);

        if (fullscreenRenderer != null)
        {
            fullscreenRenderer.enabled = false; // initially off
        }

        shrinkSteps.Clear();
        float y = 1f;
        while (y > 0f)
        {
            shrinkSteps.Enqueue(new Vector3(1f, y, 1f));
            y -= 0.04f;
            if (y < 0f) y = 0f;
        }
    }



    public void Update()
    {
        FindObjects();
        if (quadParent == null) return;

        if (!isAnimating)
        {
            CurrentPhase = quadParent.gameObject.activeSelf ? AnimationPhase.Idle : AnimationPhase.None;
            return;
        }

        float dt = Time.deltaTime;


        if (!isShrinking && animPhase == 0 && flashTimer <= 0f)
        {
            if (fullscreenRenderer != null)
            {
                fullscreenRenderer.enabled = true;
                flashTimer = flashDuration; // flash only starts with first expansion
            }
        }


        // ----------------------------
        // Handle FullScreen flash exactly like official log
        // Official: True -> False Δt ≈ 0.09s after activation
        // ----------------------------
        if (flashTimer > 0f)
        {
            flashTimer -= dt;
            if (flashTimer <= 0f && fullscreenRenderer != null)
            {
                fullscreenRenderer.enabled = false;
            }
        }

        // ----------------------------
        // Expansion sequence
        // Change 5: Apply scale and rotation simultaneously per step
        // Official log: first scale (0->0.3) and rotation (0->25°) occur at same Δt
        // ----------------------------
        if (!isShrinking && animPhase < 3)
        {
            animAccumulator += dt;
            if (animAccumulator >= animStepTime)
            {
                animAccumulator -= animStepTime;

                switch (animPhase)
                {
                    case 0:
                        quadParent.localScale = scaleMid1;   // (1,0.3,1)
                        quadParent.localRotation = rotMid1;  // 25°
                        break;
                    case 1:
                        quadParent.localScale = scaleMid2;   // (1,0.5,1)
                        quadParent.localRotation = rotMid2;  // 345°
                        break;
                    case 2:
                        quadParent.localScale = scaleFull;   // (1,1,1)
                        quadParent.localRotation = rotStart; // reset to 0°
                        break;
                }

                animPhase++;
            }
        }
        else if (!isShrinking)
        {
            // ----------------------------
            // Change 6: Pause duration matches official Δt=2.0s
            // ----------------------------
            CurrentPhase = AnimationPhase.Paused;
            pauseTime += dt;
            if (pauseTime >= pauseDuration)
            {
                isShrinking = true;
                animAccumulator = 0f;
            }
        }
        else
        {
            // ----------------------------
            // Shrinking sequence matches official
            // Δt per step = 0.007s, scale from 1->0
            // ----------------------------
            CurrentPhase = AnimationPhase.Shrinking;
            animAccumulator += dt;

            while (animAccumulator >= shrinkStepTime && shrinkSteps.Count > 0)
            {
                animAccumulator -= shrinkStepTime;
                quadParent.localScale = shrinkSteps.Dequeue();

                if (shrinkSteps.Count == 0)
                {
                    quadParent.gameObject.SetActive(false); // Active True -> False
                    isAnimating = false;
                    CurrentPhase = AnimationPhase.None;
                }
            }
        }
    }


    public bool IsActive => isAnimating;
}