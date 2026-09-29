package com.chatprac.server.chat;

import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;

/**
 * Redis 저장 구조
 *  - room:seq              (String) roomId 발급용 카운터
 *  - room:{roomId}         (Hash)   name, createdAt
 *  - room:{roomId}:members (Set)    참가자 userId 목록
 */
@Repository
public class RedisRoomRepository implements RoomRepository {

    private final StringRedisTemplate redis;

    public RedisRoomRepository(StringRedisTemplate redis) {
        this.redis = redis;
    }

    private static String key(long roomId) {
        return "room:" + roomId;
    }

    private static String membersKey(long roomId) {
        return "room:" + roomId + ":members";
    }

    @Override
    public Room create(String name) {
        Long id = redis.opsForValue().increment("room:seq");
        String roomName = (name == null || name.isBlank()) ? "방 " + id : name.trim();
        LocalDateTime now = LocalDateTime.now();
        redis.opsForHash().putAll(key(id), Map.of("name", roomName, "createdAt", now.toString()));
        return new Room(id, roomName, now);
    }

    @Override
    public Optional<Room> findById(long roomId) {
        Map<Object, Object> hash = redis.opsForHash().entries(key(roomId));
        if (hash.isEmpty()) return Optional.empty();
        Object createdAt = hash.get("createdAt");
        return Optional.of(new Room(roomId, String.valueOf(hash.get("name")),
                createdAt == null ? null : LocalDateTime.parse(createdAt.toString())));
    }

    @Override
    public boolean addMember(long roomId, long userId) {
        Long added = redis.opsForSet().add(membersKey(roomId), String.valueOf(userId));
        return added != null && added > 0;
    }

    @Override
    public boolean removeMember(long roomId, long userId) {
        Long removed = redis.opsForSet().remove(membersKey(roomId), String.valueOf(userId));
        return removed != null && removed > 0;
    }

    @Override
    public boolean isMember(long roomId, long userId) {
        return Boolean.TRUE.equals(redis.opsForSet().isMember(membersKey(roomId), String.valueOf(userId)));
    }

    @Override
    public List<Long> findMemberIds(long roomId) {
        Set<String> ids = redis.opsForSet().members(membersKey(roomId));
        if (ids == null) return List.of();
        return ids.stream().map(Long::valueOf).sorted().toList();
    }
}
