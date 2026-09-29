package com.chatprac.server.chat;

import com.chatprac.server.chat.dto.ChatMessageResponse;
import com.chatprac.server.chat.dto.SendMessageRequest;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

/**
 * Unity ChatApiClient와 맞춘 채팅 API
 *  - 조회: GET  /api/rooms/{roomId}/messages[?limit=50][&afterId=123]
 *  - 전송: POST /api/rooms/{roomId}/messages  {"sender":"...","content":"..."}
 */
@RestController
@RequestMapping("/api/rooms/{roomId}/messages")
public class ChatMessageController {

    private final ChatMessageService service;

    public ChatMessageController(ChatMessageService service) {
        this.service = service;
    }

    @GetMapping
    public List<ChatMessageResponse> getMessages(@PathVariable String roomId,
                                                 @RequestParam(required = false) Long afterId,
                                                 @RequestParam(required = false) Integer limit) {
        return service.getMessages(roomId, afterId, limit);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public ChatMessageResponse send(@PathVariable String roomId,
                                    @Valid @RequestBody SendMessageRequest request) {
        return service.send(roomId, request);
    }
}
