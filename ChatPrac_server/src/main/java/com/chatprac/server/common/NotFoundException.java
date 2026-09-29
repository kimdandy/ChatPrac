package com.chatprac.server.common;

/** 404 Not Found - 방 또는 유저가 존재하지 않음 */
public class NotFoundException extends RuntimeException {
    public NotFoundException(String message) {
        super(message);
    }
}
