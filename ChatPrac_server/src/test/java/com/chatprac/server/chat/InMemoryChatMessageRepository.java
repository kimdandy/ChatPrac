package com.chatprac.server.chat;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

/** 테스트용 메모리 저장소 (Redis 없이 컨트롤러/서비스 동작 확인) */
class InMemoryChatMessageRepository implements ChatMessageRepository {

    private final Map<String, List<ChatMessage>> rooms = new ConcurrentHashMap<>();
    private final Map<String, AtomicLong> seqs = new ConcurrentHashMap<>();

    void clear() {
        rooms.clear();
        seqs.clear();
    }

    @Override
    public synchronized ChatMessage save(String roomId, String sender, String content) {
        long id = seqs.computeIfAbsent(roomId, k -> new AtomicLong()).incrementAndGet();
        ChatMessage m = new ChatMessage(id, roomId, sender, content, LocalDateTime.now());
        rooms.computeIfAbsent(roomId, k -> new ArrayList<>()).add(m);
        return m;
    }

    @Override
    public synchronized List<ChatMessage> findLatest(String roomId, int limit) {
        List<ChatMessage> all = rooms.getOrDefault(roomId, List.of());
        return new ArrayList<>(all.subList(Math.max(0, all.size() - limit), all.size()));
    }

    @Override
    public synchronized List<ChatMessage> findAfter(String roomId, long afterId, int limit) {
        return rooms.getOrDefault(roomId, List.of()).stream()
                .filter(m -> m.id() > afterId)
                .limit(limit)
                .toList();
    }
}
