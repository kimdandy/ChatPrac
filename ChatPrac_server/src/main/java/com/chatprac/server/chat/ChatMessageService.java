package com.chatprac.server.chat;

import com.chatprac.server.chat.dto.ChatMessageResponse;
import com.chatprac.server.chat.dto.SendMessageRequest;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class ChatMessageService {

    public static final int DEFAULT_LIMIT = 50;
    public static final int MAX_LIMIT = 200;

    private final ChatMessageRepository repository;

    public ChatMessageService(ChatMessageRepository repository) {
        this.repository = repository;
    }

    /**
     * 방의 메시지를 오래된 순(id 오름차순)으로 반환합니다.
     * - afterId가 없으면: 최신 limit개
     * - afterId가 있으면: 그 id 이후에 들어온 메시지 최대 limit개 (폴링용)
     */
    public List<ChatMessageResponse> getMessages(String roomId, Long afterId, Integer limit) {
        String room = validateRoomId(roomId);
        int size = normalizeLimit(limit);

        List<ChatMessage> messages = (afterId != null)
                ? repository.findAfter(room, afterId, size)
                : repository.findLatest(room, size);
        return messages.stream().map(ChatMessageResponse::from).toList();
    }

    public ChatMessageResponse send(String roomId, SendMessageRequest request) {
        String room = validateRoomId(roomId);
        ChatMessage saved = repository.save(room, request.sender().trim(), request.content().trim());
        return ChatMessageResponse.from(saved);
    }

    private static String validateRoomId(String roomId) {
        if (roomId == null || roomId.isBlank()) {
            throw new IllegalArgumentException("roomId는 비어 있을 수 없습니다.");
        }
        String trimmed = roomId.trim();
        if (trimmed.length() > ChatMessage.MAX_ROOM_ID_LENGTH) {
            throw new IllegalArgumentException("roomId는 " + ChatMessage.MAX_ROOM_ID_LENGTH + "자 이하여야 합니다.");
        }
        return trimmed;
    }

    private static int normalizeLimit(Integer limit) {
        if (limit == null) return DEFAULT_LIMIT;
        if (limit < 1) throw new IllegalArgumentException("limit은 1 이상이어야 합니다.");
        return Math.min(limit, MAX_LIMIT);
    }
}
