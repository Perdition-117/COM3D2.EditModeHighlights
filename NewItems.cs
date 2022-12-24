using BepInEx;
using BepInEx.Configuration;

namespace COM3D2.EditModeHighlights;

enum MarkSeenPreference {
	CategoryLoad,
	Hover,
	Click,
}

[BepInPlugin("net.perdition.com3d2.editmodehighlights", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("net.perdition.com3d2.editmodeitemmanager")]
public partial class EditModeHighlights : BaseUnityPlugin {
	private static ConfigEntry<MarkSeenPreference> _configMarkSeenPreference;
	private static ConfigEntry<bool> _configMarkColorsSeen;
	private static ConfigEntry<bool> _configHighlightItems;
	private static ConfigEntry<bool> _configHighlightPresets;
	private static ConfigEntry<Color> _configHighlightFrameColor;

	private static bool _doResetAnimations = false;

	private static MarkSeenPreference MarkSeenPreference => _configMarkSeenPreference.Value;
	private static bool MarkColorsSeen => _configMarkColorsSeen.Value;
	private static bool HighlightItems => _configHighlightItems.Value;
	private static bool HighlightPresets => _configHighlightPresets.Value;
	private static Color HighlightFrameColor => _configHighlightFrameColor.Value;

	private void Awake() {
		_configMarkSeenPreference = Config.Bind("General", "MarkSeenPreference", MarkSeenPreference.Click, "Determines when items get marked as seen");
		_configMarkColorsSeen = Config.Bind("General", "MarkColorsSeen", true, "Automatically mark all color variants as seen together with the main item (when item grouping is enabled)");
		_configHighlightItems = Config.Bind("General", "HighlightItems", true, "Highlight new items");
		_configHighlightPresets = Config.Bind("General", "HighlightPresets", true, "Highlight new presets");
		_configHighlightFrameColor = Config.Bind("General", "HighlightFrameColor", new Color(0, 0.75f, 1), "Color of the highlight border");

		_configHighlightFrameColor.SettingChanged += (o, e) => HighlightFrame.SetColors(HighlightFrameColor);

		_configHighlightItems.SettingChanged += (o, e) => {
			foreach (var frame in HighlightFrame.GetFrames()) {
				var buttonEdit = ItemManager.GetButtonEdit(frame.Container.Parent);
				if (buttonEdit.m_MenuItem != null) {
					SetHighlightOverlay(buttonEdit, HighlightItems && (IsNewItem(buttonEdit.m_MenuItem) || (SceneEdit.Instance.m_bUseGroup && HasNewItems(buttonEdit.m_MenuItem))));
				} else if (buttonEdit.m_PartsType != null) {
					SetHighlightOverlay(buttonEdit, HighlightItems && HasNewItems(buttonEdit.m_PartsType));
				} else if (buttonEdit.m_Category != null && buttonEdit.m_Category.m_listPartsType.Count > 0) {
					SetHighlightOverlay(buttonEdit, HighlightItems && HasNewItems(buttonEdit.m_Category));
				}
			}
		};

		_configHighlightPresets.SettingChanged += (o, e) => UpdatePresetCategoryHighlight();

		ItemManager.SetUnseenItems = HighlightItems;
		ItemManager.SetUnseenPresets = HighlightPresets;

		ItemManager.EditModeLoaded += OnEditModeLoaded;
		ItemManager.MenuItemButtonCreated += OnMenuItemButtonCreated;
		ItemManager.GroupSetButtonCreated += OnGroupSetButtonCreated;
		ItemManager.PartTypeButtonCreated += OnPartTypeButtonCreated;
		ItemManager.CategoryButtonCreated += OnCategoryButtonCreated;
		ItemManager.PresetPanelUpdated += OnPresetPanelUpdated;
		ItemManager.PresetButtonCreated += OnPresetButtonCreated;

		Harmony.CreateAndPatchAll(typeof(EditModeHighlights));
	}

	public static void MarkAsSeen(string fileName, bool doSave = false) {
		if (ItemManager.TryGetItem(fileName, out var item)) {
			item.IsNew = false;
			if (doSave) {
				ItemManager.SaveDatabase();
			}
		}
	}

	public static bool IsNewItem(string fileName) {
		return ItemManager.TryGetItem(fileName, out var item) && item.IsNew;
	}

	public static bool IsNewItem(SceneEdit.SMenuItem item) {
		return IsNewItem(item.m_strMenuFileName);
	}

	public static bool HasNewItems(SceneEdit.SMenuItem item) {
		return item.m_bGroupLeader && item.m_listMember.Exists(item => IsNewItem(item));
	}

	public static bool HasNewItems(SceneEdit.SPartsType partType) {
		return partType.m_listMenu.Exists(item => IsNewItem(item));
	}

	public static bool HasNewItems(SceneEdit.SCategory category) {
		return category.m_listPartsType.Exists(partType => ItemManager.IsItemPartType(partType) && ItemManager.IsEnabledPartType(partType) && HasNewItems(partType));
	}

	private static void SetHighlightOverlay(OverlayContainer container, bool isActive) {
		container.HighlightOverlay.Active = isActive;
		if (isActive) {
			_doResetAnimations = true;
		}
	}

	private static void SetHighlightOverlay(GameObject gameObject, bool isActive) {
		var overlay = OverlayContainer.GetContainer(gameObject);
		SetHighlightOverlay(overlay, isActive);
	}

	private static void SetHighlightOverlay(ButtonEdit buttonEdit, bool isActive) {
		SetHighlightOverlay(buttonEdit.gameObject.transform.parent.gameObject, isActive);
	}

	private static void AddHighlightOverlay(OverlayContainer container, bool isActive = false) {
		container.HighlightOverlay = new HighlightFrame(container) {
			Active = isActive,
			Color = HighlightFrameColor,
		};
		if (isActive) {
			_doResetAnimations = true;
		}
	}

	[HarmonyPatch(typeof(SceneEdit), nameof(SceneEdit.Update))]
	[HarmonyPostfix]
	private static void SceneEdit_OnUpdate() {
		if (_doResetAnimations) {
			_doResetAnimations = false;
			HighlightFrame.ResetAnimations();
		}
	}

	[HarmonyPatch(typeof(SceneEdit), nameof(SceneEdit.ClickCallback))]
	[HarmonyPrefix]
	private static void PreClickCallback(SceneEdit __instance, ref bool __state) {
		__state = __instance.m_maid.IsBusy;
	}

	[HarmonyPatch(typeof(SceneEdit), nameof(SceneEdit.ClickCallback))]
	[HarmonyPostfix]
	private static void ClickCallback(SceneEdit __instance, bool __runOriginal, ref bool __state) {
		if (!__runOriginal || __state) return;

		var buttonEdit = UIButton.current.GetComponentInChildren<ButtonEdit>();

		if (buttonEdit.m_MenuItem != null && !buttonEdit.m_MenuItem.m_bColor && MarkSeenPreference == MarkSeenPreference.Click) {
			UpdateItem(__instance, buttonEdit);
		}

		if (buttonEdit.m_PartsType != null && MarkSeenPreference == MarkSeenPreference.CategoryLoad) {
			foreach (var item in buttonEdit.m_PartsType.m_listMenu) {
				MarkAsSeen(item.m_strMenuFileName);
			}
			ItemManager.SaveDatabase();
			SetHighlightOverlay(buttonEdit, false);
			UpdateCategoryHighlights(__instance, buttonEdit.m_PartsType.m_mpn);
		}
	}

	[HarmonyPatch(typeof(SceneEdit), nameof(SceneEdit.HoverOverCallback))]
	[HarmonyPatch(typeof(SceneEdit), nameof(SceneEdit.HoverOverCallbackOnGroup))]
	[HarmonyPostfix]
	private static void HoverOverCallback(SceneEdit __instance) {
		var buttonEdit = UIEventTrigger.current.GetComponentInChildren<ButtonEdit>();
		if (MarkSeenPreference == MarkSeenPreference.Hover && buttonEdit.m_MenuItem != null && !__instance.m_maid.boAllProcPropBUSY) {
			UpdateItem(__instance, buttonEdit);
		}
	}

	private static void OnMenuItemButtonCreated(object sender, MenuItemEventArgs e) {
		AddHighlightOverlay(e.Container, HighlightItems && (IsNewItem(e.MenuItem) || (SceneEdit.Instance.m_bUseGroup && HasNewItems(e.MenuItem))));
	}

	private static void OnGroupSetButtonCreated(object sender, GroupSetButtonCreatedEventArgs e) {
		AddHighlightOverlay(e.Container, HighlightItems && IsNewItem(e.MenuItem) && !(e.IsSelected && MarkSeenPreference == MarkSeenPreference.Click));
	}

	private static void OnPartTypeButtonCreated(object sender, PartTypeButtonCreatedEventArgs e) {
		AddHighlightOverlay(e.Container, HighlightItems && HasNewItems(e.PartType));
	}

	private static void OnCategoryButtonCreated(object sender, CategoryButtonCreatedEventArgs e) {
		AddHighlightOverlay(e.Container, HighlightItems && HasNewItems(e.Category));
	}

	private static void UpdateItem(SceneEdit sceneEdit, ButtonEdit buttonEdit) {
		var item = buttonEdit.m_MenuItem;
		var isGroupButton = sceneEdit.m_listBtnGroupMember.Exists(e => e.gcBtnEdit == buttonEdit);
		if (MarkColorsSeen && sceneEdit.m_bUseGroup && item.m_bGroupLeader && !isGroupButton) {
			foreach (var colorItem in item.m_listMember) {
				MarkAsSeen(colorItem.m_strMenuFileName);
			}
			ItemManager.SaveDatabase();
		} else {
			MarkAsSeen(item.m_strMenuFileName, true);
		}

		if ((sceneEdit.m_bUseGroup && !isGroupButton && !HasNewItems(item)) || !sceneEdit.m_bUseGroup || isGroupButton) {
			SetHighlightOverlay(buttonEdit, false);

			if (isGroupButton) {
				UpdateItemHighlights(sceneEdit, item.m_bGroupLeader ? item : item.m_leaderMenu);
			} else {
				UpdatePartTypeHighlights(sceneEdit, item);
			}
		}
	}

	private static void UpdateItemHighlights(SceneEdit sceneEdit, SceneEdit.SMenuItem item) {
		if (TryGetItemButtonEdit(sceneEdit, item, out var buttonEdit) && !HasNewItems(item)) {
			SetHighlightOverlay(buttonEdit, false);
			UpdatePartTypeHighlights(sceneEdit, item);
		}
	}

	private static void UpdatePartTypeHighlights(SceneEdit sceneEdit, SceneEdit.SMenuItem item) {
		if (TryGetPartTypeButtonEdit(sceneEdit, item, out var buttonEdit) && !HasNewItems(item.m_ParentPartsType)) {
			SetHighlightOverlay(buttonEdit, false);
			UpdateCategoryHighlights(sceneEdit, item.m_mpn);
		}
	}

	private static void UpdateCategoryHighlights(SceneEdit sceneEdit, MPN mpn) {
		if (TryGetCategoryButtonEdit(sceneEdit, mpn, out var buttonEdit) && !HasNewItems(buttonEdit.m_Category)) {
			SetHighlightOverlay(buttonEdit, false);
		}
	}

	private static bool TryGetItemButtonEdit(SceneEdit sceneEdit, SceneEdit.SMenuItem item, out ButtonEdit buttonEdit) {
		buttonEdit = null;
		foreach (var menuItemSet in sceneEdit.m_listBtnMenuItem) {
			if (menuItemSet.mi == item) {
				buttonEdit = menuItemSet.gcBtnEdit;
				break;
			}
		}
		return buttonEdit != null;
	}

	private static bool TryGetPartTypeButtonEdit(SceneEdit sceneEdit, SceneEdit.SMenuItem item, out ButtonEdit buttonEdit) {
		buttonEdit = null;
		foreach (var button in sceneEdit.m_listBtnPartsType) {
			if (button == null) continue;

			var buttonEdit2 = ItemManager.GetButtonEdit(button);
			if (buttonEdit2.m_PartsType == item.m_ParentPartsType) {
				buttonEdit = buttonEdit2;
				break;
			}
		}
		return buttonEdit != null;
	}

	private static bool TryGetCategoryButtonEdit(SceneEdit sceneEdit, SceneEditInfo.EMenuCategory category, out ButtonEdit buttonEdit) {
		buttonEdit = null;
		foreach (var button in sceneEdit.m_listBtnCate) {
			var buttonEdit2 = ItemManager.GetButtonEdit(button);
			if (buttonEdit2.m_Category.m_eCategory == category) {
				buttonEdit = buttonEdit2;
				break;
			}
		}
		return buttonEdit != null;
	}

	private static bool TryGetCategoryButtonEdit(SceneEdit sceneEdit, MPN mpn, out ButtonEdit buttonEdit) {
		buttonEdit = null;
		return SceneEditInfo.m_dicPartsTypePair.TryGetValue(mpn, out var cCateNameType) && TryGetCategoryButtonEdit(sceneEdit, cCateNameType.m_eMenuCate, out buttonEdit);
	}
}
