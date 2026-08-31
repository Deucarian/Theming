using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianThemeManagerWindow
    {


        private void DrawComposerPreview()
        {
            DeucarianThemeManagerSelection selection =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            DrawThemePreview(
                selection.ResolvedTheme,
                null,
                composerSurface,
                composerCorners,
                composerBorder,
                composerSize,
                composerTypography);
        }

        private void ApplyComposerPreview()
        {
            DeucarianThemePreviewCoordinator.ApplyComposerPreview(
                DeucarianThemeManagerSelection.FromEditorPrefs(),
                composerSource,
                composerSurface,
                composerCorners,
                composerBorder,
                composerSize,
                composerTypography);
        }

        private static void DrawThemePreview(
            DeucarianTheme theme,
            DeucarianThemeStyle style,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile corners,
            DeucarianThemeStrokeProfile border,
            DeucarianThemeDensity density,
            DeucarianThemeTypographyProfile typography)
        {
            Rect previewRect = GUILayoutUtility.GetRect(260f, 232f, GUILayout.ExpandWidth(true));
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color baseColor = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.SurfaceRaised,
                new Color(0.10f, 0.16f, 0.20f, 0.96f));
            Color surfaceColor = surface != null
                ? surface.ResolveSurfaceColor(baseColor)
                : style != null ? style.ResolveSurfaceColor(baseColor) : baseColor;
            Color borderColor = border != null
                ? border.ResolveBorderColor(surfaceColor)
                : style != null
                    ? style.ResolveBorderColor(surfaceColor)
                    : new Color(0.65f, 0.78f, 0.86f, 0.5f);
            float radius = corners != null
                ? corners.CornerRadius
                : style != null ? style.CornerRadius : 8f;
            float borderWidth = border != null
                ? border.BorderWidth
                : style != null ? style.BorderWidth : 1f;
            Rect panelRect = new Rect(
                previewRect.x + 2f,
                previewRect.y + 2f,
                Mathf.Max(0f, previewRect.width - 4f),
                Mathf.Max(0f, previewRect.height - 4f));
            Rect panelContentRect = DrawPreviewSurface(
                panelRect,
                surfaceColor,
                borderColor,
                radius,
                borderWidth);

            if (surface != null && surface.UseGeneratedNoiseTexture)
            {
                Texture2D texture = surface.GetGeneratedTexture();
                if (texture != null)
                {
                    Color previousColor = GUI.color;
                    GUI.color = surface.TextureTint;
                    GUI.DrawTexture(panelContentRect, texture, ScaleMode.StretchToFill, true);
                    GUI.color = previousColor;
                }
            }

            TMP_FontAsset tmpFont = typography != null
                ? typography.ResolvedFontAsset
                : DeucarianThemeTypographyProfile.ProjectDefaultFontAsset;
            Font sourceFont = ResolvePreviewFont(typography, out string fontLabel, out bool usingFallback);
            DeucarianThemeTextStyle titleToken = typography != null
                ? typography.Title
                : DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Title);
            DeucarianThemeTextStyle bodyToken = typography != null
                ? typography.Body
                : DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Body);
            DeucarianThemeTextStyle captionToken = typography != null
                ? typography.Caption
                : DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Caption);

            Color textPrimary = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.TextPrimary,
                new Color(0.92f, 0.95f, 0.97f, 1f));
            Color textSecondary = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.TextSecondary,
                new Color(0.72f, 0.78f, 0.82f, 1f));
            Color textMuted = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.TextMuted,
                new Color(0.55f, 0.62f, 0.67f, 1f));
            Color accent = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.Accent,
                new Color(0.24f, 0.76f, 0.68f, 1f));
            Color success = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.Success,
                new Color(0.34f, 0.76f, 0.52f, 1f));

            Rect content = new Rect(
                panelContentRect.x + 14f,
                panelContentRect.y + 12f,
                Mathf.Max(0f, panelContentRect.width - 28f),
                Mathf.Max(0f, panelContentRect.height - 24f));
            GUIStyle titleStyle = CreatePreviewTextStyle(sourceFont, titleToken, textPrimary, FontStyle.Bold);
            GUIStyle bodyStyle = CreatePreviewTextStyle(sourceFont, bodyToken, textSecondary, FontStyle.Normal);
            GUIStyle captionStyle = CreatePreviewTextStyle(sourceFont, captionToken, textMuted, FontStyle.Normal);

            GUI.Label(new Rect(content.x, content.y, content.width, 26f), "Theme preview", titleStyle);
            GUI.Label(
                new Rect(content.x, content.y + 28f, content.width, 34f),
                "A single specimen for typography, fields, actions, and status.",
                bodyStyle);
            GUI.Label(
                new Rect(content.x, content.y + 60f, content.width, 18f),
                "Caption · semantic role tokens",
                captionStyle);

            Rect inputRect = new Rect(content.x, content.y + 84f, content.width, 30f);
            Color inputFill = Color.Lerp(surfaceColor, textPrimary, 0.06f);
            DrawPreviewSurface(inputRect, inputFill, borderColor, Mathf.Max(3f, radius - 6f), borderWidth);
            GUI.Label(
                new Rect(inputRect.x + 9f, inputRect.y + 5f, inputRect.width - 18f, inputRect.height - 10f),
                "Sample input",
                bodyStyle);

            float controlHeight = ResolvePreviewControlHeight(density);
            float buttonGap = 8f;
            float buttonWidth = Mathf.Max(72f, (content.width - buttonGap) * 0.5f);
            Rect primaryRect = new Rect(content.x, content.y + 124f, buttonWidth, controlHeight);
            Rect secondaryRect = new Rect(
                primaryRect.xMax + buttonGap,
                primaryRect.y,
                Mathf.Max(0f, content.xMax - primaryRect.xMax - buttonGap),
                controlHeight);
            DrawPreviewSurface(primaryRect, accent, accent, Mathf.Max(3f, radius - 6f), 1f);
            DrawPreviewSurface(
                secondaryRect,
                Color.Lerp(surfaceColor, textPrimary, 0.08f),
                borderColor,
                Mathf.Max(3f, radius - 6f),
                borderWidth);
            GUIStyle buttonStyle = CreatePreviewTextStyle(sourceFont, bodyToken, textPrimary, FontStyle.Bold);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(primaryRect, "Primary", buttonStyle);
            GUI.Label(secondaryRect, "Secondary", buttonStyle);

            Rect statusRect = new Rect(content.x, content.y + 166f, content.width, 18f);
            EditorGUI.DrawRect(new Rect(statusRect.x, statusRect.y + 5f, 7f, 7f), success);
            GUI.Label(
                new Rect(statusRect.x + 13f, statusRect.y, statusRect.width - 13f, statusRect.height),
                "Success · ready to activate",
                captionStyle);
            string resolvedFontName = tmpFont != null ? tmpFont.name : "TMP project default";
            string fontNote = usingFallback
                ? $"{resolvedFontName} · editor fallback ({fontLabel})"
                : $"{resolvedFontName} · source font preview";
            GUI.Label(
                new Rect(content.x, content.yMax - 16f, content.width, 16f),
                fontNote,
                captionStyle);
        }

        internal static Font ResolvePreviewFont(
            DeucarianThemeTypographyProfile typography,
            out string fontLabel,
            out bool usingFallback)
        {
            TMP_FontAsset tmpFont = typography != null
                ? typography.ResolvedFontAsset
                : DeucarianThemeTypographyProfile.ProjectDefaultFontAsset;
            Font sourceFont = tmpFont != null ? tmpFont.sourceFontFile : null;
            fontLabel = sourceFont != null
                ? sourceFont.name
                : EditorStyles.label.font != null ? EditorStyles.label.font.name : "Unity editor font";
            usingFallback = sourceFont == null;
            return sourceFont != null ? sourceFont : EditorStyles.label.font;
        }

        private static GUIStyle CreatePreviewTextStyle(
            Font font,
            DeucarianThemeTextStyle token,
            Color color,
            FontStyle fallbackStyle)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                font = font,
                fontSize = Mathf.Max(1, Mathf.RoundToInt(token.FontSize)),
                fontStyle = ResolveUnityFontStyle(token.FontStyle, fallbackStyle),
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            style.normal.textColor = color;
            return style;
        }

        private static FontStyle ResolveUnityFontStyle(FontStyles styles, FontStyle fallback)
        {
            bool bold = (styles & FontStyles.Bold) != 0;
            bool italic = (styles & FontStyles.Italic) != 0;
            if (bold && italic)
            {
                return FontStyle.BoldAndItalic;
            }

            if (bold)
            {
                return FontStyle.Bold;
            }

            if (italic)
            {
                return FontStyle.Italic;
            }

            return styles == FontStyles.Normal ? FontStyle.Normal : fallback;
        }

        private static Color ResolvePreviewColor(DeucarianTheme theme, string roleId, Color fallback)
        {
            return theme != null && theme.TryGetColorById(roleId, out Color color)
                ? color
                : fallback;
        }

        internal static Rect DrawPreviewSurface(
            Rect rect,
            Color fillColor,
            Color borderColor,
            float radius,
            float borderWidth)
        {
            float safeWidth = Mathf.Clamp(
                borderWidth,
                0f,
                Mathf.Min(rect.width, rect.height) * 0.5f);
            if (safeWidth <= 0f)
            {
                DeucarianEditorVisualShell.DrawInsetSurface(
                    rect,
                    fillColor,
                    fillColor,
                    radius);
                return rect;
            }

            // Fill the outer surface with the border color, then inset the content by the
            // configured pixel width. This makes 2 px and wider profiles visibly distinct.
            DeucarianEditorVisualShell.DrawInsetSurface(
                rect,
                borderColor,
                borderColor,
                radius);
            Rect contentRect = new Rect(
                rect.x + safeWidth,
                rect.y + safeWidth,
                Mathf.Max(0f, rect.width - safeWidth * 2f),
                Mathf.Max(0f, rect.height - safeWidth * 2f));
            DeucarianEditorVisualShell.DrawInsetSurface(
                contentRect,
                fillColor,
                fillColor,
                Mathf.Max(0f, radius - safeWidth));
            return contentRect;
        }

        private void BuildDeveloperToolsDrawer()
        {
            if (workbench?.Drawer == null)
            {
                return;
            }

            workbench.Drawer.Clear();
            developerToolsOpen = false;
            developerToolsDrawer = DeucarianEditorWorkbenchSurfaces.CreateDrawer(false);
            developerToolsDrawer.Root.name = "deucarian-theme-manager-developer-tools";

            VisualElement header = DeucarianEditorWorkbenchSurfaces.CreateDrawerHeader(
                "Developer Tools");
            header.Add(DeucarianEditorWorkbenchSurfaces.CreateDrawerAction(
                DeucarianEditorIconIds.OpenFolder,
                "Open assets folder",
                DeucarianThemingMenuActions.OpenThemeAssetsFolder,
                "Reveal the Deucarian theme assets folder in the Project window."));
            header.Add(DeucarianEditorWorkbenchSurfaces.CreateDrawerAction(
                DeucarianEditorIconIds.ChevronDown,
                "Close",
                () => SetDeveloperToolsOpen(false),
                "Close Developer Tools."));
            developerToolsDrawer.Content.Add(header);

            VisualElement columns = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumns();
            VisualElement create = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumn("Create");
            AddDeveloperToolAction(
                create,
                DeucarianEditorIconIds.CreateFolder,
                "Theme family...",
                CreateThemeFamily,
                "Opens a save dialog and creates a theme family with its palette and style references at the chosen project location.");
            AddDeveloperToolAction(
                create,
                DeucarianEditorIconIds.CreatePackage,
                "Starter assets",
                () => DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets(),
                "Creates any missing default Deucarian theme assets and repairs their built-in references when needed.");
            AddDeveloperToolAction(
                create,
                DeucarianEditorIconIds.Palette,
                "Built-in theme styles",
                () => DeucarianThemingMenuActions.CreateBuiltinThemeStyleAssets(),
                "Creates or repairs the package's built-in visual-style assets in the default theming folder.");
            AddDeveloperToolAction(
                create,
                DeucarianEditorIconIds.Monitor,
                "UI Toolkit demo assets",
                () => DeucarianUIToolkitDemoAssetFactory.CreateDemoAssets(),
                "Creates or updates the UI Toolkit demo assets under the project's Deucarian theming folder.");

            VisualElement repair = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumn("Repair");
            AddDeveloperToolAction(
                repair,
                DeucarianEditorIconIds.Wrench,
                "Selected theme family",
                () => DeucarianThemingMenuActions.RepairActiveThemeFamilySetup(),
                "Repairs missing built-in references on the currently selected theme family and its related assets.");
            AddDeveloperToolAction(
                repair,
                DeucarianEditorIconIds.Refresh,
                "Selected palette",
                () => DeucarianThemingMenuActions.RepairActivePaletteSetup(),
                "Repairs the active palette's built-in theme and visual-style setup.");

            VisualElement legacy = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumn("Legacy");
            legacy.style.marginRight = 0f;
            AddDeveloperToolAction(
                legacy,
                DeucarianEditorIconIds.History,
                "Create minimal palette...",
                () => DeucarianThemingMenuActions.CreateMinimalPaletteFromSavePanel(),
                "Opens a save dialog and creates a minimal legacy palette asset at the chosen project location.");

            columns.Add(create);
            columns.Add(repair);
            columns.Add(legacy);
            developerToolsDrawer.Content.Add(columns);
            workbench.Drawer.Add(developerToolsDrawer.Root);
        }

        private void AddDeveloperToolAction(
            VisualElement column,
            string iconId,
            string text,
            Action action,
            string confirmationDescription)
        {
            column?.Add(DeucarianEditorWorkbenchSurfaces.CreateDrawerAction(
                iconId,
                text,
                () =>
                {
                    if (!TryExecuteDeveloperToolAction(
                            text,
                            confirmationDescription,
                            action))
                    {
                        return;
                    }

                    RefreshAssets();
                },
                confirmationDescription));
        }
    }
}
