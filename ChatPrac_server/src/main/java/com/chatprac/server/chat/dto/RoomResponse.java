package com.chatprac.server.chat.dto;

import com.chatprac.server.chat.Room;
import com.chatprac.server.user.dto.UserResponse;

import java.time.LocalDateTime;
import java.util.List;

/** {"roomId":1,"name":"마피아 1번방","createdAt":"...","members":[{"userId":1,"nickname":"철수"}]} */
public record RoomResponse(
        Long roomId,
        String name,
        LocalDateTime createdAt,
        List<UserResponse> members
) {
    public static RoomResponse of(Room room, List<UserResponse> members) {
        return new RoomResponse(room.id(), room.name(), room.createdAt(), members);
    }
}
