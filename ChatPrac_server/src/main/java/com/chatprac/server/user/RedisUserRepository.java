package com.chatprac.server.user;

import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.stereotype.Repository;

import java.util.Optional;

/**
 * Redis 저장 구조
 *  - user:seq          (String) userId 발급용 카운터
 *  - user:{userId}     (Hash)   nickname
 */
@Repository
public class RedisUserRepository implements UserRepository {

    private final StringRedisTemplate redis;

    public RedisUserRepository(StringRedisTemplate redis) {
        this.redis = redis;
    }

    private static String key(long userId) {
        return "user:" + userId;
    }

    @Override
    public User create(String nickname) {
        Long id = redis.opsForValue().increment("user:seq");
        redis.opsForHash().put(key(id), "nickname", nickname);
        return new User(id, nickname);
    }

    @Override
    public Optional<User> findById(long userId) {
        Object nickname = redis.opsForHash().get(key(userId), "nickname");
        return nickname == null ? Optional.empty() : Optional.of(new User(userId, nickname.toString()));
    }
}
