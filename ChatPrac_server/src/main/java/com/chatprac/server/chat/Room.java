package com.chatprac.server.chat;

import java.time.LocalDateTime;

/** 채팅방 (게임 방) */
public record Room(Long id, String name, LocalDateTime createdAt) {
    public static final int MAX_NAME_LENGTH = 50;
}
