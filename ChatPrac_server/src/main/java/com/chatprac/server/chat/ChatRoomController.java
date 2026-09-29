package com.chatprac.server.chat;

import com.chatprac.server.chat.dto.CreateRoomRequest;
import com.chatprac.server.chat.dto.JoinRequest;
import com.chatprac.server.chat.dto.RoomResponse;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 * [개발용] 방 API - 로비/게임 API 명세가 나오면 교체 예정
 *  - 방 생성:   POST   /api/v1/rooms                           {"name":"마피아 1번방"} → 201
 *  - 방 조회:   GET    /api/v1/rooms/{roomId}                  → 200 (참가자 목록 포함) / 404
 *  - 방 참가:   POST   /api/v1/rooms/{roomId}/members          {"userId":1} → 201 새로 참가 / 200 이미 참가자 / 404
 *  - 방 나가기: DELETE /api/v1/rooms/{roomId}/members/{userId}  → 204 / 404
 * 참가/나가기 시 입장·퇴장 시스템 메시지가 채팅에 남습니다.
 */
@RestController
@RequestMapping("/api/v1/rooms")
public class ChatRoomController {

    private final RoomService service;

    public ChatRoomController(RoomService service) {
        this.service = service;
    }

    @PostMapping
    public ResponseEntity<RoomResponse> create(@Valid @RequestBody(required = false) CreateRoomRequest request) {
        Room room = service.create(request == null ? null : request.name());
        return ResponseEntity.status(HttpStatus.CREATED).body(service.getDetail(room.id()));
    }

    @GetMapping("/{roomId}")
    public RoomResponse get(@PathVariable long roomId) {
        return service.getDetail(roomId);
    }

    @PostMapping("/{roomId}/members")
    public ResponseEntity<RoomResponse> join(@PathVariable long roomId,
                                             @Valid @RequestBody JoinRequest request) {
        boolean joined = service.join(roomId, request.userId());
        return ResponseEntity.status(joined ? HttpStatus.CREATED : HttpStatus.OK)
                .body(service.getDetail(roomId));
    }

    @DeleteMapping("/{roomId}/members/{userId}")
    public ResponseEntity<Void> leave(@PathVariable long roomId, @PathVariable long userId) {
        service.leave(roomId, userId);
        return ResponseEntity.noContent().build();
    }
}
