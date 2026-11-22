// ThermalTraceOverlay.shader
// Weapon optic thermal — three switchable modes via _RENDERMODE keyword:
//   _RENDERMODE_TRACE   — see-through trace: cold transparent, hot = thermal colour
//   _RENDERMODE_FULL    — full thermal fill: entire view thermal palette mapped
//   _RENDERMODE_OUTLINE — Sobel edge outlines on hot objects, cold transparent
//
// Pass 0: Main render (EFT: Blit(tmp, rt, material, 0))
// Pass 1: Unused passthrough
// Pass 2: Pre-pass copy (EFT: Blit(rt, tmp, material, 2))

Shader "Thermals/ThermalTraceOverlay"
{
    Properties
    {
        // ── EFT-assigned via SetMaterialProperties ────────────────────────────
        _MainTex                ("Camera RT",           2D)     = "white" {}
        _RampTex                ("Thermal Palette",     2D)     = "white" {}
        _MainTexColorCoef       ("Main Tex Color Coef", Float)  = 1.0
        _MinimumTemperatureValue("Min Temperature",     Float)  = 0.05
        _Noise                  ("Noise Texture",       2D)     = "white" {}
        _NoiseIntensity         ("Noise Intensity",     Float)  = 0.0
        _DepthFade              ("Depth Fade",          Float)  = 0.0
        _RampShift              ("Ramp Shift",          Float)  = 0.0
        _RadiusBlur             ("Radius Blur",         Float)  = 5.0
        _Bias                   ("Bias",                Float)  = 2.0

        // ── Trace settings ────────────────────────────────────────────────────
        _TraceThreshold         ("Trace Threshold",     Range(0,1))      = 0.45
        _TraceSoftness          ("Trace Softness",      Range(0.01,0.5)) = 0.12
        _TraceIntensity         ("Trace Brightness",    Range(0,2))      = 1.2
        _BaseOpacity            ("Cold Area Opacity",   Range(0,1))      = 0.0
        _GlowWidth              ("Glow Width",          Range(0,0.02))   = 0.004
        _GlowIntensity          ("Glow Intensity",      Range(0,1))      = 0.3
        _OutlineWidth           ("Outline Width",       Range(0.001,0.01)) = 0.003
        _OutlineIntensity       ("Outline Intensity",   Range(0,2))        = 1.5

        // ── Colour control (overrides EFT's white-hot palette) ────────────────
        // _UseRampColor: 0 = use _ColdColor→_HotColor gradient (green thermal),
        //                1 = use EFT's _RampTex palette (white hot).
        _HotColor               ("Hot Color",            Color)        = (0.2, 1.0, 0.2, 1)
        _ColdColor              ("Cold Color (Full mode)",Color)        = (0, 0.02, 0, 1)
        _UseRampColor           ("Use EFT Palette",       Range(0,1))   = 0
        _HeatPower              ("Heat Power (contrast)", Range(0.2,3)) = 0.6
        _HeatGain               ("Heat Gain (strength)",  Range(1,4))   = 2.0
        _HeatFloor              ("Heat Floor (cold cutoff)", Range(0,1)) = 0.15
        _DebugHeat              ("DEBUG: Show Raw Heat",   Range(0,1))   = 0

        // Set only on the goggles' thermal camera, so the Holosun keeps its own look.
        _ZoneSensorFx           ("Goggle Sensor Effects",  Range(0,1))   = 0
        _ZoneNoiseTex           ("Goggle Noise Texture",   2D)           = "black" {}
        _ZoneNoiseIntensity     ("Goggle Noise Intensity", Float)        = 0
        _ZoneDepthFade          ("Goggle Distance Fade",   Float)        = 0
    }

    SubShader
    {
        // ── Pass 0: Main render ───────────────────────────────────────────────
        // EFT's OnPreCull runs a ping-pong: scene→temp, pass2(temp→screen),
        // pass0(screen→temp), then plain copy temp→screen. Each Blit OVERWRITES
        // its target, so pass 0 must output the complete opaque image. Blend is
        // OFF — any alpha blending here composites against stale buffer contents
        // (the cause of the white-hot bleed-through). We write final RGB directly.
        Pass
        {
            Name "TRACE_MAIN"
            Blend Off
            Cull Off
            ZWrite Off
            ZTest Always
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            sampler2D   _MainTex;
            float4      _MainTex_TexelSize;
            sampler2D   _RampTex;
            float       _MainTexColorCoef;
            float       _MinimumTemperatureValue;
            float       _RampShift;
            float       _TraceThreshold;
            float       _TraceSoftness;
            float       _TraceIntensity;
            float       _BaseOpacity;
            float       _GlowWidth;
            float       _GlowIntensity;
            float       _OutlineWidth;
            float       _OutlineIntensity;
            float4      _HotColor;
            float4      _ColdColor;
            float       _UseRampColor;
            float       _HeatPower;
            float       _HeatGain;
            float       _HeatFloor;
            float       _DebugHeat;
            // Render mode as a float uniform instead of shader keywords.
            // Keywords were being stripped from the AssetBundle (only FULL
            // survived), causing TRACE/OUTLINE to fall back to a default variant.
            // A float uniform can't be stripped, so all modes always work.
            //   0 = TRACE, 1 = FULL, 2 = OUTLINE, 3 = CLEAR
            float       _RenderMode;
            float       _ZoneSensorFx;
            sampler2D   _ZoneNoiseTex;
            float       _ZoneNoiseIntensity;
            float       _ZoneDepthFade;
            sampler2D_float _CameraDepthTexture;

            // The game's thermal fade: heat x exp(-metres x fade) ^ 0.25.
            float DistanceFade(float2 uv)
            {
                if (_ZoneSensorFx < 0.5 || _ZoneDepthFade <= 0.0) return 1.0;

                float metres = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
                return sqrt(sqrt(saturate(exp(-metres * _ZoneDepthFade))));
            }

            // The game's thermal noise: sqrt(colour^2 + noise x intensity x 0.785), with the noise
            // texture scrolled every frame by the same offsets the game uses.
            float3 ApplyNoise(float3 col, float2 uv)
            {
                if (_ZoneSensorFx < 0.5 || _ZoneNoiseIntensity <= 0.0) return col;

                float2 offset = frac(_Time.x * float2(14345.677734375, -12345.677734375));
                float  n      = tex2D(_ZoneNoiseTex, uv + offset).r * _ZoneNoiseIntensity * 0.785;
                return sqrt(col * col + n);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // Raw heat from _MainTex luminance. The diagnostic proved the thermal
            // data lives directly in _MainTex luminance (hot = bright). EFT's
            // _MinimumTemperatureValue is calibrated for EFT's own internal
            // luminance scaling and is far too large to subtract from raw 0-1
            // luminance — doing so drove heat to zero everywhere (black screen).
            // So we use luminance directly, with a small adjustable floor.
            float SampleHeatRaw(float2 uv)
            {
                float lum = Luminance(tex2D(_MainTex, uv).rgb) * DistanceFade(uv);
                // _HeatFloor removes the dark baseline so cold reads as 0.
                return saturate((lum - _HeatFloor) / max(1.0 - _HeatFloor, 0.001));
            }

            // Gained heat: raw heat with contrast curve + gain, for COLOUR only.
            // _HeatPower < 1 brightens mid heat; _HeatGain scales overall.
            float SampleHeat(float2 uv)
            {
                float h = SampleHeatRaw(uv);
                return saturate(pow(h, _HeatPower) * _HeatGain);
            }

            // Returns the thermal colour for a heat value.
            // Either EFT's palette (_UseRampColor=1) or our cold→hot gradient.
            float4 HeatColor(float heat)
            {
                float4 ramp     = tex2D(_RampTex, float2(heat, 0.5));
                float4 gradient = lerp(_ColdColor, _HotColor, heat);
                return lerp(gradient, ramp, _UseRampColor);
            }

            float SobelHeat(float2 uv)
            {
                float2 t  = _MainTex_TexelSize.xy * _OutlineWidth * 200.0;
                float  tl = SampleHeatRaw(uv + float2(-t.x,  t.y));
                float  tc = SampleHeatRaw(uv + float2( 0.0,  t.y));
                float  tr = SampleHeatRaw(uv + float2( t.x,  t.y));
                float  ml = SampleHeatRaw(uv + float2(-t.x,  0.0));
                float  mr = SampleHeatRaw(uv + float2( t.x,  0.0));
                float  bl = SampleHeatRaw(uv + float2(-t.x, -t.y));
                float  bc = SampleHeatRaw(uv + float2( 0.0, -t.y));
                float  br = SampleHeatRaw(uv + float2( t.x, -t.y));
                float  gx = -tl - 2.0*ml - bl + tr + 2.0*mr + br;
                float  gy = -tl - 2.0*tc - tr + bl + 2.0*bc + br;
                return saturate(length(float2(gx, gy)));
            }

            float4 frag(v2f i) : SV_Target
            {
                float  heat         = SampleHeat(i.uv);
                float4 thermalColor = HeatColor(heat);

                // Runtime branch on _RenderMode float (0=TRACE,1=FULL,2=OUTLINE,3=CLEAR)
                int mode = (int)round(_RenderMode);

                // ═══ DEBUG: visualize heat — RAW in red, GAINED in green ══════
                // Shows both heat samples side by side so we can see which one
                // has usable contrast. Red channel = SampleHeatRaw, green =
                // SampleHeat (gained). If red is black but green is bright, the
                // masks must use the gained sample.
                if (_DebugHeat > 0.5)
                {
                    float raw    = SampleHeatRaw(i.uv);
                    float gained = SampleHeat(i.uv);
                    return float4(raw, gained, 0, 1.0);
                }

                // ═══ CLEAR: pass the scene through unchanged (collimator/red dot)
                // With Blend Off we can't be "transparent" — instead we output
                // _MainTex directly so the normal scope view shows.
                if (mode == 3)
                {
                    return tex2D(_MainTex, i.uv);
                }

                // ═══ FULL: entire view mapped to thermal palette ══════════════
                if (mode == 1)
                {
                    return float4(ApplyNoise(thermalColor.rgb * _TraceIntensity, i.uv), 1.0);
                }

                // ═══ OUTLINE (mode 2): green edges, see-through cold, no white ══
                // Edges of hot objects → bright green. Cold areas → see-through
                // scene. The hot INTERIOR (white-hot in _MainTex) is suppressed
                // toward the cold see-through look so it doesn't shine white —
                // we blend the interior back to a darkened scene, then paint the
                // green edge on top.
                if (mode == 2)
                {
                    float edge      = SobelHeat(i.uv);
                    float heatVal   = SampleHeat(i.uv);
                    float hotRegion = smoothstep(
                        _TraceThreshold - _TraceSoftness,
                        _TraceThreshold + _TraceSoftness, heatVal);
                    float outline   = saturate(edge * hotRegion * _OutlineIntensity);

                    float3 scene    = tex2D(_MainTex, i.uv).rgb;
                    // Suppress the white-hot interior: where it's a hot region but
                    // NOT an edge, fade the bright scene down so it doesn't shine
                    // white. Cold areas (hotRegion≈0) keep the scene untouched.
                    float  suppress = saturate(hotRegion - outline);
                    float3 baseCol  = lerp(scene, scene * 0.15, suppress);

                    float3 edgeCol  = ApplyNoise(_HotColor.rgb * _TraceIntensity, i.uv);
                    float3 col      = lerp(baseCol, edgeCol, outline);
                    return float4(col, 1.0);
                }

                // ═══ TRACE (mode 0): solid green on hot, SEE-THROUGH elsewhere ═══
                // Cold areas show the scene (_MainTex) for a see-through view;
                // hot objects get filled solid green. This is what makes TRACE
                // DISTINCT from FULL — FULL paints cold areas dark green, TRACE
                // leaves them as the see-through scene.
                {
                    float heatVal   = SampleHeat(i.uv);
                    float traceMask = smoothstep(
                        _TraceThreshold - _TraceSoftness,
                        _TraceThreshold + _TraceSoftness, heatVal);

                    float glowMask = 0.0;
                    [branch]
                    if (_GlowWidth > 0.001 && _GlowIntensity > 0.001)
                    {
                        float gw = _GlowWidth;
                        float d  = gw * 0.707;
                        float mx = max(
                            max(max(SampleHeat(i.uv+float2( gw,0)),
                                    SampleHeat(i.uv+float2(-gw,0))),
                                max(SampleHeat(i.uv+float2(0, gw)),
                                    SampleHeat(i.uv+float2(0,-gw)))),
                            max(max(SampleHeat(i.uv+float2( d, d)),
                                    SampleHeat(i.uv+float2(-d, d))),
                                max(SampleHeat(i.uv+float2( d,-d)),
                                    SampleHeat(i.uv+float2(-d,-d)))));
                        float dil = smoothstep(
                            _TraceThreshold - _TraceSoftness*2.0,
                            _TraceThreshold + _TraceSoftness, mx);
                        glowMask = saturate(dil - traceMask) * _GlowIntensity;
                    }

                    float fill   = max(traceMask, glowMask * 0.5);
                    // Cold base = the see-through scene (_MainTex). Hot = bright
                    // solid green. This is the key difference from FULL.
                    float3 scene = tex2D(_MainTex, i.uv).rgb;
                    float3 hotCol = ApplyNoise(_HotColor.rgb * _TraceIntensity, i.uv);
                    float3 col   = lerp(scene, hotCol, fill);
                    return float4(col, 1.0);
                }
            }
            ENDCG
        }

        // ── Pass 1: Unused passthrough ────────────────────────────────────────
        Pass
        {
            Name "PASSTHROUGH_1"
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert_p1
            #pragma fragment frag_p1
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct a1 { float4 v:POSITION; float2 uv:TEXCOORD0; };
            struct v1 { float4 p:SV_POSITION; float2 uv:TEXCOORD0; };
            v1 vert_p1(a1 i){v1 o;o.p=UnityObjectToClipPos(i.v);o.uv=i.uv;return o;}
            float4 frag_p1(v1 i):SV_Target{return tex2D(_MainTex,i.uv);}
            ENDCG
        }

        // ── Pass 2: Pre-pass copy ─────────────────────────────────────────────
        // EFT blits pass 2 then pass 0, CHAINED: pass 2 writes a temp buffer that
        // pass 0 then reads as its _MainTex. So pass 2 must pass the scene through
        // unchanged — it is the source for pass 0, not a separate underlay.
        Pass
        {
            Name "PREPASS_2"
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert_p2
            #pragma fragment frag_p2
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            float     _RadiusBlur;
            float     _Bias;
            float     _ZoneSensorFx;
            struct a2 { float4 v:POSITION; float2 uv:TEXCOORD0; };
            struct v2 { float4 p:SV_POSITION; float2 uv:TEXCOORD0; };
            v2 vert_p2(a2 i){v2 o;o.p=UnityObjectToClipPos(i.v);o.uv=i.uv;return o;}

            // The game's unsharp mask: a 3x3 blur at _RadiusBlur texels weighted 1-2-4, then
            // Bias x colour - blur, or with Bias <= 0 a dilation, max(colour, -Bias x blur).
            float4 frag_p2(v2 i):SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);

                if (_ZoneSensorFx < 0.5) return c;

                float2 r = _MainTex_TexelSize.xy * _RadiusBlur;

                float4 edges = tex2D(_MainTex, i.uv + float2(0, -r.y)) + tex2D(_MainTex, i.uv + float2(-r.x, 0))
                             + tex2D(_MainTex, i.uv + float2(r.x, 0))  + tex2D(_MainTex, i.uv + float2(0, r.y));
                float4 corners = tex2D(_MainTex, i.uv + float2(-r.x, -r.y)) + tex2D(_MainTex, i.uv + float2(r.x, -r.y))
                               + tex2D(_MainTex, i.uv + float2(-r.x, r.y))  + tex2D(_MainTex, i.uv + float2(r.x, r.y));
                float4 blur = (c * 4.0 + edges * 2.0 + corners) * 0.0625;

                return _Bias > 0.0 ? _Bias * c - blur : max(c, -_Bias * blur);
            }
            ENDCG
        }
    }

    Fallback "Hidden/Internal-BlackError"
}
