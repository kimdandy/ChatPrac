package com.chatprac.server.chat;

import com.chatprac.server.chat.dto.ChatMessageResponse;
import com.chatprac.server.chat.dto.ChatMessageSummary;
import com.chatprac.server.chat.dto.SendMessageRequest;
import com.chatprac.server.user.User;
import com.chatprac.server.user.UserService;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class ChatMessageService {

    public static final int DEFAULT_LIMIT = 50;
    public static final int MAX_LIMIT = 200;

    private final ChatMessageRepository repository;
    private final RoomService roomService;
    private final UserService userService;

    public ChatMessageService(ChatMessageRepository repository, RoomService roomService, UserService userService) {
        this.repository = repository;
        this.roomService = roomService;
        this.userService = userService;
    }

    /**
     * 채팅 조회. 오래된 순(messageId 오름차순)으로 반환합니다.
     * - afterId가 없으면: 최신 limit개
     * - afterId가 있으면: 그 messageId 이후에 들어온 메시지 최대 limit개 (폴링용)
     * @throws com.chatprac.server.common.NotFoundException 방이 없으면 404
     */
    public List<ChatMessageSummary> getMessages(long roomId, Long afterId, Integer limit) {
        roomService.get(roomId);
        int size = normalizeLimit(limit);

        List<ChatMessage> messages = (afterId != null)
                ? repository.findAfter(roomId, afterId, size)
                : repository.findLatest(roomId, size);
        return messages.stream().map(ChatMessageSummary::from).toList();
    }

    /**
     * 채팅 전송
     * 404: 방 또는 유저가 존재하지 않음 / 403: 현재 채팅할 수 없는 플레이어(방 참가자가 아님)
     */
    public ChatMessageResponse send(long roomId, SendMessageRequest request) {
        roomService.get(roomId);
        User user = userService.get(request.userId());
        roomService.requireMember(roomId, user.id());

        ChatMessage saved = repository.save(roomId, MessageType.USER,
                user.id(), user.nickname(), request.message().trim());
        return ChatMessageResponse.from(saved);
    }

    /** 공지(시스템 메시지) 전송 */
    public ChatMessageResponse sendSystem(long roomId, String message) {
        roomService.get(roomId);
        ChatMessage saved = repository.save(roomId, MessageType.SYSTEM,
                null, ChatMessage.SYSTEM_NICKNAME, message.trim());
        return ChatMessageResponse.from(saved);
    }

    private static int normalizeLimit(Integer limit) {
        if (limit == null) return DEFAULT_LIMIT;
        if (limit < 1) throw new IllegalArgumentException("limit은 1 이상이어야 합니다.");
        return Math.min(limit, MAX_LIMIT);
    }
}
