// FusionNVG.shader
// Fusion goggle overlay — thermal zone, tube character, and device faceplate.
//
// THIS SHADER DOES NOT DO NIGHT VISION, AND DOES NOT READ THE SCREEN.
// It is drawn by FusionOverlayCamera as an alpha-blended full-screen pass AFTER
// the host camera has finished, upscale included. BSG's own NightVision material
// produces the tube image underneath and is never touched; everything here is
// additive on top of it, and anywhere alpha is 0 the vanilla image shows through
// byte for byte.
//
// LAYER ORDER, bottom to top:
//   1. Thermal zone   — the second camera's render, masked to up to four circles
//                       or to one rectangle, with an optional heat key so cold
//                       areas stay transparent.
//   2. Device overlay — the HUD, or a static bezel/reticle texture. Topmost:
//                       hardware occludes.
//
// Layer 2 draws even when the thermal channel is off; a HUD does not vanish
// because the thermal is disabled.
//
// Nothing is sampled from the screen, so aspect cannot come from
// _MainTex_TexelSize — it is passed in as _ZoneAspect from C#.

Shader "Thermals/FusionNVG"
{
    Properties
    {
        // _ThermalZoneRT is deliberately NOT declared here. A material-level
        // value shadows a global one in Unity, so declaring it with a "black"
        // default meant the material sampled that default forever. It is a
        // uniform in the CGPROGRAM below, set explicitly on the material.
        _ThermalZoneEnabled  ("Zone Enabled",        Range(0,1))  = 1
        // Zones are pushed as a vector array from C#: xy = centre, z = radius,
        // w = edge softness. _ZoneCount says how many are live.
        _ZoneCount           ("Zone Count",          Range(0,4))  = 1
        // 0 = the circles above, 1 = one rectangle. The rectangle is pushed from
        // C# as _ZoneRect: xy = bottom-left, zw = top-right, in screen UV.
        _ZoneShape           ("Zone Shape",          Range(0,1))  = 0
        _ZoneRect            ("Zone Rectangle",      Vector)      = (0.25,0.25,0.75,0.75)
        _ZoneRectSoftness    ("Rectangle Edge",      Range(0,0.1)) = 0.002
        _ThermalZoneUvScale  ("Zone Magnification",  Float)       = 1
        _ThermalZoneOpacity  ("Zone Opacity",        Range(0,1))  = 1
        _ThermalZoneZoom     ("Zone Zoom",           Float)       = 1
        _ZoneAspect          ("Aspect (w/h)",        Float)       = 1.777

        // Screen-space remap. Identity in composite mode 2; only the pre-upscale
        // modes need a correction.
        _ScreenUvScale       ("Screen UV Scale",     Vector)      = (1,1,0,0)
        _ScreenUvOffset      ("Screen UV Offset",    Vector)      = (0,0,0,0)

        // Heat keying: draw only hot pixels, let the NVG show through cold ones.
        _HeatKeyOnly         ("Heat Only",           Range(0,1))  = 0
        _HeatKeyThreshold    ("Heat Only Threshold", Range(0,1))  = 0.15
        _HeatKeySoftness     ("Heat Only Softness",  Range(0.001,0.5)) = 0.08
        // How opaque the cold areas are while Heat Only is on. 0 leaves them fully
        // see-through; above 0 lays the cold colour over the night vision, so an
        // outline mode can sit on a dark backing.
        _HeatKeyBackground   ("Heat Only Background", Range(0,1)) = 0

        // Device overlay — bezel, reticle, tube artwork, authored in Unity.
        _OverlayTex          ("Device Overlay",      2D)          = "black" {}
        _OverlayOpacity      ("Overlay Opacity",     Range(0,1))  = 0
        _OverlayTint         ("Overlay Tint",        Color)       = (1,1,1,1)

        // Diagnostic: flat magenta instead of the thermal sample.
        _ZoneDebugFill       ("Debug Fill",          Range(0,1))  = 0
    }

    SubShader
    {
        Tags { "RenderType"="Overlay" }

        Pass
        {
            Name "FUSION_GOGGLE_OVERLAY"
            Cull Off
            ZWrite Off
            ZTest Always
            Fog { Mode Off }
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            sampler2D _ThermalZoneRT;
            float     _ThermalZoneEnabled;
            float     _ZoneCount;
            float4    _Zones[4];          // xy centre, z radius, w softness
            float     _ZoneShape;         // 0 = circles, 1 = rectangle
            float4    _ZoneRect;          // xy bottom-left, zw top-right
            float     _ZoneRectSoftness;  // edge softness, in screen heights

            // Tube mask — BSG's own, so dual and quad layouts align exactly.
            sampler2D _TubeMask;
            float     _TubeClip;          // 0 = off
            float     _TubeInvMaskSize;   // 1 / TextureMask.Size
            float     _TubeInvAspect;     // maskHeight / maskWidth
            float     _TubeCameraAspect;
            float     _TubeMaskSoftness;
            float     _TubeMaskChannel;   // 0 = alpha, 1 = red
            float     _TubeMaskInvert;
            float     _ThermalZoneUvScale;
            float     _ThermalZoneOpacity;
            float     _ThermalZoneZoom;
            float     _ZoneAspect;

            float4    _ScreenUvScale;
            float4    _ScreenUvOffset;

            float     _HeatKeyOnly;
            float     _HeatKeyThreshold;
            float     _HeatKeySoftness;
            float     _HeatKeyBackground;

            sampler2D _OverlayTex;
            float     _OverlayOpacity;
            float4    _OverlayTint;


            float     _ZoneDebugFill;

            // Standard blit vertex path. No manual y-flip: Blit's own quad
            // already carries the right UV orientation for the platform.
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // Source-over compositing with straight (non-premultiplied) alpha.
            // Layers go bottom to top; the accumulated pair is what the
            // fixed-function blend then puts over the night vision image.
            void Over(inout float3 col, inout float a, float3 layerCol, float layerA)
            {
                float outA = layerA + a * (1.0 - layerA);
                if (outA > 0.0001)
                    col = (layerCol * layerA + col * a * (1.0 - layerA)) / outA;
                a = outA;
            }

            // The device faceplate or HUD. Applied over whatever the thermal left in
            // the accumulator, including nothing.
            void ApplyDeviceLayers(inout float3 col, inout float a, float2 screenUv)
            {
                [branch]
                if (_OverlayOpacity > 0.001)
                {
                    float4 ov = tex2D(_OverlayTex, screenUv);
                    Over(col, a, ov.rgb * _OverlayTint.rgb, ov.a * _OverlayOpacity);
                }
            }

            // Coverage of the thermal circles at this pixel. Zones are combined
            // with max rather than summed, so where two tubes overlap the
            // thermal does not double up into a bright seam.
            float ZoneCoverage(float2 screenUv)
            {
                float cov = 0.0;
                int count = (int)min(_ZoneCount, 4.0);

                [unroll(4)]
                for (int z = 0; z < 4; z++)
                {
                    if (z >= count) break;

                    float4 zd = _Zones[z];

                    // Radius 0 marks a disabled zone — C# zeroes it rather than
                    // packing a separate flag.
                    if (zd.z <= 0.0001) continue;

                    float2 d = screenUv - zd.xy;
                    d.x *= _ZoneAspect;
                    cov = max(cov, 1.0 - smoothstep(zd.z - zd.w, zd.z + zd.w, length(d)));
                }
                return cov;
            }

            // Coverage of the single rectangular zone. A box signed distance:
            // negative inside, positive outside, measured in screen heights so
            // the edge is equally soft on all four sides at any aspect.
            float RectCoverage(float2 screenUv)
            {
                float2 d = max(_ZoneRect.xy - screenUv, screenUv - _ZoneRect.zw);
                d.x *= _ZoneAspect;

                float outside = max(d.x, d.y);
                float soft = max(_ZoneRectSoftness, 0.00001);

                return 1.0 - smoothstep(-soft, soft, outside);
            }

            // Samples the goggle's tube mask. Reconstructs TextureMask's own UV
            // math: _InvMaskSize = 1/Size, _InvAspect = maskH/maskW, and the
            // camera aspect squares it up against the screen.
            float TubeMaskValue(float2 screenUv)
            {
                if (_TubeClip < 0.5) return 1.0;

                float2 muv = screenUv - 0.5;
                muv.x *= _TubeCameraAspect * _TubeInvAspect;
                muv   *= _TubeInvMaskSize;
                muv   += 0.5;

                // Past the texture's edge there is no tube either way — the
                // invert below must not turn that into "everywhere is tube".
                if (muv.x < 0.0 || muv.x > 1.0 || muv.y < 0.0 || muv.y > 1.0)
                    return 0.0;

                float4 m = tex2D(_TubeMask, muv);
                float  v = (_TubeMaskChannel > 0.5) ? m.r : m.a;

                // BSG's masks mark the SURROUND, not the tube, because they are
                // used to darken outside the optic. Taken raw, the clip keeps
                // exactly the wrong region.
                if (_TubeMaskInvert > 0.5) v = 1.0 - v;

                return (_TubeMaskSoftness > 0.001)
                    ? smoothstep(0.5 - _TubeMaskSoftness, 0.5 + _TubeMaskSoftness, v)
                    : step(0.5, v);
            }

            float4 frag(v2f i) : SV_Target
            {
                // Work in SCREEN space. Identity in composite mode 2; the
                // pre-upscale modes need the remap.
                float2 screenUv = i.uv * _ScreenUvScale.xy + _ScreenUvOffset.xy;

                // Accumulator. Starts fully transparent, so anywhere no layer
                // draws, the night vision underneath shows through untouched.
                float3 col = float3(0, 0, 0);
                float  a   = 0.0;

                [branch]
                if (_ThermalZoneEnabled > 0.001)
                {
                    float zone = (_ZoneShape > 0.5)
                               ? RectCoverage(screenUv)
                               : ZoneCoverage(screenUv);

                    // Clip to the tubes. Thermal bleeding into the black around
                    // the tubes reads as a bug, so the mask gates everything.
                    zone *= TubeMaskValue(screenUv);

                    [branch]
                    if (zone > 0.001)
                    {
                        if (_ZoneDebugFill > 0.5)
                        {
                            col = float3(1, 0, 1);
                            a   = zone;
                        }
                        else
                        {
                            // Scale UVs about the SCREEN CENTRE, not a zone
                            // centre. The thermal camera looks straight ahead, so
                            // the middle of its render is the forward axis —
                            // anchoring to a zone would drag the sampling window
                            // with that zone and break world alignment, and with
                            // several zones there is no single centre to use.
                            float scale = _ThermalZoneUvScale
                                        / max(_ThermalZoneZoom, 0.01);
                            float2 zoneUv = 0.5 + (screenUv - 0.5) * scale;

                            float inBounds = step(0.0, zoneUv.x) * step(zoneUv.x, 1.0)
                                           * step(0.0, zoneUv.y) * step(zoneUv.y, 1.0);

                            float3 thermal = tex2D(_ThermalZoneRT, zoneUv).rgb;
                            float alpha = zone * inBounds * _ThermalZoneOpacity;

                            // HEAT KEYING. The thermal render is hot-colour on
                            // near-black, so its brightness IS the heat signal —
                            // key alpha off it and cold areas go transparent,
                            // revealing the night vision while the zones still
                            // bound where thermal can appear.
                            //
                            // max(rgb) rather than a luminance dot product: a
                            // saturated orange has low perceptual luminance and a
                            // weighted formula would key out the hottest thing on
                            // screen.
                            [branch]
                            if (_HeatKeyOnly > 0.5)
                            {
                                float heat = max(thermal.r,
                                                 max(thermal.g, thermal.b));
                                float key = smoothstep(
                                    _HeatKeyThreshold - _HeatKeySoftness,
                                    _HeatKeyThreshold + _HeatKeySoftness,
                                    heat);
                                alpha *= lerp(_HeatKeyBackground, 1.0, key);
                            }

                            col = thermal;
                            a   = alpha;
                        }
                    }
                }

                ApplyDeviceLayers(col, a, screenUv);

                return float4(col, a);
            }
            ENDCG
        }
    }

    Fallback Off
}
