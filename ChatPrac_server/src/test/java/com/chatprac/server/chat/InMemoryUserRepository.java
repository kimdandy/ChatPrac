package com.chatprac.server.chat;

import com.chatprac.server.user.User;
import com.chatprac.server.user.UserRepository;

import java.util.Map;
import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

/** 테스트용 메모리 유저 저장소 */
class InMemoryUserRepository implements UserRepository {

    private final AtomicLong seq = new AtomicLong();
    private final Map<Long, User> users = new ConcurrentHashMap<>();

    void clear() {
        seq.set(0);
        users.clear();
    }

    @Override
    public User create(String nickname) {
        User user = new User(seq.incrementAndGet(), nickname);
        users.put(user.id(), user);
        return user;
    }

    @Override
    public Optional<User> findById(long userId) {
        return Optional.ofNullable(users.get(userId));
    }
}
