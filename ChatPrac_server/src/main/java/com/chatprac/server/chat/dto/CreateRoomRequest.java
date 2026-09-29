package com.chatprac.server.chat.dto;

import com.chatprac.server.chat.Room;
import jakarta.validation.constraints.Size;

/** [개발용] POST /api/v1/rooms 요청 본문: {"name":"마피아 1번방"} (name 생략 가능) */
public record CreateRoomRequest(
        @Size(max = Room.MAX_NAME_LENGTH, message = "name은 {max}자 이하여야 합니다.")
        String name
) {
}
