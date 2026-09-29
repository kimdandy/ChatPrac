package com.chatprac.server.user;

import com.chatprac.server.user.dto.CreateUserRequest;
import com.chatprac.server.user.dto.UserResponse;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

/**
 * [개발용] 유저 API - 로그인/회원 API 명세가 나오면 교체 예정
 *  - 생성: POST /api/v1/users           {"nickname":"철수"} → 201 {"userId":1,"nickname":"철수"}
 *  - 조회: GET  /api/v1/users/{userId}  → 200 / 404
 */
@RestController
@RequestMapping("/api/v1/users")
public class UserController {

    private final UserService service;

    public UserController(UserService service) {
        this.service = service;
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public UserResponse create(@Valid @RequestBody CreateUserRequest request) {
        return UserResponse.from(service.create(request.nickname()));
    }

    @GetMapping("/{userId}")
    public UserResponse get(@PathVariable long userId) {
        return UserResponse.from(service.get(userId));
    }
}
