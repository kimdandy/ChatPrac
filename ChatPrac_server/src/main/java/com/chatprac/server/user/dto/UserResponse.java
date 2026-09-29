package com.chatprac.server.user.dto;

import com.chatprac.server.user.User;

/** {"userId":1,"nickname":"철수"} */
public record UserResponse(Long userId, String nickname) {
    public static UserResponse from(User u) {
        return new UserResponse(u.id(), u.nickname());
    }
}
