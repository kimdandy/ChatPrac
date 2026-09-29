package com.chatprac.server.user;

import com.chatprac.server.common.NotFoundException;
import org.springframework.stereotype.Service;

@Service
public class UserService {

    private final UserRepository repository;

    public UserService(UserRepository repository) {
        this.repository = repository;
    }

    public User create(String nickname) {
        return repository.create(nickname.trim());
    }

    /** 유저를 찾고, 없으면 404 */
    public User get(long userId) {
        return repository.findById(userId)
                .orElseThrow(() -> new NotFoundException("유저가 존재하지 않습니다. (userId=" + userId + ")"));
    }
}
