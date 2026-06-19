---
name: pixellab-asset-generation
description: Generate pixel art (characters, animations, tilesets, isometric tiles, map objects) via the PixelLab MCP server and import the result into this Unity project. Use whenever the task is "generate a sprite/character/tileset/animation" or "replace a Graybox2D placeholder with real art".
---

# PixelLab MCP — Asset Generation Workflow

PixelLab is a separate MCP server (`pixellab`, local scope, added to this project) that generates pixel art on a remote service and hands back download URLs. Tool names below come from the hosted docs at `https://api.pixellab.ai/mcp/docs` — **before calling any of them, run `ToolSearch` for `pixellab` to confirm the live schema and exact `mcp__pixellab__*` tool name**, since parameter sets can drift from docs.

## Core model: everything is async
Every `create_*` call returns an ID **immediately** (job queued, not finished). You then poll the matching `get_*` call until status is `completed` (or `failed`/`review`). Typical timings: isometric tiles 10–20s, map objects 15–30s, objects/tilesets 2–4min, characters/animations 2–5min. Don't block-poll tightly — space out `get_*` calls.

## Tool map (asset-generation subset — ignore the chat/sandbox/agent-deploy tools, those are for PixelLab's own hosted game-builder product, not this workflow)

| Asset type | Create | Status/fetch | List | Delete |
|---|---|---|---|---|
| Character (humanoid/quadruped, 4/8-dir) | `create_character` | `get_character` | `list_characters` | `delete_character` |
| Character variant (same identity) | `create_character_state` | (poll via `get_character`) | — | — |
| Character animation | `animate_character` | (poll via `get_character`) | — | `delete_animation` |
| Top-down Wang tileset (16-tile, seamless) | `create_topdown_tileset` | `get_topdown_tileset` | `list_topdown_tilesets` | `delete_topdown_tileset` |
| Sidescroller tileset (16-tile, transparent) | `create_sidescroller_tileset` | `get_sidescroller_tileset` | `list_sidescroller_tilesets` | `delete_sidescroller_tileset` |
| Isometric tile (single) | `create_isometric_tile` | `get_isometric_tile` | `list_isometric_tiles` | `delete_isometric_tile` |
| Pro tile (style-image driven) | `create_tiles_pro` | `get_tiles_pro` | `list_tiles_pro` | `delete_tiles_pro` |
| Map object (transparent single/multi-frame) | `create_map_object` | `get_map_object` | — | — |
| 1-direction object | `create_1_direction_object` | `get_object` | `list_objects` | `delete_object` |
| 8-direction object | `create_8_direction_object` | `get_object` | `list_objects` | `delete_object` |
| Object variant | `create_object_state` | `get_object` | — | — |
| Object animation | `animate_object` | `get_object` | — | — |
| Account credits | — | `get_balance` | — | — |

Utility: `agent_help(question)` queries PixelLab's own knowledge base for usage questions if something here is unclear; `agent_feedback` reports bugs.

## Practical workflow for this project
1. **Check credits first** for anything beyond a single tile: `get_balance`. Animation/multi-direction calls expose a `confirm_cost` flag (default `False`) — call once to see the previewed cost, then re-call with `confirm_cost=True` to actually run it. Don't blindly pass `confirm_cost=True` on the first call.
2. **Pick the right tool for the placeholder being replaced:**
   - A `Graybox2D/64x64.png`-style single icon/portrait → `create_map_object` or `create_1_direction_object`.
   - A character that needs to face multiple directions in `HandDisplay`/combat → `create_character` (`n_directions=8` for full rotation, `4` if only cardinal facings are used) then `animate_character` for idle/attack/hit poses.
   - Repeating floor/wall art for a tilemap → `create_topdown_tileset` (RPG-style top-down) — this project is top-down combat-screen style, so prefer `view="low top-down"` to match existing camera framing, not `"high top-down"`.
   - A side-scrolling platform segment (if ever added) → `create_sidescroller_tileset`.
3. **Keep style consistent across calls** — these all help avoid a mismatched-style mess:
   - Reuse `seed` across related generations.
   - Pass `lower_base_tile_id`/`upper_base_tile_id`/`base_tile_id` from a previous tileset call into the next one when generating an adjacent terrain type, for seamless transitions.
   - Use `reference_image_base64`/`style_image_base64`/`style_images` params (where available) pointing at an already-approved generated asset or an existing project sprite to keep new assets visually consistent with what's already in `Assets/`.
4. **Poll until done**, then grab the download URL from the completed `get_*` response. URLs are unauthenticated (the UUID is the access key) — fetch the bytes (e.g. `curl -o Assets/Art/Generated/<name>.png <url>` via Bash) directly into the project.
5. **Import immediately — do not defer.** PixelLab auto-deletes generated assets (confirmed for map objects; assume the same elsewhere unless `agent_help` says otherwise) roughly 8 hours after generation. Download into `Assets/` the same session you generate.
6. **Set Unity import settings via Unity MCP** after the file lands in `Assets/`: Texture Type = Sprite, Filter Mode = Point (no filter), Compression = None, and Pixels Per Unit matching this project's `Pixel Perfect Camera` (640×360 reference — see `inbox-zero-conventions`). For multi-frame animations/tilesets, slice as a grid sprite sheet matching the tile/frame size returned by the generation call.
7. **Wire the sprite reference** into the prefab/scene exactly like a Graybox2D swap — per CLAUDE.md, this should be a drop-in `m_Sprite` GUID swap on the existing `Image`/`SpriteRenderer`, no layout changes. See `inbox-zero-scene-yaml` for the exact YAML fields if editing by hand, or use Unity MCP's `manage_components`/`manage_asset` if going through the Editor.
8. **Multi-frame objects can land in "review" status** instead of "completed" — when that happens, inspect the candidate frames and call `select_object_frames(object_id, indices=[...])` to promote the ones you want (or `dismiss_review(object_id)` to discard all and try again with a different prompt).

## Gotchas
- Tool list only loads at MCP-server-connect time for a session — if you just added/reconfigured the `pixellab` server, the calling session needs a restart before these tools appear (this bit us once already: a local-scope config got written under a different drive-letter casing than the active project key and silently didn't load — verify with `claude mcp list` from Bash if a tool seems missing).
- `delete_*` calls require `confirm=True` — they are permanent, including all rotations/animations/storage files.
- Don't use the chat/agent/sandbox tool families (`chat_send_message`, `sandbox_*`, `agent_talk`, etc.) for this project — those drive PixelLab's own hosted game-builder, not asset generation for import into our Unity project.
