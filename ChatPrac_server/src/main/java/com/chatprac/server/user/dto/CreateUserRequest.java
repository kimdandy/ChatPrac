package com.chatprac.server.user.dto;

import com.chatprac.server.user.User;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

/** POST /api/v1/users 요청 본문: {"nickname":"철수"} */
public record CreateUserRequest(
        @NotBlank(message = "nickname은 비어 있을 수 없습니다.")
        @Size(max = User.MAX_NICKNAME_LENGTH, message = "nickname은 {max}자 이하여야 합니다.")
        String nickname
) {
}
