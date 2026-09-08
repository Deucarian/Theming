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
    internal static class DeucarianThemeEditorConfirmations
    {
        internal static string BuildDeveloperToolConfirmationMessage(
            string actionName,
            string description)
        {
            string safeDescription = string.IsNullOrWhiteSpace(description)
                ? "This tool may create or modify project assets."
                : description.Trim();
            return safeDescription
                + "\n\nThis operation may create or modify project assets. Continue with '"
                + (actionName ?? "this developer tool")
                + "'?";
        }

        internal static bool ConfirmDeveloperToolAction(
            string actionName,
            string description,
            Func<string, string, string, string, bool> confirmation = null)
        {
            Func<string, string, string, string, bool> confirmationHandler = confirmation
                ?? ((title, message, ok, cancel) => EditorUtility.DisplayDialog(
                    title,
                    message,
                    ok,
                    cancel));
            return confirmationHandler(
                "Developer Tools — " + (actionName ?? "Action"),
                BuildDeveloperToolConfirmationMessage(actionName, description),
                "Continue",
                "Cancel");
        }

        internal static bool TryExecuteDeveloperToolAction(
            string actionName,
            string description,
            Action action,
            Func<string, string, string, string, bool> confirmation = null)
        {
            if (!ConfirmDeveloperToolAction(actionName, description, confirmation))
            {
                return false;
            }

            action?.Invoke();
            return true;
        }

        internal static bool ShouldKeepCurrentComposerDraft(
            string currentStyleName,
            string requestedStyleName,
            Func<string, string, string, string, string, int> showDialog = null)
        {
            Func<string, string, string, string, string, int> dialog = showDialog
                ?? EditorUtility.DisplayDialogComplex;
            string current = string.IsNullOrWhiteSpace(currentStyleName)
                ? "the current style"
                : currentStyleName;
            string requested = string.IsNullOrWhiteSpace(requestedStyleName)
                ? "the selected style"
                : requestedStyleName;
            int choice = dialog(
                "Keep Style Composer Changes?",
                $"{current} has unapplied composer changes. Keep editing it, or discard those composer changes and switch to {requested}?",
                "Keep editing",
                "Cancel",
                "Discard draft and switch");
            return choice != 2;
        }
    }
}
