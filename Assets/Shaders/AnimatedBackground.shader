Shader "InboxZero/AnimatedBackground"
{
    Properties
    {
        // Required by Unity's UI system — not sampled by this shader.
        _MainTex    ("Texture", 2D)        = "white" {}

        // 0 = Plasma, 1 = NoiseDrift
        _Mode       ("Mode", Int)          = 0
        _Speed      ("Speed", Float)       = 0.4
        _Scale      ("Scale", Float)       = 3.0
        _PixelSize  ("Pixel Size", Float)  = 2.0

        // Required UI stencil properties
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
            "Queue"           = "Background"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "PreviewType"     = "Plane"
        }

        Stencil
        {
            Ref        [_Stencil]
            Comp       [_StencilComp]
            Pass       [_StencilOp]
            ReadMask   [_StencilReadMask]
            WriteMask  [_StencilWriteMask]
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
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
            };

            int   _Mode;
            float _Speed;
            float _Scale;
            float _PixelSize;

            // ── Noise helpers ─────────────────────────────────────────────────

            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 74.23);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);  // smoothstep
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 4-octave FBM
            float fbm(float2 p)
            {
                float v   = 0.0;
                float amp = 0.5;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    v   += valueNoise(p) * amp;
                    p    = p * 2.1 + float2(1.7, 9.2);
                    amp *= 0.5;
                }
                return v;
            }

            // ── Plasma ────────────────────────────────────────────────────────
            // Classic demo-scene sine-wave plasma with a rainbow color ramp.

            float4 plasmaEffect(float2 uv, float t)
            {
                float v  = sin(uv.x * _Scale + t * 1.1);
                       v += sin(uv.y * _Scale + t * 0.9);
                       v += sin((uv.x + uv.y) * _Scale * 0.7 + t);
                float2 c  = float2(uv.x + 0.5 * sin(t * 0.3),
                                   uv.y + 0.5 * cos(t * 0.2));
                       v += sin(length(c) * _Scale * 1.5 + t);
                       v  = v * 0.25;  // map to roughly -1..1

                float r = sin(v * 3.14159 + 0.000) * 0.5 + 0.5;
                float g = sin(v * 3.14159 + 2.094) * 0.5 + 0.5;
                float b = sin(v * 3.14159 + 4.189) * 0.5 + 0.5;

                // Pull brightness down so it reads as a background, not a torch
                return float4(r * 0.65, g * 0.65, b * 0.65, 1.0);
            }

            // ── Noise Drift ───────────────────────────────────────────────────
            // Slow FBM noise in a dark navy/teal palette — subtle, professional.

            float4 noiseDriftEffect(float2 uv, float t)
            {
                float2 p = uv * _Scale + float2(t * 0.04, t * 0.025);
                float  n = fbm(p);

                // Second FBM layer for more organic variation
                float2 q = uv * _Scale * 1.3 + float2(t * 0.03, -t * 0.02) + float2(5.2, 1.3);
                n = lerp(n, fbm(q), 0.4);

                // Remap to increase contrast in the mid-range
                n = smoothstep(0.2, 0.8, n);

                float3 colorDark = float3(0.02, 0.03, 0.08);  // very dark navy
                float3 colorMid  = float3(0.12, 0.22, 0.42);  // visible deep blue

                return float4(lerp(colorDark, colorMid, n), 1.0);
            }

            // ── Digital Rain ──────────────────────────────────────────────────
            // Columns of pixels falling top-to-bottom — pure shader, no texture.

            float4 digitalRainEffect(float2 uv, float t)
            {
                // Work in pixel-grid cell space so everything is hard-edged
                float resX = 640.0 / _PixelSize;
                float resY = 360.0 / _PixelSize;
                float2 cell = floor(uv * float2(resX, resY));

                // One stream per pixel column; _Scale widens columns (try 2-4)
                float streamCol = floor(cell.x / max(1.0, _Scale * 0.5));

                // Per-column randomness
                float speed  = 0.4 + hash21(float2(streamCol, 0.0)) * 1.2;
                float phase  = hash21(float2(streamCol, 1.0));
                float active = step(0.38, hash21(float2(streamCol, 2.0))); // ~62% active

                // Head falls from top (high cellY) to bottom (cellY = 0)
                // frac cycles 0→1; we want headY to go from resY→0
                float headCellY = (1.0 - frac(t * speed * 0.12 + phase)) * resY;

                float trailLen = 6.0 + hash21(float2(streamCol, 3.0)) * 14.0;

                float dist = headCellY - cell.y;
                float brightness = 0.0;
                if (dist >= 0.0 && dist < trailLen)
                {
                    float fade = 1.0 - (dist / trailLen);
                    brightness = fade * fade;
                }
                // Head cell: flash white-green
                if (dist >= 0.0 && dist < 1.0)
                    brightness = 1.5;

                brightness = saturate(brightness) * active;

                float3 bgColor   = float3(0.00, 0.01, 0.02);
                float3 trailCol  = float3(0.00, 0.50, 0.15);
                float3 headCol   = float3(0.65, 1.00, 0.70);

                float3 col = lerp(bgColor, trailCol, brightness);
                // Blend toward head colour only on the brightest cells
                col = lerp(col, headCol, smoothstep(0.75, 1.0, brightness));

                return float4(col, 1.0);
            }

            // ── Graph Paper ───────────────────────────────────────────────────
            // Slowly drifting grid — minor lines every N pixels, major every 4N.

            float4 graphPaperEffect(float2 uv, float t)
            {
                // Slow diagonal drift
                float2 drift = uv + float2(t * 0.015, t * 0.010);

                // Work in raw pixel space (no pre-snapping — we draw lines ourselves)
                float resX = 640.0 / _PixelSize;
                float resY = 360.0 / _PixelSize;
                float2 px = drift * float2(resX, resY);

                float minor = max(2.0, _Scale);   // minor cell size in pixels
                float major = minor * 4.0;

                // Distance to nearest line (in pixels), for both axes
                float2 minorMod = frac(px / minor) * minor;
                float2 majorMod = frac(px / major) * major;

                float minorDist = min(min(minorMod.x, minor - minorMod.x),
                                      min(minorMod.y, minor - minorMod.y));
                float majorDist = min(min(majorMod.x, major - majorMod.x),
                                      min(majorMod.y, major - majorMod.y));

                float3 bgColor    = float3(0.04, 0.05, 0.06);  // dark charcoal
                float3 minorColor = float3(0.10, 0.14, 0.13);  // faint green-grey line
                float3 majorColor = float3(0.18, 0.26, 0.22);  // visible green line

                float3 col = bgColor;
                if (minorDist < 1.0) col = minorColor;
                if (majorDist < 1.0) col = majorColor;

                return float4(col, 1.0);
            }

            // ── Pixel Fire ────────────────────────────────────────────────────
            // Hard-quantized noise blobs — no lerp, no blur, true pixel art fire.

            float3 fireRamp(float level)
            {
                // 6 hard colour bands — picked with floor(), not lerp()
                if (level < 1.0) return float3(0.02, 0.00, 0.00); // near-black ember
                if (level < 2.0) return float3(0.55, 0.00, 0.00); // deep red
                if (level < 3.0) return float3(0.88, 0.22, 0.00); // orange-red
                if (level < 4.0) return float3(1.00, 0.60, 0.00); // orange
                if (level < 5.0) return float3(1.00, 0.88, 0.10); // yellow
                return              float3(1.00, 1.00, 0.75);      // pale flicker
            }

            // Shared noise sampler for all fire variants
            float fireNoise(float2 uv, float t)
            {
                float resX = 640.0 / _PixelSize;
                float resY = 360.0 / _PixelSize;
                float2 cellUV = floor(uv * float2(resX, resY)) / float2(resX, resY);
                float2 p1 = cellUV * _Scale + float2( t * 0.09,  t * 0.06);
                float2 p2 = cellUV * _Scale * 1.5 + float2(-t * 0.05,  t * 0.08) + float2(3.9, 1.7);
                return fbm(p1) * 0.55 + fbm(p2) * 0.45;
            }

            float4 pixelFireEffect(float2 uv, float t)
            {
                float level = floor(saturate(fireNoise(uv, t)) * 6.0);
                return float4(fireRamp(level), 1.0);
            }

            // ── Muted Retro ───────────────────────────────────────────────────
            // Desaturated ochre/sienna palette — like old pixel art from the 80s.

            float3 mutedRetroRamp(float level)
            {
                if (level < 1.0) return float3(0.05, 0.02, 0.01); // dark brown-black
                if (level < 2.0) return float3(0.28, 0.11, 0.03); // dark ochre
                if (level < 3.0) return float3(0.52, 0.25, 0.07); // burnt sienna
                if (level < 4.0) return float3(0.72, 0.42, 0.13); // dull orange
                if (level < 5.0) return float3(0.88, 0.62, 0.24); // warm amber
                return              float3(0.96, 0.82, 0.50);      // pale cream
            }

            float4 mutedRetroEffect(float2 uv, float t)
            {
                float level = floor(saturate(fireNoise(uv, t)) * 6.0);
                return float4(mutedRetroRamp(level), 1.0);
            }

            // ── Dark Embers ───────────────────────────────────────────────────
            // Mostly black with rare deep-red and orange pockets — like cooling lava.

            float3 darkEmbersRamp(float level)
            {
                if (level < 1.0) return float3(0.00, 0.00, 0.00); // black
                if (level < 2.0) return float3(0.07, 0.01, 0.00); // near-black red
                if (level < 3.0) return float3(0.22, 0.03, 0.00); // deep crimson
                if (level < 4.0) return float3(0.50, 0.07, 0.00); // dark red
                if (level < 5.0) return float3(0.78, 0.18, 0.00); // ember
                return              float3(1.00, 0.42, 0.05);      // bright flare (rare)
            }

            float4 darkEmbersEffect(float2 uv, float t)
            {
                float n = fireNoise(uv, t);
                // Bias toward dark so most of the screen stays black
                n = pow(n, 2.2);
                float level = floor(saturate(n) * 6.0);
                return float4(darkEmbersRamp(level), 1.0);
            }

            // ── Vertex ────────────────────────────────────────────────────────

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // ── Fragment ──────────────────────────────────────────────────────

            float4 frag(v2f i) : SV_Target
            {
                // Snap UVs to a coarser pixel grid for the retro look
                float2 uv = i.uv;
                if (_PixelSize > 1.0)
                {
                    float resX = 640.0 / _PixelSize;
                    float resY = 360.0 / _PixelSize;
                    uv = floor(uv * float2(resX, resY)) / float2(resX, resY);
                }

                float t = _Time.y * _Speed;

                if (_Mode == 0)      return plasmaEffect(uv, t);
                else if (_Mode == 1) return noiseDriftEffect(uv, t);
                else if (_Mode == 2) return digitalRainEffect(uv, t);
                else if (_Mode == 3) return graphPaperEffect(uv, t);
                else if (_Mode == 4) return pixelFireEffect(uv, t);
                else if (_Mode == 5) return mutedRetroEffect(uv, t);
                else                 return darkEmbersEffect(uv, t);
            }
            ENDCG
        }
    }
}
