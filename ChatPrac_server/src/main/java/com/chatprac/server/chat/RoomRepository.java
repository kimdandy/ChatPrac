package com.chatprac.server.chat;

import java.util.List;
import java.util.Optional;

public interface RoomRepository {

    Room create(String name);

    Optional<Room> findById(long roomId);

    /** @return 새로 참가했으면 true, 이미 참가자였으면 false */
    boolean addMember(long roomId, long userId);

    /** @return 참가자였다가 나갔으면 true, 원래 참가자가 아니었으면 false */
    boolean removeMember(long roomId, long userId);

    boolean isMember(long roomId, long userId);

    /** 참가자 userId 목록 (오름차순) */
    List<Long> findMemberIds(long roomId);
}
