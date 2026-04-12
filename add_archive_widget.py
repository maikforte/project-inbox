import re

scene_path = r"d:\Projects\Unity\project-inbox\Assets\Scenes\CombatScene.unity"

with open(scene_path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Add ArchiveWidget RT to parent's m_Children list (after DeckWidget entry)
content = content.replace(
    "  - {fileID: 263559083}\n  m_Father: {fileID: 0}",
    "  - {fileID: 263559083}\n  - {fileID: 900000001}\n  m_Father: {fileID: 0}"
)

# 2. Append all ArchiveWidget YAML blocks at the end
archive_yaml = """
--- !u!1 &900000000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 900000001}
  - component: {fileID: 900000004}
  - component: {fileID: 900000003}
  - component: {fileID: 900000002}
  m_Layer: 5
  m_Name: ArchiveWidget
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &900000001
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 900000000}
  m_LocalRotation: {x: -0, y: -0, z: -0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: -2880}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 900000010}
  m_Father: {fileID: 1004}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 31, y: 26.399994}
  m_SizeDelta: {x: 32, y: 32}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!114 &900000002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 900000000}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::InboxZero.UI.ArchiveWidget
  countLabel: {fileID: 900000011}
--- !u!114 &900000003
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 900000000}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 5427993419156271904, guid: 86997a8d80c5b8549a91ee899cf9e3a2, type: 3}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!222 &900000004
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 900000000}
  m_CullTransparentMesh: 1
--- !u!1 &900000009
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 900000010}
  - component: {fileID: 900000012}
  - component: {fileID: 900000011}
  m_Layer: 5
  m_Name: CountLabel
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &900000010
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 900000009}
  m_LocalRotation: {x: -0, y: -0, z: -0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 900000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 13.2, y: 15.37}
  m_SizeDelta: {x: 22.99, y: 15.37}
  m_Pivot: {x: 0.5, y: 1}
--- !u!114 &900000011
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 900000009}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}
  m_Name:
  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TextMeshProUGUI
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: 0
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 11400000, guid: 968711493fda13b4789c59de06321a30, type: 2}
  m_sharedMaterial: {fileID: 2536256334939891893, guid: 968711493fda13b4789c59de06321a30, type: 2}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: 1, g: 1, b: 1, a: 1}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontColorGradient:
    topLeft: {r: 1, g: 1, b: 1, a: 1}
    topRight: {r: 1, g: 1, b: 1, a: 1}
    bottomLeft: {r: 1, g: 1, b: 1, a: 1}
    bottomRight: {r: 1, g: 1, b: 1, a: 1}
  m_fontColorGradientPreset: {fileID: 0}
  m_spriteAsset: {fileID: 0}
  m_tintAllSprites: 0
  m_StyleSheet: {fileID: 0}
  m_TextStyleHashCode: -1183493901
  m_overrideHtmlColors: 0
  m_faceColor:
    serializedVersion: 2
    rgba: 4294967295
  m_fontSize: 14
  m_fontSizeBase: 14
  m_fontWeight: 400
  m_enableAutoSizing: 0
  m_fontSizeMin: 14
  m_fontSizeMax: 72
  m_fontStyle: 0
  m_HorizontalAlignment: 2
  m_VerticalAlignment: 512
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_enableWordWrapping: 1
  m_WordWrappingRatios: 0.4
  m_overflowMode: 0
  m_linkedTextComponent: {fileID: 0}
  parentLinkedComponent: {fileID: 0}
  m_enableKerning: 1
  m_ActiveFontFeatures:
  - 1801810542
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_EmojiFallbackSupport: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isSdfShader: 0
  m_renderer: {fileID: 900000012}
  m_IsTextObjectScaleStatic: 0
--- !u!222 &900000012
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 900000009}
  m_CullTransparentMesh: 1
"""

content = content.rstrip() + "\n" + archive_yaml

with open(scene_path, "w", encoding="utf-8") as f:
    f.write(content)

print("Done — ArchiveWidget added to CombatScene.")
