package com.chatprac.server.chat.dto;

import jakarta.validation.constraints.NotNull;

/** [개발용] POST /api/v1/rooms/{roomId}/members 요청 본문: {"userId":1} */
public record JoinRequest(
        @NotNull(message = "userId는 필수입니다.")
        Long userId
) {
}
