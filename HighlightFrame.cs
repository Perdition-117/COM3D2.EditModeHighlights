using System.Linq;

namespace COM3D2.EditModeHighlights;

internal class HighlightFrame : IOverlay {
	internal const int BorderPadding = 12;

	private static readonly UIAtlas _atlas;

	private readonly UISprite _sprite;
	private readonly TweenAlpha _tweener;

	static HighlightFrame() {
		_atlas = Resources.FindObjectsOfTypeAll<UIAtlas>().FirstOrDefault(o => o.name == "AtlasCommon");
	}

	public HighlightFrame(OverlayContainer container) {
		var gameObject = NGUITools.AddChild(container.Parent);
		gameObject.name = "Highlight";

		_sprite = gameObject.AddComponent<UISprite>();
		_sprite.atlas = _atlas;
		_sprite.spriteName = "cm3d2_common_plate_white";
		_sprite.type = UIBasicSprite.Type.Sliced;
		_sprite.centerType = UIBasicSprite.AdvancedType.Invisible;
		_sprite.width = container.Anchor.width + BorderPadding;
		_sprite.height = container.Anchor.height + BorderPadding;

		_tweener = gameObject.AddComponent<TweenAlpha>();
		_tweener.from = 1f;
		_tweener.to = 0.5f;
		_tweener.duration = 0.5f;
		_tweener.style = UITweener.Style.PingPong;
		_tweener.method = UITweener.Method.EaseIn;
		_tweener.PlayForward();
	}

	public bool Active {
		get => _sprite.atlas != null;
		set => _sprite.atlas = value ? _atlas : null;
	}

	public Color Color {
		get => _sprite.color;
		set => _sprite.color = value;
	}

	public void ResetAnimation() {
		_tweener.tweenFactor = 0;
	}

	public static IEnumerable<HighlightFrame> GetFrames() {
		foreach (var button in SceneEdit.Instance.m_listBtnMenuItem) {
			if (OverlayContainer.TryGetContainer(button.goItem, out var container)) {
				yield return (HighlightFrame)container.HighlightOverlay;
			}
		}
		foreach (var button in SceneEdit.Instance.m_listBtnGroupMember) {
			if (OverlayContainer.TryGetContainer(button.goItem, out var container)) {
				yield return (HighlightFrame)container.HighlightOverlay;
			}
		}
		foreach (var button in SceneEdit.Instance.m_listBtnPartsType) {
			if (button != null && OverlayContainer.TryGetContainer(button, out var container) && container.HighlightOverlay is HighlightFrame highlightFrame) {
					yield return highlightFrame;
			}
		}
		foreach (var button in SceneEdit.Instance.m_listBtnCate) {
			if (OverlayContainer.TryGetContainer(button, out var container) && container.HighlightOverlay is HighlightFrame highlightFrame) {
					yield return highlightFrame;
			}
		}
	}

	public static void SetColors(Color color) {
		foreach (var frame in GetFrames()) {
			frame.Color = color;
		}
	}

	public static void ResetAnimations() {
		foreach (var frame in GetFrames()) {
			frame.ResetAnimation();
		}
	}
}
