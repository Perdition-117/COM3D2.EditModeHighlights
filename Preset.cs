using System;

namespace COM3D2.EditModeHighlights;

partial class EditModeHighlights {
	private static Dictionary<string, PresetCtrl.PresetButton> PresetButtons => BaseMgr<PresetMgr>.Instance.m_presetCtrl.m_dicPresetButton;

	public static bool IsNewPreset(string fileName) {
		return ItemManager.TryGetPreset(fileName, out var preset) && preset.IsNew;
	}

	public static bool HasNewPresets() {
		var presets = GameMain.Instance.CharacterMgr.PresetListLoad();
		return presets.Exists(preset => IsNewPreset(preset.strFileName));
	}

	public static void MarkAsSeenPreset(string fileName, bool doSave = false) {
		if (ItemManager.TryGetPreset(fileName, out var preset)) {
			preset.IsNew = false;
			if (doSave) {
				ItemManager.SaveDatabase();
			}
		}
	}

	private static void UpdatePreset(string presetName) {
		if (PresetButtons.TryGetValue(presetName, out var button)) {
			MarkAsSeenPreset(presetName, true);

			SetHighlightOverlay(button.presetButton, false);

			if (!HasNewPresets()) {
				UpdatePresetCategoryHighlight();
			}
		}
	}

	private static void OnEditModeLoaded(object sender, EventArgs e) {
		UpdatePresetCategoryHighlight();
	}

	private static void UpdatePresetCategoryHighlight() {
		if (TryGetCategoryButtonEdit(SceneEdit.Instance, SceneEditInfo.EMenuCategory.プリセット, out var categoryButtonEdit)) {
			SetHighlightOverlay(categoryButtonEdit, HighlightPresets && HasNewPresets());
		}
	}

	private static void OnPresetPanelUpdated(object sender, EventArgs e) {
		if (MarkSeenPreference == MarkSeenPreference.CategoryLoad) {
			foreach (var button in PresetButtons.Values) {
				MarkAsSeenPreset(button.preset.strFileName);
			}
		}

		UpdatePresetCategoryHighlight();

		PresetMgr.Instance.m_presetCtrl.m_goPresetParent.GetComponent<UITable>().Reposition();
	}

	private static void OnPresetButtonCreated(object sender, PresetButtonCreatedEventArgs e) {
		var button = e.PresetButton;
		AddHighlightOverlay(e.Container, HighlightPresets && IsNewPreset(button.preset.strFileName));

		var eventTrigger = button.presetButton.GetOrAddComponent<UIEventTrigger>();
		EventDelegate.Add(eventTrigger.onHoverOver, PresetButton_OnHoverOver);
	}

	private static void PresetButton_OnHoverOver() {
		if (MarkSeenPreference == MarkSeenPreference.MouseHover) {
			UpdatePreset(UIEventTrigger.current.name);
		}
	}

	[HarmonyPatch(typeof(PresetCtrl), nameof(PresetCtrl.Init))]
	[HarmonyPostfix]
	private static void Init(PresetCtrl __instance) {
		// reduce padding to compensate for the added frame size from the borders
		__instance.m_goPresetParent.GetComponent<UITable>().padding -= new Vector2(HighlightFrame.BorderPadding / 2, HighlightFrame.BorderPadding / 2);
	}

	[HarmonyPatch(typeof(PresetMgr), nameof(PresetMgr.ClickPreset))]
	[HarmonyPrefix]
	private static void PreClickPreset(ref bool __state) {
		__state = GameMain.Instance.CharacterMgr.GetMaid(0).IsBusy;
	}

	[HarmonyPatch(typeof(PresetMgr), nameof(PresetMgr.ClickPreset))]
	[HarmonyPostfix]
	private static void ClickPreset(bool __runOriginal, ref bool __state) {
		if (!__runOriginal || __state) return;

		if (UICamera.currentTouchID == -1 && MarkSeenPreference == MarkSeenPreference.Click) {
			UpdatePreset(UIButton.current.name);
		}
	}
}
