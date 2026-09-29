package com.chatprac.server.chat;

import com.chatprac.server.chat.dto.ChatMessageResponse;
import com.chatprac.server.chat.dto.ChatMessageSummary;
import com.chatprac.server.chat.dto.SendMessageRequest;
import com.chatprac.server.chat.dto.SystemMessageRequest;
import com.chatprac.server.common.ForbiddenException;
import jakarta.validation.Valid;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

/**
 * 채팅 API (v1)
 *  - 전송: POST /api/v1/rooms/{roomId}/messages  {"userId":1,"message":"..."}
 *          201 Created / 403 채팅할 수 없는 플레이어 / 404 방 또는 유저 없음
 *  - 조회: GET  /api/v1/rooms/{roomId}/messages[?limit=50][&afterId=100]
 *          200 OK / 404 방 없음
 *          (limit, afterId는 명세 추가 파라미터 - WebSocket 적용 전까지 폴링용)
 *  - 공지: POST /api/v1/rooms/{roomId}/system-messages  {"message":"..."}  (명세 추가 API)
 */
@RestController
@RequestMapping("/api/v1/rooms/{roomId}")
public class ChatMessageController {

    private final ChatMessageService service;
    private final String adminKey;

    public ChatMessageController(ChatMessageService service,
                                 @Value("${chat.admin-key:}") String adminKey) {
        this.service = service;
        this.adminKey = adminKey;
    }

    @GetMapping("/messages")
    public List<ChatMessageSummary> getMessages(@PathVariable long roomId,
                                                @RequestParam(required = false) Long afterId,
                                                @RequestParam(required = false) Integer limit) {
        return service.getMessages(roomId, afterId, limit);
    }

    @PostMapping("/messages")
    @ResponseStatus(HttpStatus.CREATED)
    public ChatMessageResponse send(@PathVariable long roomId,
                                    @Valid @RequestBody SendMessageRequest request) {
        return service.send(roomId, request);
    }

    @PostMapping("/system-messages")
    @ResponseStatus(HttpStatus.CREATED)
    public ChatMessageResponse sendSystem(@PathVariable long roomId,
                                          @RequestHeader(value = "X-Admin-Key", required = false) String key,
                                          @Valid @RequestBody SystemMessageRequest request) {
        if (!adminKey.isBlank() && !adminKey.equals(key)) {
            throw new ForbiddenException("공지 권한이 없습니다. (X-Admin-Key 확인)");
        }
        return service.sendSystem(roomId, request.message());
    }
}
