package com.chatprac.server.chat;

import java.util.List;

/** 채팅 메시지 저장소. 실제 구현은 {@link RedisChatMessageRepository}. */
public interface ChatMessageRepository {

    /** 새 메시지를 저장하고, id와 createdAt이 채워진 메시지를 반환합니다. */
    ChatMessage save(String roomId, String sender, String content);

    /** 방의 최신 메시지 limit개를 오래된 순(id 오름차순)으로 반환합니다. */
    List<ChatMessage> findLatest(String roomId, int limit);

    /** afterId보다 큰 id의 메시지를 최대 limit개, 오래된 순으로 반환합니다. (폴링용) */
    List<ChatMessage> findAfter(String roomId, long afterId, int limit);
}
