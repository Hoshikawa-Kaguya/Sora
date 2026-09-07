# Known Intentional Protocol Gaps

These gaps are intentional and must not be reported unless the re-evaluation criteria below are met.

## OB11 Segments

`contact`, `keyboard`, `music`, segment-form `poke`, `shake`, and outgoing `market_face` are skipped because they have no justified cross-protocol framework representation or are legacy/niche features.

## OB11 Actions

Skip convenience aliases (`send_poke`), LLBot-internal actions (`scan_qrcode`, `get_config`, `set_config`, `llonebot_debug`, `get_event`, `send_pb`, `get_rkey`), and trivial/admin/legacy actions (`set_restart`, `can_send_record`, `can_send_image`, `clean_cache`, `get_credentials`).

The following go-cqhttp or niche LLBot actions are also intentionally skipped: `get_guild_list`, `.handle_quick_operation`, `get_group_honor_info`, `get_group_at_all_remain`, `get_group_file_system_info`, `send_group_sign`, `get_group_album_list`, `create_group_album`, `delete_group_album`, `upload_group_album`, `get_flash_file_info`, `download_flash_file`, `upload_flash_file`, `reshare_flash_file`, `batch_delete_group_member`, `get_group_ignore_add_request`, `set_group_file_forever`, `set_group_msg_mask`, `get_recommend_face`, `get_ai_characters`, `send_group_ai_record`, `get_profile_like`, `get_profile_like_me`, `fetch_emoji_like`, `get_qq_avatar`, `get_doubt_friends_add_request`, `set_doubt_friends_add_request`, and `get_robot_uin_range`.

## Unsurfaced OB11 Fields

OB11-only fields in `GroupInfo`, `GroupMemberInfo`, `GroupFileInfo`, and reaction counts remain in adapter DTOs rather than framework entities. They may be exposed through a future adapter extension type.

## OB11 Parameter Extensions

OB11-only parameters such as `friend_poke.target_id` do not modify the cross-protocol `IBotApi`; they belong in `IOneBot11ExtApi`.

## Re-evaluate When

1. Milky adds equivalent support.
2. A commonly requested feature justifies an adapter extension.
3. A segment gains a natural cross-protocol `SegmentType`.
4. Developers need an unsurfaced field through an OB11-specific extension model.
