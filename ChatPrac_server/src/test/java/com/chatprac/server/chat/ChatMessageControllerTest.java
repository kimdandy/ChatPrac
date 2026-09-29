package com.chatprac.server.chat;

import com.jayway.jsonpath.JsonPath;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Primary;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;

import java.nio.charset.StandardCharsets;

import static org.hamcrest.Matchers.hasSize;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
class ChatMessageControllerTest {

    @TestConfiguration
    static class TestConfig {
        @Bean @Primary InMemoryChatMessageRepository inMemoryChatMessageRepository() { return new InMemoryChatMessageRepository(); }
        @Bean @Primary InMemoryRoomRepository inMemoryRoomRepository() { return new InMemoryRoomRepository(); }
        @Bean @Primary InMemoryUserRepository inMemoryUserRepository() { return new InMemoryUserRepository(); }
    }

    @Autowired MockMvc mvc;
    @Autowired InMemoryChatMessageRepository messages;
    @Autowired InMemoryRoomRepository rooms;
    @Autowired InMemoryUserRepository users;

    long roomId;
    long chulsoo;   // 방 참가자
    long younghee;  // 방 참가자 아님

    @BeforeEach
    void setUp() throws Exception {
        messages.clear();
        rooms.clear();
        users.clear();

        roomId = idOf(mvc.perform(post("/api/v1/rooms").contentType(MediaType.APPLICATION_JSON)
                .content("{\"name\":\"마피아 1번방\"}")).andExpect(status().isCreated()), "$.roomId");
        chulsoo = createUser("철수");
        younghee = createUser("영희");
        mvc.perform(post("/api/v1/rooms/{roomId}/members", roomId).contentType(MediaType.APPLICATION_JSON)
                .content("{\"userId\":" + chulsoo + "}")).andExpect(status().isCreated());
    }

    private long createUser(String nickname) throws Exception {
        return idOf(mvc.perform(post("/api/v1/users").contentType(MediaType.APPLICATION_JSON)
                .content("{\"nickname\":\"" + nickname + "\"}")).andExpect(status().isCreated()), "$.userId");
    }

    private static long idOf(org.springframework.test.web.servlet.ResultActions r, String path) throws Exception {
        String body = r.andReturn().getResponse().getContentAsString(StandardCharsets.UTF_8);
        return ((Number) JsonPath.read(body, path)).longValue();
    }

    private org.springframework.test.web.servlet.ResultActions send(long room, long userId, String message) throws Exception {
        return mvc.perform(post("/api/v1/rooms/{roomId}/messages", room)
                .contentType(MediaType.APPLICATION_JSON)
                .content("{\"userId\":" + userId + ",\"message\":\"" + message + "\"}"));
    }

    @Test
    void 전송_성공시_201과_명세_형식으로_응답한다() throws Exception {
        send(roomId, chulsoo, "2번이 마피아 같은데?")
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.messageId").isNumber())
                .andExpect(jsonPath("$.userId").value(chulsoo))
                .andExpect(jsonPath("$.nickname").value("철수"))
                .andExpect(jsonPath("$.message").value("2번이 마피아 같은데?"))
                .andExpect(jsonPath("$.createdAt").isString())
                .andExpect(jsonPath("$.type").value("USER"));
    }

    @Test
    void 조회는_명세_형식으로_오래된_순으로_반환한다() throws Exception {
        send(roomId, chulsoo, "첫 번째").andExpect(status().isCreated());
        send(roomId, chulsoo, "두 번째").andExpect(status().isCreated());

        // [0]은 철수 입장 시스템 메시지
        mvc.perform(get("/api/v1/rooms/{roomId}/messages", roomId))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$", hasSize(3)))
                .andExpect(jsonPath("$[0].type").value("SYSTEM"))
                .andExpect(jsonPath("$[0].message").value("철수님이 입장했습니다."))
                .andExpect(jsonPath("$[1].messageId").isNumber())
                .andExpect(jsonPath("$[1].userId").value(chulsoo))
                .andExpect(jsonPath("$[1].nickname").value("철수"))
                .andExpect(jsonPath("$[1].message").value("첫 번째"))
                .andExpect(jsonPath("$[2].message").value("두 번째"));
    }

    @Test
    void 방_참가자가_아니면_403() throws Exception {
        send(roomId, younghee, "안녕").andExpect(status().isForbidden());
    }

    @Test
    void 없는_방이면_404() throws Exception {
        send(999, chulsoo, "안녕").andExpect(status().isNotFound());
        mvc.perform(get("/api/v1/rooms/999/messages")).andExpect(status().isNotFound());
    }

    @Test
    void 없는_유저면_404() throws Exception {
        send(roomId, 999, "안녕").andExpect(status().isNotFound());
    }

    @Test
    void 빈_메시지는_400() throws Exception {
        send(roomId, chulsoo, "   ").andExpect(status().isBadRequest());
    }

    @Test
    void afterId_이후의_메시지만_반환한다() throws Exception {
        long first = idOf(send(roomId, chulsoo, "old"), "$.messageId");
        send(roomId, chulsoo, "new");

        mvc.perform(get("/api/v1/rooms/{roomId}/messages", roomId).param("afterId", String.valueOf(first)))
                .andExpect(jsonPath("$", hasSize(1)))
                .andExpect(jsonPath("$[0].message").value("new"));
    }

    @Test
    void 이미_참가한_유저가_다시_참가하면_입장_알림을_만들지_않는다() throws Exception {
        mvc.perform(post("/api/v1/rooms/{roomId}/members", roomId).contentType(MediaType.APPLICATION_JSON)
                .content("{\"userId\":" + chulsoo + "}")).andExpect(status().isOk());

        mvc.perform(get("/api/v1/rooms/{roomId}/messages", roomId))
                .andExpect(jsonPath("$", hasSize(1)));
    }

    @Test
    void 방을_나가면_퇴장_알림이_남고_더_이상_채팅할_수_없다() throws Exception {
        mvc.perform(delete("/api/v1/rooms/{roomId}/members/{userId}", roomId, chulsoo))
                .andExpect(status().isNoContent());

        mvc.perform(get("/api/v1/rooms/{roomId}/messages", roomId))
                .andExpect(jsonPath("$[1].message").value("철수님이 퇴장했습니다."));
        send(roomId, chulsoo, "안녕").andExpect(status().isForbidden());
    }

    @Test
    void 공지는_시스템_메시지로_저장된다() throws Exception {
        mvc.perform(post("/api/v1/rooms/{roomId}/system-messages", roomId)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("{\"message\":\"밤이 되었습니다.\"}"))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.type").value("SYSTEM"))
                .andExpect(jsonPath("$.message").value("밤이 되었습니다."));
    }
}
