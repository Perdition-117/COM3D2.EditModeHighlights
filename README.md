# COM3D2.EditModeHighlights

Highlights new items and presets in edit mode.

![Animation](https://user-images.githubusercontent.com/87424475/209343114-e1586e26-7712-454d-b6e7-da812ab6667c.gif)

## Configuration

Settings are found in `BepInEx\config\net.perdition.com3d2.editmodehighlights.cfg` and may be modified directly or by using a BepInEx configuration manager.

By default, highlights are removed when an item or preset is equipped. This behavior is controlled via the `MarkSeenPreference`.

- `CategoryLoad` - When opening a category (such as Shoes), all items within are instantly marked as seen. Highlights remain until the next time the category is opened.
- `MouseHover` - Items are marked as seen when moving the mouse cursor over them.
- `Click` - Items are marked as seen when equipping them. (default)

## Notes

Items that are removed and later readded are once again highlighted. For improved compatibility with [ExtendedPresetManagement](https://github.com/krypto5863/COM3D2.ExtendedPresetManagement), the same does not apply to presets, which are permanently marked as seen. (on a filename basis)

## Installation

Get the latest version from [the release page](../../releases/latest). Extract the archive contents into `BepInEx\plugins`.
