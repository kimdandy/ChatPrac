package com.chatprac.server.chat;

import java.time.LocalDateTime;

/**
 * 채팅 메시지. Redis에 JSON 문자열로 저장됩니다.
 * Unity 클라이언트(ChatApiClient)가 사용하는 필드: id, sender, content, createdAt
 */
public record ChatMessage(
        Long id,
        String roomId,
        String sender,
        String content,
        LocalDateTime createdAt
) {
    public static final int MAX_ROOM_ID_LENGTH = 50;
    public static final int MAX_SENDER_LENGTH = 50;
    public static final int MAX_CONTENT_LENGTH = 1000;
}
