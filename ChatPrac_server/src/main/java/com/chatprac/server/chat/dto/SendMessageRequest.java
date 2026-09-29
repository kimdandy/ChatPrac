package com.chatprac.server.chat.dto;

import com.chatprac.server.chat.ChatMessage;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

/**
 * POST /api/rooms/{roomId}/messages 요청 본문
 * Unity가 보내는 JSON: {"sender":"User1234","content":"안녕하세요"}
 */
public record SendMessageRequest(
        @NotBlank(message = "sender는 비어 있을 수 없습니다.")
        @Size(max = ChatMessage.MAX_SENDER_LENGTH, message = "sender는 {max}자 이하여야 합니다.")
        String sender,

        @NotBlank(message = "content는 비어 있을 수 없습니다.")
        @Size(max = ChatMessage.MAX_CONTENT_LENGTH, message = "content는 {max}자 이하여야 합니다.")
        String content
) {
}
