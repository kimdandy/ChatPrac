package com.chatprac.server.chat;

import com.chatprac.server.chat.dto.RoomResponse;
import com.chatprac.server.common.ForbiddenException;
import com.chatprac.server.common.NotFoundException;
import com.chatprac.server.user.User;
import com.chatprac.server.user.UserRepository;
import com.chatprac.server.user.UserService;
import com.chatprac.server.user.dto.UserResponse;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class RoomService {

    private final RoomRepository rooms;
    private final UserRepository users;
    private final UserService userService;
    private final ChatMessageRepository messages;

    public RoomService(RoomRepository rooms, UserRepository users, UserService userService,
                       ChatMessageRepository messages) {
        this.rooms = rooms;
        this.users = users;
        this.userService = userService;
        this.messages = messages;
    }

    public Room create(String name) {
        return rooms.create(name);
    }

    /** 방을 찾고, 없으면 404 */
    public Room get(long roomId) {
        return rooms.findById(roomId)
                .orElseThrow(() -> new NotFoundException("방이 존재하지 않습니다. (roomId=" + roomId + ")"));
    }

    public RoomResponse getDetail(long roomId) {
        Room room = get(roomId);
        List<UserResponse> members = rooms.findMemberIds(roomId).stream()
                .map(users::findById)
                .flatMap(java.util.Optional::stream)
                .map(UserResponse::from)
                .toList();
        return RoomResponse.of(room, members);
    }

    /**
     * 방 참가. 처음 참가하면 "OOO님이 입장했습니다." 시스템 메시지를 남깁니다.
     * @return 새로 참가했으면 true, 이미 참가자였으면 false
     */
    public boolean join(long roomId, long userId) {
        get(roomId);
        User user = userService.get(userId);
        boolean joined = rooms.addMember(roomId, userId);
        if (joined) {
            messages.save(roomId, MessageType.SYSTEM, null, ChatMessage.SYSTEM_NICKNAME,
                    user.nickname() + "님이 입장했습니다.");
        }
        return joined;
    }

    /** 방 나가기. 참가자였다면 "OOO님이 퇴장했습니다." 시스템 메시지를 남깁니다. */
    public void leave(long roomId, long userId) {
        get(roomId);
        User user = userService.get(userId);
        if (rooms.removeMember(roomId, userId)) {
            messages.save(roomId, MessageType.SYSTEM, null, ChatMessage.SYSTEM_NICKNAME,
                    user.nickname() + "님이 퇴장했습니다.");
        }
    }

    /** 방 참가자가 아니면 403 (현재 채팅할 수 없는 플레이어) */
    public void requireMember(long roomId, long userId) {
        if (!rooms.isMember(roomId, userId)) {
            throw new ForbiddenException("현재 채팅할 수 없는 플레이어입니다. (방 참가자가 아님)");
        }
    }
}
