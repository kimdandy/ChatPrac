package com.chatprac.server.chat;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

/** 테스트용 메모리 방 저장소 */
class InMemoryRoomRepository implements RoomRepository {

    private final AtomicLong seq = new AtomicLong();
    private final Map<Long, Room> rooms = new ConcurrentHashMap<>();
    private final Map<Long, Set<Long>> members = new ConcurrentHashMap<>();

    void clear() {
        seq.set(0);
        rooms.clear();
        members.clear();
    }

    @Override
    public Room create(String name) {
        long id = seq.incrementAndGet();
        Room room = new Room(id, (name == null || name.isBlank()) ? "방 " + id : name.trim(), LocalDateTime.now());
        rooms.put(id, room);
        return room;
    }

    @Override
    public Optional<Room> findById(long roomId) {
        return Optional.ofNullable(rooms.get(roomId));
    }

    private Set<Long> set(long roomId) {
        return members.computeIfAbsent(roomId, k -> ConcurrentHashMap.newKeySet());
    }

    @Override
    public boolean addMember(long roomId, long userId) {
        return set(roomId).add(userId);
    }

    @Override
    public boolean removeMember(long roomId, long userId) {
        return set(roomId).remove(userId);
    }

    @Override
    public boolean isMember(long roomId, long userId) {
        return set(roomId).contains(userId);
    }

    @Override
    public List<Long> findMemberIds(long roomId) {
        return set(roomId).stream().sorted().toList();
    }
}
