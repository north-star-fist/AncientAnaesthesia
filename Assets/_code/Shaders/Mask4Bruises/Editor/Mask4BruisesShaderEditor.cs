using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace Sergei.Safonov.Shaders {

    public class Mask4BruisesShaderEditor : ShaderGUI {
        // Keywords
        private const string KeywordRenderingModeCutout = "_RENDERING_CUTOUT";

        private const string KeywordMainEmissionMap = "_EMISSION_MAP";

        // Tags
        private const string MaterialTagRenderType = "RenderType";

        // Properties

        // Main
        private const string PropNameMainTex = "_MainTex";
        private const string PropNameMainColorTint = "_Color";
        private const string PropNameAlphaCutoff = "_Cutoff";
        private const string PropNameMetallicMap = "_MetalMapTex";
        private const string PropNameMetallic = "_Metallic";
        private const string PropNameMainNormalMap = "_NormalMapTex";
        private const string PropNameMainNormalsScale = "_NormalScale";
        private const string PropNameMainSmoothness = "_Smoothness";
        private const string PropNameMainSmoothnessSource = "_SmoothnessSource";
        private const string PropNameMainEmissionMap = "_EmissionMapTex";
        private const string PropNameMainEmissionColor = "_EmissionColor";

        // Alternative
        private const string PropNameAltMaskTex = "_AltMaskTex";

        private const string PropNameAltPortion = "_AltPortion";
        private const string PropNameAltTex = "_AltTex";
        private const string PropNameAltColorTint = "_AltColor";
        private const string PropNameAltMetallic = "_AltMetallic";
        private const string PropNameAltNormalMap = "_AltNormalMapTex";
        private const string PropNameAltNormalsScale = "_AltNormalScale";
        private const string PropNameAltSmoothness = "_AltSmoothness";
        private const string PropNameAltSmoothnessSource = "_AltSmoothnessSource";

        // Alternative 2
        private const string PropNameAltPortion2 = "_AltPortion2";
        private const string PropNameAltTex2 = "_AltTex2";
        private const string PropNameAltColorTint2 = "_AltColor2";
        private const string PropNameAltMetallic2 = "_AltMetallic2";
        private const string PropNameAltNormalMap2 = "_AltNormalMapTex2";
        private const string PropNameAltNormalsScale2 = "_AltNormalScale2";
        private const string PropNameAltSmoothness2 = "_AltSmoothness2";
        private const string PropNameAltSmoothnessSource2 = "_AltSmoothnessSource2";

        // Alternative 3
        private const string PropNameAltPortion3 = "_AltPortion3";
        private const string PropNameAltTex3 = "_AltTex3";
        private const string PropNameAltColorTint3 = "_AltColor3";
        private const string PropNameAltMetallic3 = "_AltMetallic3";
        private const string PropNameAltNormalMap3 = "_AltNormalMapTex3";
        private const string PropNameAltNormalsScale3 = "_AltNormalScale3";
        private const string PropNameAltSmoothness3 = "_AltSmoothness3";
        private const string PropNameAltSmoothnessSource3 = "_AltSmoothnessSource3";

        // Alternative 4
        private const string PropNameAltPortion4 = "_AltPortion4";
        private const string PropNameAltTex4 = "_AltTex4";
        private const string PropNameAltColorTint4 = "_AltColor4";
        private const string PropNameAltMetallic4 = "_AltMetallic4";
        private const string PropNameAltNormalMap4 = "_AltNormalMapTex4";
        private const string PropNameAltNormalsScale4 = "_AltNormalScale4";
        private const string PropNameAltSmoothness4 = "_AltSmoothness4";
        private const string PropNameAltSmoothnessSource4 = "_AltSmoothnessSource4";


        // Culling, blending modes and Z-Buffer writing
        private const string PropNameCull = "_Cull";
        private const string PropNameSrcBlendMode = "_SrcBlend";
        private const string PropNameDstBlendMode = "_DstBlend";
        private const string PropNameZWrite = "_ZWrite";

        Material _target;
        MaterialEditor _editor;
        MaterialProperty[] _properties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
            _target = materialEditor.target as Material;
            if (_target == null) {
                return;
            }
            _editor = materialEditor;
            _properties = properties;

            RenderingMode rm = DoRenderingMode();
            DoMain(rm);
            DoAlternatives();
            DoAdvanced();
        }

        RenderingMode DoRenderingMode() {
            RenderingMode mode = RenderingMode.Opaque;
            if (IsKeywordEnabled(KeywordRenderingModeCutout)) {
                mode = RenderingMode.Cutout;
            }

            EditorGUI.BeginChangeCheck();
            mode = (RenderingMode)EditorGUILayout.EnumPopup(
                MakeLabel("Rendering Mode"), mode
            );
            if (EditorGUI.EndChangeCheck()) {
                RecordAction("Rendering Mode Changing");
                SetKeyword(KeywordRenderingModeCutout, mode == RenderingMode.Cutout);

                RenderingSettings settings = RenderingSettings.modes[(int)mode];
                foreach (Material m in _editor.targets) {
                    m.renderQueue = (int)settings.queue;
                    m.SetOverrideTag(MaterialTagRenderType, settings.renderType);
                    m.SetInteger(PropNameSrcBlendMode, (int)settings.srcBlend);
                    m.SetInteger(PropNameDstBlendMode, (int)settings.dstBlend);
                    m.SetInteger(PropNameZWrite, settings.zWrite ? 1 : 0);
                }
            }
            return mode;
        }

        void DoMain(RenderingMode rm) {
            GUILayout.Label("Main Surface", EditorStyles.boldLabel);

            DoTexture(PropNameMainTex, PropNameMainColorTint, "Albedo (RGB)");
            if (rm is RenderingMode.Cutout) {
                DoShaderPropertyIntendedBy(FindProperty(PropNameAlphaCutoff), 2);
            }
            DoTexture(PropNameMetallicMap, PropNameMetallic, "Metallic (R)[A]");
            DoSmoothness(PropNameMainSmoothness, PropNameMainSmoothnessSource);
            DoTexture(PropNameMainNormalMap, PropNameMainNormalsScale, null);
            DoTexture(
                PropNameMainEmissionMap,
                PropNameMainEmissionColor,
                "Emission (RGB)",
                KeywordMainEmissionMap,
                true
            );
            DoTileAndScale(PropNameMainTex);
        }

        void DoAlternatives() {
            GUILayout.Label("Alternative Surface", EditorStyles.boldLabel);
            DoTexture(
                PropNameAltMaskTex,
                null,
                "Alternative Mask",
                null,
                false
            );

            DoAltChannel(
                PropNameAltPortion,
                PropNameAltTex,
                PropNameAltColorTint,
                PropNameAltMetallic,
                PropNameAltSmoothness,
                PropNameAltSmoothnessSource,
                PropNameAltNormalMap,
                PropNameAltNormalsScale,
                0
            );
            DoAltChannel(
                PropNameAltPortion2,
                PropNameAltTex2,
                PropNameAltColorTint2,
                PropNameAltMetallic2,
                PropNameAltSmoothness2,
                PropNameAltSmoothnessSource2,
                PropNameAltNormalMap2,
                PropNameAltNormalsScale2,
                1
            );
            DoAltChannel(
                PropNameAltPortion3,
                PropNameAltTex3,
                PropNameAltColorTint3,
                PropNameAltMetallic3,
                PropNameAltSmoothness3,
                PropNameAltSmoothnessSource3,
                PropNameAltNormalMap3,
                PropNameAltNormalsScale3,
                2
            );
            DoAltChannel(
                PropNameAltPortion4,
                PropNameAltTex4,
                PropNameAltColorTint4,
                PropNameAltMetallic4,
                PropNameAltSmoothness4,
                PropNameAltSmoothnessSource4,
                PropNameAltNormalMap4,
                PropNameAltNormalsScale4,
                3
            );
        }

        private void DoAltChannel(
            string propNameAltPortion,
            string propNameAltTex,
            string propNameAltColorTint,
            string propNameAltMetallic,
            string propNameAltSmoothness,
            string propNameAltSmoothnessSource,
            string propNameAltNormalMap,
            string propNameAltNormalsScale,
            int channel
        ) {
            DoShaderPropertyIntendedBy(FindProperty(propNameAltPortion), 2);
            DoAltTexture(
                propNameAltTex,
                propNameAltColorTint,
                $"Alternative {channel + 1} Albedo (RGB)"
            );
            DoShaderPropertyIntendedBy(FindProperty(propNameAltMetallic), 2);
            DoSmoothness(propNameAltSmoothness, propNameAltSmoothnessSource);
            DoAltTexture(propNameAltNormalMap, propNameAltNormalsScale);
        }


        void DoAdvanced() {
            GUILayout.Label("Advanced Options", EditorStyles.boldLabel);

            MaterialProperty cullProp = FindProperty(PropNameCull);
            if (cullProp == null) {
                return;
            }
            CullMode cullMode = (CullMode)cullProp.intValue;

            EditorGUI.BeginChangeCheck();
            cullMode = (CullMode)EditorGUILayout.EnumPopup(MakeLabel("Cull Mode"), cullMode);
            if (EditorGUI.EndChangeCheck()) {
                RecordAction("Cull Mode");
                cullProp.intValue = (int)cullMode;
            }

            //_editor.EnableInstancingField();
        }


        private void DoSmoothness(string propNameMainSmoothness, string propNameSmoothnessSource) {
            DoShaderPropertyIntendedBy(FindProperty(propNameMainSmoothness), 2);
            var prop = FindProperty(propNameSmoothnessSource);
            if (prop == null) {
                return;
            }
            SmoothnessSourceMetal source = (SmoothnessSourceMetal)prop.intValue;
            var newSource = (SmoothnessSourceMetal)EditorGUILayout.EnumPopup(prop.displayName, source);
            prop.intValue = (int)newSource;
        }

        private void DoTexture(
            string texturePropName,
            string extraPropName = null,
            string tooltip = null,
            string keyword = null,
            bool emissive = false,
            string channelProp = null
        ) {
            MaterialProperty texProp = FindProperty(texturePropName);
            if (texProp == null) {
                return;
            }
            if (keyword == null) {
                doIt(texProp, extraPropName, tooltip, emissive, channelProp);
            } else {
                Texture tex = texProp.textureValue;
                EditorGUI.BeginChangeCheck();
                doIt(texProp, extraPropName, tooltip, emissive, channelProp);
                if (EditorGUI.EndChangeCheck()) {
                    if (tex != texProp.textureValue) {
                        RecordAction($"Switching {keyword}: {texProp.textureValue != null}");
                        SetKeyword(keyword, texProp.textureValue);
                    }
                    if (emissive) {
                        _editor.LightmapEmissionProperty(2);
                        foreach (Material m in _editor.targets) {
                            // Being sure we invest into GI anyway (so we uncheck EmissiveIsBlack)
                            m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                        }
                    }
                }
            }


            void doIt(MaterialProperty tex, string extraPropName, string tooltip, bool hdr = false, string channel = null) {
                if (hdr) {
                    JustDoTextureWithHdrColor(tex, extraPropName, tooltip);
                } else {
                    JustDoTexture(tex, extraPropName, tooltip, channel);
                }
            }
        }

        private void DoAltTexture(
            string texturePropName,
            string extraPropName = null,
            string tooltip = null,
            string textureIsSetKeyword = null,
            bool withHdrColor = false
        ) {
            MaterialProperty texProp = FindProperty(texturePropName);
            if (texProp == null) {
                return;
            }
            DoTexture(texturePropName, extraPropName, tooltip, textureIsSetKeyword, withHdrColor);
        }

        private void DoTileAndScale(MaterialProperty mainTex) {
            _editor.TextureScaleOffsetProperty(mainTex);
        }

        private void DoTileAndScale(string texPropName) {
            MaterialProperty tex = FindProperty(texPropName);
            if (tex == null) { return; }
            DoTileAndScale(tex);
        }

        private void DoShaderPropertyIntendedBy(MaterialProperty slider, int intendation) {
            if (slider == null) {
                return;
            }
            EditorGUI.indentLevel += intendation;
            _editor.ShaderProperty(slider, MakeLabel(slider));
            EditorGUI.indentLevel -= intendation;
        }

        private void JustDoTexture(
            MaterialProperty texProp,
            string extraPropName = null,
            string tooltip = null,
            string channelProp = null
        ) {
            if (texProp == null) {
                return;
            }
            MaterialProperty extraProperty = FindProperty(extraPropName);
            _editor.TexturePropertySingleLine(
                MakeLabel(texProp, tooltip),
                texProp,
                extraProperty
            );
            // Channel selecting
            if (channelProp != null) {
                int c = _target.GetInteger(channelProp);
                EditorGUI.BeginChangeCheck();
                c = (int)(TextureChanel)EditorGUILayout.EnumPopup(MakeLabel("Channel"), (TextureChanel)c);
                if (EditorGUI.EndChangeCheck()) {
                    RecordAction($"{texProp.displayName} channel selected");
                    _target.SetInteger(channelProp, c);
                }
            }
        }

        private void JustDoTextureWithHdrColor(MaterialProperty texProp, string extraPropName = null, string tooltip = null) {
            MaterialProperty extraProperty = FindProperty(extraPropName);
            _editor.TexturePropertyWithHDRColor(MakeLabel(texProp, tooltip), texProp, extraProperty, showAlpha: false);
        }

        MaterialProperty FindProperty(string name) {
            return name != null ? FindProperty(name, _properties, false) : null;
        }

        void SetKeyword(string keyword, bool state) {
            if (state) {
                foreach (Material m in _editor.targets) {
                    m.EnableKeyword(keyword);
                }
            } else {
                foreach (Material m in _editor.targets) {
                    m.DisableKeyword(keyword);
                }
            }
        }

        bool IsKeywordEnabled(string keyword) {
            return _target.IsKeywordEnabled(keyword);
        }


        void RecordAction(string label) {
            _editor.RegisterPropertyChangeUndo(label);
        }


        static GUIContent s_staticLabel = new GUIContent();

        static GUIContent MakeLabel(string text, string tooltip = null) {
            s_staticLabel.text = text;
            s_staticLabel.tooltip = tooltip;
            return s_staticLabel;
        }
        static GUIContent MakeLabel(MaterialProperty property, string tooltip = null) {
            return MakeLabel(property.displayName, tooltip);
        }

        enum TextureChanel {
            R,
            G,
            B,
            A
        }

        enum SmoothnessSourceMetal {
            Uniform, Albedo, Metallic
        }


        // Rendering modes
        enum RenderingMode {
            Opaque, Cutout
        }

        struct RenderingSettings {
            public RenderQueue queue;
            public string renderType;
            public BlendMode srcBlend, dstBlend;
            public bool zWrite;

            public static RenderingSettings[] modes = {
                new RenderingSettings() {
                    queue = RenderQueue.Geometry,
                    renderType = "",
                    srcBlend = BlendMode.One,
                    dstBlend = BlendMode.Zero,
                    zWrite = true
                },
                new RenderingSettings() {
                    queue = RenderQueue.AlphaTest,
                    renderType = "TransparentCutout",
                    srcBlend = BlendMode.One,
                    dstBlend = BlendMode.Zero,
                    zWrite = true
                },
                new RenderingSettings() {
                    queue = RenderQueue.Transparent,
                    renderType = "Transparent",
                    srcBlend = BlendMode.SrcAlpha,
                    dstBlend = BlendMode.OneMinusSrcAlpha,
                    zWrite = false
                },
                new RenderingSettings() {
                    queue = RenderQueue.Transparent,
                    renderType = "Transparent",
                    srcBlend = BlendMode.One,
                    dstBlend = BlendMode.OneMinusSrcAlpha,
                    zWrite = false
                }
            };
        }
    }
}