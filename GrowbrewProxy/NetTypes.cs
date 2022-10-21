﻿namespace GrowbrewProxy
{
    public class NetTypes
    {
        public enum PacketTypes
        {
            PLAYER_LOGIC_UPDATE = 0,
            CALL_FUNCTION,
            UPDATE_STATUS,
            TILE_CHANGE_REQ,
            LOAD_MAP,
            TILE_EXTRA,
            TILE_EXTRA_MULTI,
            TILE_ACTIVATE,
            APPLY_DMG,
            INVENTORY_STATE,
            ITEM_ACTIVATE,
            ITEM_ACTIVATE_OBJ,
            UPDATE_TREE,
            MODIFY_INVENTORY_ITEM,
            MODIFY_ITEM_OBJ,
            APPLY_LOCK,
            UPDATE_ITEMS_DATA,
            PARTICLE_EFF,
            ICON_STATE,
            ITEM_EFF,
            SET_CHARACTER_STATE,
            PING_REPLY,
            PING_REQ,
            PLAYER_HIT,
            APP_CHECK_RESPONSE,
            APP_INTEGRITY_FAIL,
            DISCONNECT,
            BATTLE_JOIN,
            BATTLE_EVENT,
            USE_DOOR,
            PARENTAL_MSG,
            GONE_FISHIN,
            STEAM,
            PET_BATTLE,
            NPC,
            SPECIAL,
            PARTICLE_EFFECT_V2,
            ARROW_TO_ITEM,
            TILE_INDEX_SELECTION,
            UPDATE_PLAYER_TRIBUTE,
            PVE_UPDATE_MODE,
            PVE_NPC,
            PVP_CARD_BATTLE,
            PVE_ATTACKED,
            PVE_LOGIC_UPDATE,
            PVE_BOSS, // not actually ingame, making a prediction though that it's related to some boss stuff or replaced with varlist call.
            SET_EXTRA_MODS,
            ON_STEP_ON_TILE_MOD
        };

        public enum NetMessages
        {
            UNKNOWN = 0,
            SERVER_HELLO,
            GENERIC_TEXT,
            GAME_MESSAGE,
            GAME_PACKET,
            ERROR,
            TRACK,
            LOG_REQ,
            LOG_RES
        };

    }
}
