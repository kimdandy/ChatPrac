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

import static org.hamcrest.Matchers.hasSize;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
class ChatMessageControllerTest {

    @TestConfiguration
    static class TestConfig {
        @Bean
        @Primary
        InMemoryChatMessageRepository inMemoryChatMessageRepository() {
            return new InMemoryChatMessageRepository();
        }
    }

    @Autowired
    MockMvc mvc;

    @Autowired
    InMemoryChatMessageRepository repository;

    @BeforeEach
    void clean() {
        repository.clear();
    }

    /** 메시지를 보내고, 응답으로 받은 id를 반환합니다. */
    private long send(String room, String sender, String content) throws Exception {
        String body = mvc.perform(post("/api/rooms/{roomId}/messages", room)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("{\"sender\":\"" + sender + "\",\"content\":\"" + content + "\"}"))
                .andExpect(status().isCreated())
                .andReturn().getResponse().getContentAsString(java.nio.charset.StandardCharsets.UTF_8);
        return ((Number) JsonPath.read(body, "$.id")).longValue();
    }

    @Test
    void 전송한_메시지를_조회할_수_있다() throws Exception {
        send("1", "User1234", "안녕하세요");

        mvc.perform(get("/api/rooms/1/messages"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$", hasSize(1)))
                .andExpect(jsonPath("$[0].id").isNumber())
                .andExpect(jsonPath("$[0].sender").value("User1234"))
                .andExpect(jsonPath("$[0].content").value("안녕하세요"))
                .andExpect(jsonPath("$[0].createdAt").isString());
    }

    @Test
    void 조회는_최신_limit개를_오래된_순으로_반환한다() throws Exception {
        for (int i = 1; i <= 5; i++) send("1", "A", "msg" + i);

        mvc.perform(get("/api/rooms/1/messages").param("limit", "3"))
                .andExpect(jsonPath("$", hasSize(3)))
                .andExpect(jsonPath("$[0].content").value("msg3"))
                .andExpect(jsonPath("$[2].content").value("msg5"));
    }

    @Test
    void 방끼리_메시지가_섞이지_않는다() throws Exception {
        send("1", "A", "room1");
        send("2", "B", "room2");

        mvc.perform(get("/api/rooms/2/messages"))
                .andExpect(jsonPath("$", hasSize(1)))
                .andExpect(jsonPath("$[0].content").value("room2"));
    }

    @Test
    void afterId_이후의_메시지만_반환한다() throws Exception {
        long firstId = send("1", "A", "old");
        send("1", "B", "new");

        mvc.perform(get("/api/rooms/1/messages").param("afterId", String.valueOf(firstId)))
                .andExpect(jsonPath("$", hasSize(1)))
                .andExpect(jsonPath("$[0].content").value("new"));
    }

    @Test
    void 빈_내용은_400을_반환한다() throws Exception {
        mvc.perform(post("/api/rooms/1/messages")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content("{\"sender\":\"A\",\"content\":\"   \"}"))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.status").value(400));
    }
}
