using System;
using TMPro;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Semantic text roles shared by themed TMP targets and editor previews.</summary>
    public enum DeucarianThemeTextRole
    {
        Title,
        Body,
        Caption
    }

    /// <summary>Typography metrics for one semantic text role.</summary>
    [Serializable]
    public struct DeucarianThemeTextStyle
    {
        [SerializeField, Min(1f)] private float fontSize;
        [SerializeField] private FontStyles fontStyle;
        [SerializeField] private float characterSpacing;
        [SerializeField] private float lineSpacing;

        public DeucarianThemeTextStyle(
            float fontSize,
            FontStyles fontStyle = FontStyles.Normal,
            float characterSpacing = 0f,
            float lineSpacing = 0f)
        {
            this.fontSize = fontSize;
            this.fontStyle = fontStyle;
            this.characterSpacing = characterSpacing;
            this.lineSpacing = lineSpacing;
            this = Sanitize(this);
        }

        public float FontSize => fontSize;
        public FontStyles FontStyle => fontStyle;
        public float CharacterSpacing => characterSpacing;
        public float LineSpacing => lineSpacing;

        public static DeucarianThemeTextStyle DefaultFor(DeucarianThemeTextRole role)
        {
            switch (role)
            {
                case DeucarianThemeTextRole.Title:
                    return new DeucarianThemeTextStyle(20f, FontStyles.Bold);
                case DeucarianThemeTextRole.Caption:
                    return new DeucarianThemeTextStyle(11f);
                default:
                    return new DeucarianThemeTextStyle(14f);
            }
        }

        internal static DeucarianThemeTextStyle Sanitize(DeucarianThemeTextStyle value)
        {
            value.fontSize = Mathf.Clamp(value.fontSize <= 0f ? 1f : value.fontSize, 1f, 512f);
            value.characterSpacing = Mathf.Clamp(value.characterSpacing, -100f, 100f);
            value.lineSpacing = Mathf.Clamp(value.lineSpacing, -100f, 100f);
            return value;
        }
    }

    /// <summary>Optional TMP font and role metrics composed into a visual theme style.</summary>
    [CreateAssetMenu(
        fileName = "Theme Typography",
        menuName = "Deucarian/Theming/Theme Typography Profile")]
    public sealed class DeucarianThemeTypographyProfile : ScriptableObject
    {
        [SerializeField] private string displayName = "System Default";
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private DeucarianThemeTextStyle title =
            new DeucarianThemeTextStyle(20f, FontStyles.Bold);
        [SerializeField] private DeucarianThemeTextStyle body =
            new DeucarianThemeTextStyle(14f);
        [SerializeField] private DeucarianThemeTextStyle caption =
            new DeucarianThemeTextStyle(11f);

        public string DisplayName => displayName;
        public TMP_FontAsset FontAsset => fontAsset;
        public TMP_FontAsset ResolvedFontAsset => fontAsset != null ? fontAsset : ProjectDefaultFontAsset;
        public static TMP_FontAsset ProjectDefaultFontAsset
        {
            get
            {
                // LoadDefaultSettings returns null quietly when TMP Essentials have not been imported.
                // Accessing TMP_Settings.instance directly opens an editor importer window in that case.
                TMP_Settings settings = TMP_Settings.LoadDefaultSettings();
                return settings != null ? TMP_Settings.defaultFontAsset : null;
            }
        }
        public DeucarianThemeTextStyle Title => title;
        public DeucarianThemeTextStyle Body => body;
        public DeucarianThemeTextStyle Caption => caption;

        public DeucarianThemeTextStyle GetStyle(DeucarianThemeTextRole role)
        {
            switch (role)
            {
                case DeucarianThemeTextRole.Title:
                    return title;
                case DeucarianThemeTextRole.Caption:
                    return caption;
                default:
                    return body;
            }
        }

        /// <summary>Configures the optional font and all semantic role metrics.</summary>
        public void Configure(
            TMP_FontAsset resolvedFontAsset,
            DeucarianThemeTextStyle titleStyle,
            DeucarianThemeTextStyle bodyStyle,
            DeucarianThemeTextStyle captionStyle,
            string name = "System Default")
        {
            displayName = string.IsNullOrWhiteSpace(name) ? "System Default" : name.Trim();
            fontAsset = resolvedFontAsset;
            title = DeucarianThemeTextStyle.Sanitize(titleStyle);
            body = DeucarianThemeTextStyle.Sanitize(bodyStyle);
            caption = DeucarianThemeTextStyle.Sanitize(captionStyle);
            NotifyChanged();
        }

        private void OnValidate()
        {
            displayName = string.IsNullOrWhiteSpace(displayName) ? "System Default" : displayName.Trim();
            title = DeucarianThemeTextStyle.Sanitize(title);
            body = DeucarianThemeTextStyle.Sanitize(body);
            caption = DeucarianThemeTextStyle.Sanitize(caption);
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}
