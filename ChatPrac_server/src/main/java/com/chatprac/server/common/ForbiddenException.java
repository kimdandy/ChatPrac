package com.chatprac.server.common;

/** 403 Forbidden - 현재 채팅할 수 없는 플레이어 등 */
public class ForbiddenException extends RuntimeException {
    public ForbiddenException(String message) {
        super(message);
    }
}
