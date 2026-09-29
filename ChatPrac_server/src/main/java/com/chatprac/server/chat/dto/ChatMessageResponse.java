package com.chatprac.server.chat.dto;

import com.chatprac.server.chat.ChatMessage;

import java.time.LocalDateTime;

/**
 * 응답 JSON: {"id":1,"sender":"User1234","content":"안녕하세요","createdAt":"2026-09-29T14:03:12.345"}
 * 필드 이름은 Unity ChatApiClient의 idField / senderField / contentField / createdAtField 기본값과 같습니다.
 */
public record ChatMessageResponse(
        Long id,
        String roomId,
        String sender,
        String content,
        LocalDateTime createdAt
) {
    public static ChatMessageResponse from(ChatMessage m) {
        return new ChatMessageResponse(m.id(), m.roomId(), m.sender(), m.content(), m.createdAt());
    }
}
