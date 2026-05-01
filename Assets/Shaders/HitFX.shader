Shader "InboxZero/HitFX"
{
    // Pixel-art slash + dissolve effect for the combat HitOverlay.
    // Phase 1: a thin diagonal slash sweeps upper-right -> lower-left.
    // Phase 2: the slash pixels break apart into tiny 1px specks that drift
    //          upward and fade, dissolving progressively from slash-start to slash-end.
    // Driven by _Progress 0->1 from C#.

    Properties
    {
        _MainTex         ("Texture", 2D)             = "white" {}
        _Progress        ("Progress", Range(0,1))    = 0

        _StencilComp     ("Stencil Comparison", Float) = 8
        _Stencil         ("Stencil ID",         Float) = 0
        _StencilOp       ("Stencil Operation",  Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask",  Float) = 255
        _ColorMask       ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+10"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "PreviewType"     = "Plane"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            float _Progress;

            // Per-cell hash: deterministic random per integer cell coordinate.
            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 74.23);
                return frac(p.x * p.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float p = saturate(_Progress);
                if (p <= 0.001 || p >= 0.999) return float4(0, 0, 0, 0);

                // ── Fine pixel grid (N=80 → ~2 canvas px per cell at 160px panel) ──
                const float N = 80.0;
                float2 cell = floor(i.uv * N);
                float2 pos  = cell - N * 0.5 + 0.5; // center-relative, in cells

                // ── Slash geometry ─────────────────────────────────────────────
                // Line: x + y = 0  (diagonal upper-right → lower-left).
                // slashDist   = perpendicular distance from the line.
                // signedAlong = signed projection onto (1,-1)/√2:
                //               +large = upper-right end, -large = lower-left end.
                float slashDist   = abs(pos.x + pos.y) * 0.7071;
                float signedAlong = (pos.x - pos.y) * 0.7071;
                float halfLen     = N * 0.62; // line extends almost corner-to-corner

                // ── Phase 1: slash sweeps across (p 0 → sweepEnd) ─────────────
                // Blade tip travels from +halfLen (upper-right) to -halfLen (lower-left).
                const float sweepEnd = 0.30;
                float sweepP = saturate(p / sweepEnd);
                float tipPos = halfLen * (1.0 - 2.0 * sweepP);

                // Only the region the blade has already passed is visible (cutDist >= 0).
                float cutDist  = signedAlong - tipPos; // 0 at tip, grows behind it
                float trailLen = N * 0.50;             // trailing glow length

                // Solid line fades out quickly once the dissolve phase starts.
                float slashFadeOut = saturate(1.0 - (p - sweepEnd) / 0.10);

                float4 slashOut = float4(0, 0, 0, 0);
                if (slashDist < 0.6 && cutDist >= 0.0 && cutDist < trailLen && slashFadeOut > 0.0)
                {
                    float trailAlpha = 1.0 - (cutDist / trailLen);
                    trailAlpha = trailAlpha * trailAlpha;
                    // White at the tip, pale-yellow behind it.
                    float3 col = lerp(float3(1.0, 1.0, 1.0), float3(1.0, 0.9, 0.5),
                                      saturate(cutDist / (trailLen * 0.6)));
                    slashOut = float4(col, trailAlpha * slashFadeOut);
                }

                // ── Phase 2: dissolve (p dissolveStart → 1.0) ────────────────
                // Each cell asks: "where did I originally come from?"
                // If that origin lies on the slash line AND the wave has reached it,
                // this cell is a drifting dissolve particle.
                //
                // Technique — inverse drift:
                //   • Each cell has a per-cell random drift velocity (hash-seeded).
                //   • posOrig = pos - driftVel * dissolveProgress
                //   • Check origSlashDist and origAlong to see if origin was on the line.
                //   • The wave delay makes the dissolution travel from upper-right to lower-left.
                const float dissolveStart = 0.28;
                float dissolveP = saturate((p - dissolveStart) / (1.0 - dissolveStart));

                float4 dissolveOut = float4(0, 0, 0, 0);

                if (dissolveP > 0.0)
                {
                    // Per-cell random drift — seeded by integer cell coords so
                    // each "pixel" gets a stable, unique direction.
                    float rx = hash21(cell);
                    float ry = hash21(cell + float2(7.3,  3.1));
                    float rs = hash21(cell + float2(13.7, 6.2));

                    // Horizontal: slight left/right wobble.
                    // Vertical:   always upward (range -0.4 to -1.0).
                    float2 driftDir = float2((rx * 2.0 - 1.0) * 0.45,
                                            -(0.4 + ry * 0.6));
                    float  driftSpd = 0.5 + rs * 0.5; // speed varies per pixel

                    // Max drift over full dissolve lifetime: 6 cells (~12 canvas px).
                    // Small enough to feel "floating", not flying.
                    const float maxDrift = 6.0;

                    float2 drift   = driftDir * dissolveP * maxDrift * driftSpd;
                    float2 posOrig = pos - drift; // estimated origin before drift

                    float origSlashDist  = abs(posOrig.x + posOrig.y) * 0.7071;
                    float origAlong      = (posOrig.x - posOrig.y) * 0.7071;

                    if (origSlashDist < 0.6 && abs(origAlong) < halfLen)
                    {
                        // Wave delay: upper-right dissolves first (origAlong = +halfLen → delay 0),
                        // lower-left dissolves last (origAlong = -halfLen → delay 0.65).
                        float alongNorm  = (origAlong + halfLen) / (2.0 * halfLen); // 0..1, 1=upper-right
                        float waveDelay  = (1.0 - alongNorm) * 0.65;
                        float localDP    = saturate((dissolveP - waveDelay) / max(1.0 - waveDelay, 0.001));

                        if (localDP > 0.0)
                        {
                            // Fade: fully opaque at localDP=0, fully transparent at localDP=1.
                            float alpha = (1.0 - localDP) * (1.0 - localDP);
                            // Colour: white → pale yellow → transparent (keeps the slashy look).
                            float3 col = lerp(float3(1.0, 1.0, 0.9), float3(1.0, 0.6, 0.1), localDP);
                            dissolveOut = float4(col, alpha);
                        }
                    }
                }

                // ── Composite: slash (draw phase) over dissolve particles ───────
                float4 result = dissolveOut;
                if (slashOut.a > result.a) result = slashOut;
                return result;
            }
            ENDCG
        }
    }
}
