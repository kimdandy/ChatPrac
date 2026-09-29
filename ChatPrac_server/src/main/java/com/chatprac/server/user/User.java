package com.chatprac.server.user;

/** 유저 (게임 플레이어) */
public record User(Long id, String nickname) {
    public static final int MAX_NICKNAME_LENGTH = 20;
}
