package com.chatprac.server.user;

import java.util.Optional;

public interface UserRepository {

    User create(String nickname);

    Optional<User> findById(long userId);
}
